from collections import deque
from threading import Lock

class FrameSaveBuffer:
    def __init__(self, name):
        self.name = name
        self.maxsize = 10
        self.frames = deque()          # [(frame_id, image), ...]
        self.lock = Lock()

    def set(self, frame_id, image):
        with self.lock:
            # 末尾に追加
            self.frames.append((frame_id, image))

            # サイズ超過なら古いものを捨てる
            while len(self.frames) > self.maxsize:
                self.frames.popleft()

    def get_by_frame_id(self, target_frame_id):
        #target_frame_id に一致するフレームを返す。それ以前のフレームはすべて破棄.
        with self.lock:
            found = None
            new_frames = deque()

            for frame_id, image in self.frames:
                if frame_id < target_frame_id:
                    # いらない → 捨てる
                    continue
                elif frame_id == target_frame_id:
                    found = (frame_id, image)
                else:
                    new_frames.append((frame_id, image))

            self.frames = new_frames
            return found

    def pop_oldest(self):
        with self.lock:
            if not self.frames:
                return None
            return self.frames.popleft()