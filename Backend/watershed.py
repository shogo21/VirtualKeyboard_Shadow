import cv2
import numpy as np

UNKNOWN_LABEL = 0
FOREGROUND_LABEL = 1
BACKGROUND_LABEL = 2

def hand_mask(image, hand_landmarks):
    height, width, _ = image.shape

    points_x = []
    points_y = []
    for mark in hand_landmarks.landmark:
        points_x.append(int(width*mark.x))
        points_y.append(int(height*mark.y))
    
    hand_rect = [min(points_y)-50, max(points_y)+50, min(points_x)-50, max(points_x)+50]
    palm_rect = np.array([[points_x[0], points_y[0]],
                    [points_x[5], points_y[5]],
                    [points_x[9], points_y[9]],
                    [points_x[13], points_y[13]],
                    [points_x[17], points_y[17]]])
    finger_line = [[(points_x[0], points_y[0]), (points_x[1], points_y[1])], [(points_x[1], points_y[1]), (points_x[2], points_y[2])], [(points_x[2], points_y[2]), (points_x[3], points_y[3])],
                    [(points_x[5], points_y[5]), (points_x[6], points_y[6])], [(points_x[6], points_y[6]), (points_x[7], points_y[7])],
                    [(points_x[9], points_y[9]), (points_x[10], points_y[10])], [(points_x[10], points_y[10]), (points_x[11], points_y[11])],
                    [(points_x[13], points_y[13]), (points_x[14], points_y[14])], [(points_x[14], points_y[14]), (points_x[15], points_y[15])],
                    [(points_x[18], points_y[18]), (points_x[19], points_y[19])], [(points_x[19], points_y[19]), (points_x[20], points_y[20])],
                    ]

    tagmap = np.ones((height, width), dtype=np.int32) * BACKGROUND_LABEL
    tagmap[hand_rect[0]:hand_rect[1], hand_rect[2]:hand_rect[3]] = UNKNOWN_LABEL
    tagmap = cv2.fillConvexPoly(tagmap, palm_rect, FOREGROUND_LABEL)
    for line in finger_line:
        tagmap = cv2.line(tagmap, line[0], line[1], FOREGROUND_LABEL, 1)
    
    tagmap = cv2.watershed(image, tagmap)

    mask = np.zeros_like(tagmap, dtype='uint8')
    mask[tagmap == FOREGROUND_LABEL] = 255
    mask[tagmap != FOREGROUND_LABEL] = 0
    return mask
