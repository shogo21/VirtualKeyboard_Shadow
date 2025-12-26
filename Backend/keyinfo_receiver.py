import struct
import io
import os
import pywintypes
import time
from PIL import Image
from namedpipe import NamedPipeClient
from threading import Thread

MAGIC = b'KSF1PIPE'
MAGIC_LEN = len(MAGIC)

class KeyInfoReceiver(Thread):
    def __init__(self, sh_keys_pos_from_unity):
        super(KeyInfoReceiver, self).__init__()
        self.stop_flg = False
        self.sh_keys_pos_from_unity = sh_keys_pos_from_unity
        self.pipe = NamedPipeClient("KeyInfoPipe")

    def read_exact(self, size):
        data = b''
        while len(data) < size:
            chunk = self.pipe.read(size - len(data))
            if not chunk:
                return None
            data += chunk
        return data

    def read_until_magic(self):   
        buf = b''
        while True:
            b = self.pipe.read(1)
            if not b:
                return False
            buf += b   
            if len(buf) > MAGIC_LEN:
                buf = buf[-MAGIC_LEN:]
            if buf == MAGIC:
                return True

    def read_one_frame(self):
        ok = self.read_until_magic()
        if not ok:
            return None, None, None, None

        raw_frame_id = self.read_exact(4)
        if not raw_frame_id:
            return None, None, None, None
        frame_id = struct.unpack("<I", raw_frame_id)[0]

        raw_keysize = self.read_exact(4)
        if not raw_keysize:
            return None, None, None, None
        keysize = struct.unpack("<f", raw_keysize)[0]

        angles = []
        for _ in range(2):
            angle_x = self.read_exact(4)
            if not angle_x:
                return None, None, None, None
            anglex = struct.unpack("<f", angle_x)[0]
            angle_y = self.read_exact(4)
            if not angle_y:
                return None, None, None, None
            angley = struct.unpack("<f", angle_y)[0]
            angles.append((anglex, angley))
            
        keys = []
        for _ in range(29):
            x = self.read_exact(4)
            if not x:
                return None, None, None, None
            pos_x = struct.unpack("<f", x)[0]
            y = self.read_exact(4)
            if not y:
                return None, None, None, None
            pos_y = struct.unpack("<f", y)[0]
            keys.append((pos_x, pos_y))

        return frame_id, keysize, angles, keys

    def run(self):
        connected = False
        while not connected:
            try:
                self.pipe.connect()
                connected = True
            except pywintypes.error as e:
                if e.args[0] == 231:  # ERROR_PIPE_BUSY
                    print("Pipe is busy, retrying in 0.1s...")
                    time.sleep(0.1)
                else:
                    raise

        print("KeyInfo Receiver START.")

        while not self.stop_flg:
            frame_id, keysize, angles, keys = self.read_one_frame()
            """print(f"frame_id_receiver: {frame_id}")
            print(f"keysize_receiver: {keysize}")
            for i in range(2):
                print(f"{i}angles: {angles[i]}")
            for i in range(29):
                print(f"{i}key: {keys[i]}")"""

            self.sh_keys_pos_from_unity.set((frame_id, keysize, angles, keys))
            

        print("KeyInfo Receiver STOP")

    def stop(self):
        self.stop_flg = True