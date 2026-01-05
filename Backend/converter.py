import numpy as np
import warnings

warnings.simplefilter('error')

# 標準化
def normal(data):
    data = data.astype('float32') / 255

    R,G,B = np.dsplit(data, 3)
    R = np.squeeze(R)
    #R = (R - np.mean(R)) / np.std(R)*0.166+0.5
    std = np.std(R)
    if std < 1e-6:
        R = np.zeros_like(R) + 0.5
    else:
        R = (R - np.mean(R)) / std * 0.166 + 0.5

    G = np.squeeze(G)
    #G = (G - np.mean(G)) / np.std(G)*0.166+0.5
    std = np.std(G)
    if std < 1e-6:
        G = np.zeros_like(G) + 0.5
    else:
        G = (G - np.mean(G)) / std*0.166+0.5

    B = np.squeeze(B)
    std = np.std(B)
    if std < 1e-6:
        B = np.zeros_like(B) + 0.5
    else:
        B = (B - np.mean(B)) / std*0.166+0.5
    #B = (B - np.mean(B)) / np.std(B)*0.166+0.5

    data = np.stack([R, G, B], 2)
    data = np.clip(data, 0.0, 1.0)

    return data