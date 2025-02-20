from threading import Thread
from keras.models import load_model
import time
from logger import logging
from touch_viewer import add_result

import preprocessing

import tensorflow as tf
import numpy as np

WINDOW_SIZE = 1
NUM_ROWS = 4

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
        if touches[row] > 0.4:
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
    def __init__(self, sh_image_and_landmarks, sh_touches):
        super(TouchDetector, self).__init__()
        self.stop_flg = False
        self.sh_image_and_landmarks = sh_image_and_landmarks
        self.sh_touches = sh_touches
        self.model = tf.saved_model.load('./predict_araimodel_6464_statefultrue')
        # self.model.compile()

        #mycode
        self.output = [[0]*WINDOW_SIZE for _ in range(4)]
        self.output_float = [np.array([0.0]*WINDOW_SIZE) for _ in range(4)]
    
    def run(self):
        # In thread
        print('TOUCH DETECTOR START')
        while not self.stop_flg:
            logging('TouchDetectorLoopLog', None)
            image_and_landmarks = self.sh_image_and_landmarks.try_get()
            if image_and_landmarks is None:
                time.sleep(0.02)
            else:
                image, landmarks = image_and_landmarks
                cropped_images = preprocessing.crop(image, landmarks)

                if cropped_images is None:
                    logging('TouchDetectLog', None)
                    continue

                touches = self.model([
                    image.reshape((1,1,64,64,3)) for image in cropped_images
                ])
                touches = [t.numpy() for t in touches]

                #mycode
                process_values(touches, self.output, self.output_float)
                logging('TouchDetectLog', [t[int((WINDOW_SIZE-1)/2)].item() for t in self.output_float])
                #print(t[int((WINDOW_SIZE-1)/2)].item() for t in self.output_float])
                self.sh_touches.set([t[int((WINDOW_SIZE-1)/2)] > 0.5 for t in self.output])
                add_result([t[int((WINDOW_SIZE-1)/2)] for t in self.output_float])

                #logging('TouchDetectLog', [t[0][0][0].item() for t in touches])
                #self.sh_touches.set([t[0][0][0] > 0.5 for t in touches])
                #add_result([t[0][0][0] for t in touches])
        print('TOUCH DETECTOR END')
    
    def stop(self):
        self.stop_flg = True