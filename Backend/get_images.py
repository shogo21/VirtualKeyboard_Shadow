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

        self.current_frame = None
        self.buffer = []  # 29枚たまるまで入れておく

    def read_one_image(self):
        """Unity から PNG 1枚を受信して Pillow Image を返す"""

        # フレーム番号
        frame_data = self.pipe.read(4)
        if not frame_data:
            return None, None
        frame_num = struct.unpack("<I", frame_data)[0]

        # PNG サイズ
        size_data = self.pipe.read(4)
        if not size_data:
            return None, None
        size = struct.unpack("<I", size_data)[0]

        # PNG データ
        png_data = self.pipe.read(size)
        if not png_data:
            return None, None

        # Pillow で読み込み
        image = Image.open(io.BytesIO(png_data))
        return frame_num, image

    def save_frame_images(self, frame_num, images):
        """29枚のキー画像を保存する"""
        save_dir = f"Backend/image_test/frame_{frame_num}"
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
        """常に受信し続け、29枚たまったら保存"""
        print("MultiImage Receiver START.")
 
        while not self.stop_flg:
            frame_num, multi_img = self.read_one_image()
            if multi_img is None:
                continue

            # 最初の画像なら frame_num 記録
            if self.current_frame is None:
                self.current_frame = frame_num

            # フレームが変わった時は、前のバッファを保存してリセット
            if frame_num != self.current_frame:
                if len(self.buffer) == self.images_per_frame:
                    self.save_frame_images(self.current_frame, self.buffer)
                else:
                    print(f"Warning: frame {self.current_frame} had only {len(self.buffer)} images")

                # 新しいフレーム開始
                self.buffer = []
                self.current_frame = frame_num

            # 画像をバッファに追加
            self.buffer.append(multi_img)

            # 29枚揃ったら保存
            if len(self.buffer) == self.images_per_frame:
                self.save_frame_images(self.current_frame, self.buffer)
                self.buffer = []  # バッファリセット
        print("MultiImage Receiver STOP")


    def stop(self):
        self.stop_flg = True