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

        # Unity 側のキー画像のサイズ（RawTextureData）
        self.key_width = 64
        self.key_height = 64
        self.raw_size = self.key_width * self.key_height * 3  # RGB24 → 3バイト

        # ダミー画像を生成 (64x64 黒)
        self.dummy_image = Image.new("RGB", (self.key_width, self.key_height), (0, 0, 0))

    def read_exact(self, pipe, size):
        data = b''
        while len(data) < size:
            #print("AAAAAAAA")
            chunk = pipe.read(size - len(data))
            #print("BBBBBBBBB")
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
        #print("frame_data get")

        images = []
        for i in range(self.images_per_frame):
            # Raw サイズ
            size_data = self.read_exact(self.pipe, 4)
            if not size_data:
                images.append(self.dummy_image.copy())  # サイズ読み込み失敗 → ダミー
                continue
            #print(f"size_data: {size_data}")
            size = struct.unpack("<I", size_data)[0]
            #print(f"size: {size}")

            # Raw データ
            raw_data  = self.read_exact(self.pipe, size)
            if not raw_data or len(raw_data) != size:
                images.append(self.dummy_image.copy())
                #print("Raw data is null")
                continue
            # Pillow で読み込み
            try:
                #image = Image.open(io.BytesIO(png_data))
                #image.load()  # PNG デコード
                img = Image.frombytes("RGB", (self.key_width, self.key_height), raw_data)
                images.append(img)
                #print("img append success")
            except Exception as e:
                #print("[decode error]", e)
                images.append(self.dummy_image.copy())  # 読み込み失敗 → ダミー

        return frame_num, images

    def save_frame_images(self, frame_num, images):
        #29枚のキー画像を保存する
        if (frame_num % 100 == 0 and frame_num >= 1500):
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
                continue   # フレーム番号すら読めなかった場合のみスキップ

            self.save_frame_images(frame_num, images)

        print("MultiImage Receiver STOP")

    def stop(self):
        self.stop_flg = True