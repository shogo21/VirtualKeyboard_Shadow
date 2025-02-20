import datetime
import csv
import json
import math
import inspect
import cv2
import os

# ログ残す処理
# <日時(ミリ秒まで)>, <Front/Back>, <ログ種別(クラス)>, <呼び出し元情報>, <実データ>

file_name = ".\\logs\\back_logs ({}).csv"
logs = []
video_name = ".\\logs\\record ({}).avi"
rec = None
frame_count = 0

datestr = datetime.datetime.now().strftime('%Y-%m-%d %H-%M-%S')

def output():
    with open(file_name.format(datestr), 'w', newline='') as f:
        writer = csv.writer(f)
        writer.writerows(logs)
    if rec is not None:
        rec.release()

def logging(title, data):
    time = datetime.datetime.now().strftime("%Y/%m/%d %H:%M:%S:%f")
    caller = inspect.stack()[1]
    if "self" in caller.frame.f_locals:
        src_name = caller.frame.f_locals["self"].__class__.__name__
    else:
        src_name = os.path.splitext(os.path.basename(caller.filename))[0] + '/' + caller.function
    logs.append([time, "Back", title, src_name, json.dumps(data)])

def recording(_frame):
    global rec, frame_count
    frame = _frame.copy()
    height, width, _ = frame.shape
    if rec is None:
        rec = cv2.VideoWriter(video_name.format(datestr), 1145656920, 30, (width, height), True)
    cv2.putText(frame, str(frame_count), (0, height-15), cv2.FONT_HERSHEY_SIMPLEX, 1.0, (0, 255, 0), 1)
    logging('VideoRecordingLog', {"frame": frame_count})
    rec.write(frame)
    frame_count += 1