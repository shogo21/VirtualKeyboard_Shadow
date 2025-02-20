import math
import numpy as np
from PIL import Image
import cv2
import converter
import mediapipe as mp
POS = mp.solutions.hands.HandLandmark

finger_tip_id = [POS.INDEX_FINGER_TIP, POS.MIDDLE_FINGER_TIP, POS.RING_FINGER_TIP, POS.PINKY_TIP]
finger_dip_id = [POS.INDEX_FINGER_DIP, POS.MIDDLE_FINGER_DIP, POS.RING_FINGER_DIP, POS.PINKY_DIP]

def calc_bone_length(landmarks, width, height, fingertip_id):
    length = 0
    for joint in [0,1,2]:
        length += math.sqrt(
            pow(width*(landmarks[fingertip_id-joint].x - landmarks[fingertip_id-joint-1].x), 2) \
            + pow(height*(landmarks[fingertip_id-joint].y - landmarks[fingertip_id-joint-1].y), 2) \
            + pow(width*(landmarks[fingertip_id-joint].z - landmarks[fingertip_id-joint-1].z), 2)
        )
    return length

def crop(image, landmarks):
    height, width, _ = image.shape
    cropped_images = []
    for fingertip_id in [POS.INDEX_FINGER_TIP, POS.MIDDLE_FINGER_TIP, POS.RING_FINGER_TIP, POS.PINKY_TIP]:
        finger_length = calc_bone_length(landmarks, width, height, fingertip_id)
        finger_width = finger_length * 0.1777
        margin = finger_width * 1.5# 周りの黒い部分を確保

        try:
            expanded = np.zeros((int(height+2*margin), int(width+2*margin), 3), dtype='uint8')
            expanded[
                int(margin):int(height+margin),
                int(margin):int(width+margin)
            ] = image

            cropped = expanded[
                int(height*landmarks[fingertip_id].y): int(height*landmarks[fingertip_id].y + 2*margin),
                int(width*landmarks[fingertip_id].x): int(width*landmarks[fingertip_id].x + 2*margin),
            ]

            radian = math.degrees(math.atan2(
                height*(landmarks[fingertip_id].y - landmarks[fingertip_id-1].y),
                width*(landmarks[fingertip_id].x - landmarks[fingertip_id-1].x)
            ))
            trans = cv2.getRotationMatrix2D((margin, margin), radian+90, 1.0)

            cropped = cv2.warpAffine(cropped, trans, (int(margin*2), int(margin*2)))

            cropped = cropped[
                int(margin-finger_width):int(margin+finger_width),
                int(margin-finger_width):int(margin+finger_width),
            ]

            cropped = Image.fromarray(cropped)
            cropped = cropped.resize((64, 64))
            cropped = np.asarray(cropped).astype('float32')
            cropped_images.append(converter.normal(cropped))
        except Exception as e:
            print(e)
            print(cropped.shape)
            return None
    
    return cropped_images