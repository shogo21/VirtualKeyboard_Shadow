from collections import deque
from threading import Lock

class FrameSaveBuffer:
    def __init__(self, name):
        self.name = name
        self.maxsize = 10
        self.frames = deque() 
        self.lock = Lock()

    def set(self, frame_id, image):
        with self.lock:
            self.frames.append((frame_id, image))

            while len(self.frames) > self.maxsize:
                self.frames.popleft()

    def get_by_frame_id(self, target_frame_id):
        #target_frame_id 
        with self.lock:
            found = None
            new_frames = deque()
            if target_frame_id == None:
                return found

            for frame_id, image in self.frames:
                if frame_id < target_frame_id:
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