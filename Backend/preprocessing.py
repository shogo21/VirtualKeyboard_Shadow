import math
import numpy as np
from PIL import Image
import cv2
import converter
import mediapipe as mp
POS = mp.solutions.hands.HandLandmark

OUT_SIZE = 64

def calc_bone_length(landmarks, width, height, fingertip_id):
    length = 0
    for joint in [0,1,2]:
        length += math.sqrt(
            pow(width*(landmarks[fingertip_id-joint].x - landmarks[fingertip_id-joint-1].x), 2) \
            + pow(height*(landmarks[fingertip_id-joint].y - landmarks[fingertip_id-joint-1].y), 2) \
            + pow(width*(landmarks[fingertip_id-joint].z - landmarks[fingertip_id-joint-1].z), 2)
        )
    return length

def crop_by_key(base_image, corners):
    src = np.array([
        corners[1],
        corners[0],
        corners[3],
        corners[2]
        ], dtype=np.float32)

    dst = np.array([
        [0, 0],
        [OUT_SIZE-1, 0],
        [OUT_SIZE-1, OUT_SIZE-1],
        [0, OUT_SIZE-1]
    ], dtype=np.float32)

    # 射影変換行列
    M = cv2.getPerspectiveTransform(src, dst)

    # Warp（画像外は黒で埋める）
    cropped = cv2.warpPerspective(
        base_image,
        M,
        (OUT_SIZE, OUT_SIZE),
        flags=cv2.INTER_LINEAR,
        borderMode=cv2.BORDER_CONSTANT,
        borderValue=(0, 0, 0)
    )

    """cropped = Image.fromarray(cropped)
    cropped = cropped.resize((64, 64))
    cropped = np.asarray(cropped).astype('float32')
    cropped = converter.normal(cropped)"""

    return cropped
