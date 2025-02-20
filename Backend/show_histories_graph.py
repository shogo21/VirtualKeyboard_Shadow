from cProfile import label
import json
from matplotlib import pyplot as plt

with open('.\hist.json') as f:
    histories = json.load(f)

titles = list(set([rec[0]+"_"+rec[1] for rec in histories]))

print(titles)

result = {}
for title in titles:
    result[title] = [0]

for rec in histories:
    for title in titles:
        if title == rec[0]+"_"+rec[1]:
            result[title].append(result[title][-1]+1)
        else:
            result[title].append(result[title][-1])

for title in sorted(titles):
    if title == "image1_get_failed":
        continue
    if title == "landmarks_get_failed":
        continue
    if title == "touches_get_failed":
        continue
    if "get_failed" in title:
        plt.plot(result[title], label=title, linestyle="dashdot")
    elif "set_failed" in title:
        plt.plot(result[title], label=title, linestyle="dotted")
    elif "get" in title:
        plt.plot(result[title], label=title, linestyle="dashed")
    else:
        plt.plot(result[title], label=title)

for title in titles:
    print(title, result[title][-1])

plt.grid(axis="y")
plt.legend()
plt.show()