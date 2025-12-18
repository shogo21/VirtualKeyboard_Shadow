#29枚が一斉に送られてきた際に受け取るコード
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
            chunk = pipe.read(size - len(data))
            if not chunk:
                return None
            data += chunk
        return data

    def read_until_magic(self, pipe):   
        buf = b''
        while True:
            b = pipe.read(1)
            if not b:
                return False
            buf += b   
            if len(buf) > MAGIC_LEN:
                buf = buf[-MAGIC_LEN:]
            if buf == MAGIC:
                return True

    def read_one_frame(self, pipe):
        #Unity から 1 フレーム分の全キー画像を受信して Pillow Image リストを返す
        ok = self.read_until_magic(pipe)
        if not ok:
            return None, None

        # ---- FRAME_SIZE ----
        size_bytes = self.read_exact(pipe, 4)
        if not size_bytes:
            return None, None
        frame_size = struct.unpack("<I", size_bytes)[0]
        print(f"frame_size: {frame_size}")

        # ---- PAYLOAD ----
        payload = self.read_exact(pipe, frame_size)
        if not payload or len(payload) != frame_size:
            return None, None

        offset = 0

        # フレーム番号
        """frame_data = self.read_exact(self.pipe, 4)
        if not frame_data:
            return None, None
        frame_num = struct.unpack("<I", frame_data)[0]

        # ---- ③ 画像枚数 ----
        count_data = self.read_exact(pipe, 4)
        if not count_data:
            return None, None
        image_count = struct.unpack("<I", count_data)[0]"""
        #print(f"image_count: {image_count}")

        # Frame ID
        frame_id = struct.unpack_from("<I", payload, offset)[0]
        offset += 4

        # Image count
        image_count = struct.unpack_from("<I", payload, offset)[0]
        offset += 4
        print(f"image_count: {image_count}")

        images = []
        for _ in range(image_count):
            size = struct.unpack_from("<I", payload, offset)[0]
            offset += 4

            if size <= 0 or size != self.raw_size:
                images.append(self.dummy_image.copy())
                offset += max(size, 0)
                continue

            raw = payload[offset:offset + size]
            offset += size

            try:
                img = Image.frombytes("RGB", (self.key_width, self.key_height), raw)
                images.append(img)
            except Exception:
                images.append(self.dummy_image.copy())

        return frame_id, images

        """for i in range(image_count):
            # Raw サイズ
            size_data = self.read_exact(self.pipe, 4)
            if not size_data:
                images.append(self.dummy_image.copy())  # サイズ読み込み失敗 → ダミー
                print(f"size is not")
                continue
            size = struct.unpack("<I", size_data)[0]
            print(f"size: {size}")

            if size <= 0 or size != self.raw_size:
                # サイズ不正 → ダミー
                images.append(self.dummy_image.copy())
                # サイズ分だけ読み飛ばす（再同期を壊さない）
                if size > 0:
                    self.read_exact(pipe, size)
                continue

            #このif文コメントアウト
            if size == 0:
                # データなし → ダミー画像
                images.append(self.dummy_image.copy())
                print("dummy append")
                continue

            # Raw データ
            raw_data  = self.read_exact(self.pipe, size)
            if not raw_data or len(raw_data) != size:
                images.append(self.dummy_image.copy())
                continue
            # Pillow で読み込み
            try:
                img = Image.frombytes("RGB", (self.key_width, self.key_height), raw_data)
                images.append(img)
            except Exception as e:
                images.append(self.dummy_image.copy())"""  # 読み込み失敗 → ダミー

        """if (self.images_per_frame > image_count):
            for _ in range(self.images_per_frame-image_count):
                images.append(self.dummy_image.copy())"""


        #return frame_id, images

    def save_frame_images(self, frame_num, images):
        #29枚のキー画像を保存する
        if (frame_num % 10 == 0 and frame_num >= 50):
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