import numpy as np
import warnings

warnings.simplefilter('error')

# 標準化
def normal(data):
    data = data.astype('float32')

    R,G,B = np.dsplit(data, 3)
    R = np.squeeze(R)
    R = (R - np.mean(R)) / np.std(R)*0.166+0.5

    G = np.squeeze(G)
    G = (G - np.mean(G)) / np.std(G)*0.166+0.5

    B = np.squeeze(B)
    B = (B - np.mean(B)) / np.std(B)*0.166+0.5

    data = np.stack([R, G, B], 2)
    data = np.clip(data, 0.0, 1.0)

    return data