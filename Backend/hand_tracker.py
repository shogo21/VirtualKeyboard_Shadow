from threading import Thread
from logger import logging
import mediapipe as mp

import time

mp_hands = mp.solutions.hands
POS = mp.solutions.hands.HandLandmark

class HandTracker(Thread):
    def __init__(self, sh_image, sh_landmarks1, sh_landmarks2, sh_image_and_landmarks):
        super(HandTracker, self).__init__()
        self.stop_flg = False
        self.sh_image = sh_image
        self.sh_landmarks1 = sh_landmarks1
        self.sh_landmarks2 = sh_landmarks2
        self.sh_image_and_landmarks = sh_image_and_landmarks
    
    def run(self):
        # In thread
        print('HAND TRACKER START.')
        with mp_hands.Hands(
            model_complexity=0,
            min_detection_confidence=0.5,
            min_tracking_confidence=0.5) as hands:
            
            while not self.stop_flg:
                logging('HandTrackerLoopLog', None)
                image = self.sh_image.try_get()
                if image is None:
                    time.sleep(0.02)
                else:
                    results = hands.process(image)

                    if not results.multi_hand_landmarks:
                        logging('HandTrackLog', [])
                        self.sh_landmarks1.set([])
                        self.sh_landmarks2.set([])
                        continue

                    hand_landmarks = self.select_center_hand(results.multi_hand_landmarks)

                    self.sh_image_and_landmarks.set((image, hand_landmarks.landmark))
                    fingertips = [
                        (hand_landmarks.landmark[POS.WRIST].x, hand_landmarks.landmark[POS.WRIST].y),
                        (hand_landmarks.landmark[POS.INDEX_FINGER_TIP].x, hand_landmarks.landmark[POS.INDEX_FINGER_TIP].y),
                        (hand_landmarks.landmark[POS.MIDDLE_FINGER_TIP].x, hand_landmarks.landmark[POS.MIDDLE_FINGER_TIP].y),
                        (hand_landmarks.landmark[POS.RING_FINGER_TIP].x, hand_landmarks.landmark[POS.RING_FINGER_TIP].y),
                        (hand_landmarks.landmark[POS.PINKY_TIP].x, hand_landmarks.landmark[POS.PINKY_TIP].y)
                    ]
                    logging('HandTrackLog', fingertips)
                    self.sh_landmarks1.set(fingertips)
                    self.sh_landmarks2.set([hand_landmarks])
        
        print('HAND TRACKER END.')
    
    def stop(self):
        self.stop_flg = True
    
    def select_center_hand(self, multi_hands):
        selected_hand = None
        min_dist = 10
        for hand in multi_hands:
            hand_center = [0, 0]
            for pos in [POS.WRIST, POS.THUMB_CMC, POS.INDEX_FINGER_MCP, POS.PINKY_MCP]:
                hand_center[0] += hand.landmark[pos].x / 4
                hand_center[1] += hand.landmark[pos].y / 4
            # dist_from_center = pow(hand_center[0]-0.5, 2) + pow(hand_center[1]-0.5, 2)
            dist_from_left = hand_center[0]
            if dist_from_left < min_dist:
                selected_hand = hand
                min_dist = dist_from_left
        return selected_hand