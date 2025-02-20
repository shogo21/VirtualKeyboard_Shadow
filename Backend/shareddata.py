import queue
from logger import logging

# 以下を満たすスレッドセーフな共有データ
# ・setでデータを更新できる
# ・getでデータを取り出せる
# ・一度getで取り出すと，再びsetされるまで取り出せなくなる

class SharedData():

    def __init__(self, name):
        self.name = name
        self.queue = queue.Queue(maxsize=1)

    def set(self, data):
        try:
            self.queue.get(block=False)
            logging('SharedDataLog', {"name": self.name, "message": "set(update)"})
        except queue.Empty:
            logging('SharedDataLog', {"name": self.name, "message": "set"})
        self.queue.put(data)

    def get(self, timeout=None):
        self.queue.get(block=True, timeout=timeout)

    def try_get(self):
        try:
            data = self.queue.get(block=False)
            logging('SharedDataLog', {"name": self.name, "message": "get"})
            return data
        except queue.Empty:
            logging('SharedDataLog', {"name": self.name, "message": "get(failed)"})
            return None
