import time

log_path = "unity_log.txt"  # Unity が書くファイル

def follow(path):
    with open(path, "r", encoding="utf-8") as f:
        f.seek(0, 2)  # ファイル末尾に移動（tail -f と同じ）

        while True:
            line = f.readline()
            if line:
                yield line
            else:
                time.sleep(0.1)

for line in follow(log_path):
    print("[UNITY]", line, end="")