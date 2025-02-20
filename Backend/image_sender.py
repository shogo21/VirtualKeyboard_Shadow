import numpy as np
from namedpipe import NamedPipeClient
import watershed
import drawing_utility
from threading import Thread
import time

class ImageSender(Thread):
    def __init__(self, sh_image, sh_landmarks):
        super(ImageSender, self).__init__()
        self.stop_flg = False
        self.sh_image = sh_image
        self.sh_landmarks = sh_landmarks
        self.pipe = NamedPipeClient('ImagePipe')
        self.pipe.connect()
    
    def run(self):
        # In thread
        print('IMAGE SENDER START.')
        hand_landmarks = None
        while not self.stop_flg:
            image = self.sh_image.try_get()
            if image is not None:

                temp = self.sh_landmarks.try_get()
                if temp is None:
                    pass
                elif len(temp) == 0:
                    hand_landmarks = None
                else:
                    hand_landmarks = temp[0]

                if hand_landmarks is None:
                    mask = np.zeros(image.shape[:2], dtype='uint8')
                else:
                    mask = watershed.hand_mask(image, hand_landmarks)
                    drawing_utility.landmarks(image, hand_landmarks)

                concat_image = np.concatenate([image[:,:,[2,1,0]], mask[:,:,np.newaxis]], axis=2)
                concat_image = np.flipud(concat_image)
                byte_image = concat_image.tobytes()
                self.pipe.write(byte_image)
            time.sleep(0.02)
        print('IMAGE SENDER END.')
    
    def stop(self):
        self.stop_flg = True