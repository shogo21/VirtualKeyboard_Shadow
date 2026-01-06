from threading import Thread
from keras.models import load_model
import time
from logger import logging
from touch_viewer import add_result

import preprocessing

import tensorflow as tf
import numpy as np
import os
import cv2
import math

WINDOW_SIZE = 5
NUM_ROWS = 29
THRESHOLD = 0.5

physical_devices = tf.config.list_physical_devices('GPU')
if len(physical_devices) > 0:
    for device in physical_devices:
        tf.config.experimental.set_memory_growth(device, True)
        print('{} memory growth: {}'.format(device, tf.config.experimental.get_memory_growth(device)))
else:
    print("Not enough GPU hardware devices available")


def process_values(touches, output, output_float):
    for row in range(NUM_ROWS):
        output_float[row] = np.delete(output_float[row], 0)
        output_float[row] = np.append(output_float[row], touches[row])
        del output[row][0]
        if touches[row] > THRESHOLD:
            output[row].append(1)
        else:
            output[row].append(0)
        if output[row][int((WINDOW_SIZE-1)/2)] != output[row][int((WINDOW_SIZE-1)/2-1)]:
            count_one = output[row].count(1)
            if count_one > (WINDOW_SIZE-1)/2:
                output[row][int((WINDOW_SIZE-1)/2)] = 1
            else:
                output[row][int((WINDOW_SIZE-1)/2)] = 0

class TouchDetector(Thread):
    def __init__(self, sh_touches, sh_framebuffer, sh_keys_pos_from_unity):
        super(TouchDetector, self).__init__()
        self.stop_flg = False
        self.sh_touches = sh_touches
        self.sh_framebuffer = sh_framebuffer
        self.sh_keys_pos_from_unity = sh_keys_pos_from_unity
        self.model = tf.saved_model.load('./predict_araimodel_6464_statefultrue_29input')
        # self.model.compile()

        #mycode
        self.output = [[0]*WINDOW_SIZE for _ in range(NUM_ROWS)]
        self.output_float = [np.array([0.0]*WINDOW_SIZE) for _ in range(NUM_ROWS)]
    
    def run(self):
        # In thread
        print('TOUCH DETECTOR START')
        while not self.stop_flg:
            logging('TouchDetectorLoopLog', None)
            keys_info = self.sh_keys_pos_from_unity.try_get()
            key_images = []
            if keys_info is None:
                time.sleep(0.02)
            else:
                frame_id, keysize, angle, keys_pos = keys_info
                keysize = keysize * 1.75
                id_image = self.sh_framebuffer.get_by_frame_id(frame_id)
                if id_image is not None:
                    frameId, img = id_image
                    """if (frameId % 20 == 0):
                        cv2.imwrite(f"./image_test/frame_{frameId}.png", img)"""
                    for i, key_pos in enumerate(keys_pos):
                        corners_pos = []
                        dist = keysize / 1.5 * math.sqrt(2) / 2
                        pos = [key_pos[0] / 1.5 + 320, 480 - (key_pos[1] / 1.5 +240)]
                        for j in range(4):
                            corners_angle = -angle + math.pi/2*j + math.pi/4
                            pt = [dist * math.cos(corners_angle)+pos[0], dist * math.sin(corners_angle)+pos[1]]
                            corners_pos.append(pt)
                        cropped_image = preprocessing.crop_by_key(img, corners_pos)
                        if cropped_image is None:
                            #print("cropped_image is None")
                            #logging('TouchDetectLog', None)
                            #key_images.append(np.zeros((64, 64, 3), dtype=np.float32))
                            continue
                        else:
                            #cropped_image = cv2.rotate(cropped_image, cv2.ROTATE_180)
                            cropped_image = cv2.flip(cropped_image, 0)
                            key_images.append(cropped_image)
                            #if (frameId % 20 == 0):
                                #cv2.imwrite(f"./image_test/cropped_image_{frameId}frame_{i}key.png", cropped_image)
                                #print(key_corner_pos)
                                #print(f"Saved cropped_image_{frameId}frame_{i}key")
            
            if not key_images:
                continue
            else:
                touches = self.model([image.reshape((1,1,64,64,3)) for image in key_images])
                touches = [t.numpy() for t in touches]

                #mycode
                process_values(touches, self.output, self.output_float)
                logging('TouchDetectLog', [t[int((WINDOW_SIZE-1)/2)].item() for t in self.output_float])
                #print([t[int((WINDOW_SIZE-1)/2)].item() for t in self.output_float])
                self.sh_touches.set([t[int((WINDOW_SIZE-1)/2)] > 0.5 for t in self.output])

        print('TOUCH DETECTOR END')
    
    def stop(self):
        self.stop_flg = True