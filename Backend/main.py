import cv2
import time

import logger

from hand_tracker import HandTracker
from image_sender import ImageSender
from landmarks_sender import LandmarksSender

from shareddata import SharedData
from fisheye_undistort import undistort
from touch_detector import TouchDetector
from touches_sender import TouchesSender
from touch_viewer import TouchViewer

CAMERA_INDEX = 0

capture = cv2.VideoCapture(CAMERA_INDEX)

print(capture.get(cv2.CAP_PROP_FPS))

sh_image1 = SharedData("image1")
sh_image2 = SharedData("image2")
sh_landmarks1 = SharedData("landmarks1")
sh_landmarks2 = SharedData("landmarks2")
sh_image_and_landmarks = SharedData("image_and_land")
sh_touches = SharedData("touches")

image_sender = ImageSender(sh_image1, sh_landmarks2)
landmarks_sender = LandmarksSender(sh_landmarks1)
touches_sender = TouchesSender(sh_touches)
image_sender.start()
landmarks_sender.start()
touches_sender.start()

tracker = HandTracker(sh_image2, sh_landmarks1, sh_landmarks2, sh_image_and_landmarks)
detector = TouchDetector(sh_image_and_landmarks, sh_touches)
tracker.start()
detector.start()

viewer = TouchViewer()

try:
    while True:
        ret, frame = capture.read()
        frame = cv2.flip(frame, -1)
        frame = undistort(frame)
        frame = cv2.flip(frame, -1)

        logger.recording(frame)

        sh_image1.set(frame)
        sh_image2.set(frame)

        viewer.replot()

        time.sleep(0.02)

        if not image_sender.is_alive():
            break
        if not landmarks_sender.is_alive():
            break
        if not touches_sender.is_alive():
            break
        if not tracker.is_alive():
            break
        if not detector.is_alive():
            break
except:
    import traceback
    traceback.print_exc()

image_sender.stop()
landmarks_sender.stop()
touches_sender.stop()
tracker.stop()
detector.stop()

logger.output()