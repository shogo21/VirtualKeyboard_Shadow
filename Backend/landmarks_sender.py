from logger import logging
from namedpipe import NamedPipeClient
from threading import Thread
import time
import struct

class LandmarksSender(Thread):
    def __init__(self, sh_landmarks):
        super(LandmarksSender, self).__init__()
        self.stop_flg = False
        self.sh_landmarks = sh_landmarks
        self.pipe = NamedPipeClient('LandmarksPipe')
        self.pipe.connect()
    
    def run(self):
        # In thread
        print('LANDMARKS SENDER START.')
        while not self.stop_flg:
            landmarks = self.sh_landmarks.try_get()
            if landmarks is not None:
                logging('LandmarksSendingLog', landmarks)
                byte_landmarks = b''.join(
                    [struct.pack('<ff', *lm) for lm in landmarks]
                )
                self.pipe.write(byte_landmarks)
            time.sleep(0.02)
        print('LANDMARKS SENDER END.')

    def stop(self):
        self.stop_flg = True