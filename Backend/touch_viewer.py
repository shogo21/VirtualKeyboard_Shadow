import matplotlib.pyplot as plt

DRAW_RANGE = 50
LABELS = ['index', 'middle', 'ring', 'little']
touch_results = [[0]*DRAW_RANGE for i in range(4)]
changed = True

def add_result(res):
    global changed
    for i in range(4):
        del touch_results[i][0]
        touch_results[i].append(res[i])
    changed = True

class TouchViewer():
    def __init__(self):
        plt.ion()
        plt.ylim([0,1])
        plt.tick_params(labelbottom=False)
        self.lines = []
        for i in range(4):
            self.lines.append(plt.plot(list(range(DRAW_RANGE)), touch_results[i], label=LABELS[i])[0])
        plt.legend(loc='upper left')

    def __del__(self):
        plt.close()

    def replot(self):
        global changed
        if changed:
            for i in range(4):
                self.lines[i].set_ydata(touch_results[i])
            changed = False
            plt.pause(0.001)

if __name__ == "__main__":
    import time
    import random
    viewer = TouchViewer()
    while True:
        viewer.replot()
        add_result([random.random(), random.random(), random.random(), random.random()])
        time.sleep(0.02)
