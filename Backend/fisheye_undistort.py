import pickle
import cv2

with open('./camera_params.pickle', mode='rb') as f:
    mtx, dist, newmtx = pickle.load(f)

mappings = dict()

def undistort(image):
    height, width, _ = image.shape
    size = (width, height)

    if size not in mappings:
        mappings[size] = cv2.fisheye.initUndistortRectifyMap(mtx, dist, None, newmtx, (640, 480), cv2.CV_16SC2)
    
    map1, map2 = mappings[size]
    return cv2.remap(image, map1, map2, cv2.INTER_CUBIC, cv2.BORDER_CONSTANT)
