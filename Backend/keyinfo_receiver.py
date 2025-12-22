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
            return None, None

        raw_frame_id = self.read_exact(4)
        if not raw_frame_id:
            return None, None
        frame_id = struct.unpack("<I", raw_frame_id)[0]

        keys = []
        for _ in range(29):
            corners = []
            for _ in range(4):
                x = self.read_exact(4)
                if not x:
                    return None, None
                pos_x = struct.unpack("<f", x)[0]
                y = self.read_exact(4)
                if not y:
                    return None, None
                pos_y = struct.unpack("<f", y)[0]
                corners.append((pos_x, pos_y))
            keys.append(corners)

        return frame_id, keys

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
            frame_id, keys = self.read_one_frame()
            """print(f"frame_id: {frame_id}: {type(frame_id)}")
            for i in range(29):
                for j in range(4):
                    print(f"{i}key {j}corner: {keys[i][j]}")"""
            self.sh_keys_pos_from_unity.set((frame_id, keys))
            

        print("KeyInfo Receiver STOP")

    def stop(self):
        self.stop_flg = True