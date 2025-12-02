#29枚が一斉に送られてきた際に受け取るコード
import struct
import io
import os
import pywintypes
import time
from PIL import Image
from namedpipe import NamedPipeClient
from threading import Thread

class MultiImageReceiver(Thread):
    def __init__(self):
        super(MultiImageReceiver, self).__init__()
        self.stop_flg = False
        self.images_per_frame = 29
        self.pipe = NamedPipeClient("MultiImagePipe")
        #self.pipe.connect()

    def read_exact(self, pipe, size):
        data = b''
        while len(data) < size:
            chunk = pipe.read(size - len(data))
            if not chunk:
                return None
            data += chunk
        return data


    def read_one_frame(self, pipe):
        #Unity から 1 フレーム分の全キー画像を受信して Pillow Image リストを返す
        # フレーム番号
        frame_data = self.read_exact(self.pipe, 4)
        if not frame_data:
            return None, None
        frame_num = struct.unpack("<I", frame_data)[0]

        images = []
        for _ in range(self.images_per_frame):
            # PNG サイズ
            size_data = self.read_exact(self.pipe, 4)
            if not size_data:
                return None, None
            size = struct.unpack("<I", size_data)[0]

            # PNG データ
            png_data = self.read_exact(self.pipe, size)
            if not png_data:
                return None, None
            # Pillow で読み込み
            try:
                image = Image.open(io.BytesIO(png_data))
                images.append(image)
            except Exception as e:
                return None, None  # 1枚でも失敗したらフレーム全体を無視

        return frame_num, images

    def save_frame_images(self, frame_num, images):
        #29枚のキー画像を保存する
        if (frame_num % 100 == 0):
            save_dir = f"./image_test/frame_{frame_num}"
            os.makedirs(save_dir, exist_ok=True)

            for i, multi_img in enumerate(images):
                path = os.path.join(save_dir, f"key_{i:02d}.png")
                multi_img.save(path)

            print(f"Saved frame {frame_num} ({len(images)} images)")

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

        print("MultiImage Receiver START.")

        while not self.stop_flg:
            frame_num, images = self.read_one_frame(self.pipe)
            if images is None:
                continue

            self.save_frame_images(frame_num, images)

        print("MultiImage Receiver STOP")

    def stop(self):
        self.stop_flg = True