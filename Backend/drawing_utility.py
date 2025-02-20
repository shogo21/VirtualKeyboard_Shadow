# refer to mediapipe.python.solutions.drawing_styles

import mediapipe as mp
mp_drawing = mp.solutions.drawing_utils
mp_drawing_styles = mp.solutions.drawing_styles
mp_hands = mp.solutions.hands

from mediapipe.python.solutions import hands_connections
from mediapipe.python.solutions.drawing_utils import DrawingSpec
from mediapipe.python.solutions.hands import HandLandmark

_RADIUS = 1
_GRAY = (128, 128, 128)

# Hands
_THICKNESS_WRIST_MCP = 1
_THICKNESS_FINGER = 1
_THICKNESS_DOT = 1

# Hand landmarks
_PALM_LANMARKS = (HandLandmark.WRIST, HandLandmark.THUMB_CMC,
                  HandLandmark.INDEX_FINGER_MCP, HandLandmark.MIDDLE_FINGER_MCP,
                  HandLandmark.RING_FINGER_MCP, HandLandmark.PINKY_MCP)
_THUMP_LANDMARKS = (HandLandmark.THUMB_MCP, HandLandmark.THUMB_IP,
                    HandLandmark.THUMB_TIP)
_INDEX_FINGER_LANDMARKS = (HandLandmark.INDEX_FINGER_PIP,
                           HandLandmark.INDEX_FINGER_DIP,
                           HandLandmark.INDEX_FINGER_TIP)
_MIDDLE_FINGER_LANDMARKS = (HandLandmark.MIDDLE_FINGER_PIP,
                            HandLandmark.MIDDLE_FINGER_DIP,
                            HandLandmark.MIDDLE_FINGER_TIP)
_RING_FINGER_LANDMARKS = (HandLandmark.RING_FINGER_PIP,
                          HandLandmark.RING_FINGER_DIP,
                          HandLandmark.RING_FINGER_TIP)
_PINKY_FINGER_LANDMARKS = (HandLandmark.PINKY_PIP, HandLandmark.PINKY_DIP,
                           HandLandmark.PINKY_TIP)
_HAND_LANDMARK_STYLE = {
    _PALM_LANMARKS:
        DrawingSpec(
            color=_GRAY, thickness=_THICKNESS_DOT, circle_radius=_RADIUS),
    _THUMP_LANDMARKS:
        DrawingSpec(
            color=_GRAY, thickness=_THICKNESS_DOT, circle_radius=_RADIUS),
    _INDEX_FINGER_LANDMARKS:
        DrawingSpec(
            color=_GRAY, thickness=_THICKNESS_DOT, circle_radius=_RADIUS),
    _MIDDLE_FINGER_LANDMARKS:
        DrawingSpec(
            color=_GRAY, thickness=_THICKNESS_DOT, circle_radius=_RADIUS),
    _RING_FINGER_LANDMARKS:
        DrawingSpec(
            color=_GRAY, thickness=_THICKNESS_DOT, circle_radius=_RADIUS),
    _PINKY_FINGER_LANDMARKS:
        DrawingSpec(
            color=_GRAY, thickness=_THICKNESS_DOT, circle_radius=_RADIUS),
}

# Hands connections
_HAND_CONNECTION_STYLE = {
    hands_connections.HAND_PALM_CONNECTIONS:
        DrawingSpec(color=_GRAY, thickness=_THICKNESS_WRIST_MCP),
    hands_connections.HAND_THUMB_CONNECTIONS:
        DrawingSpec(color=_GRAY, thickness=_THICKNESS_FINGER),
    hands_connections.HAND_INDEX_FINGER_CONNECTIONS:
        DrawingSpec(color=_GRAY, thickness=_THICKNESS_FINGER),
    hands_connections.HAND_MIDDLE_FINGER_CONNECTIONS:
        DrawingSpec(color=_GRAY, thickness=_THICKNESS_FINGER),
    hands_connections.HAND_RING_FINGER_CONNECTIONS:
        DrawingSpec(color=_GRAY, thickness=_THICKNESS_FINGER),
    hands_connections.HAND_PINKY_FINGER_CONNECTIONS:
        DrawingSpec(color=_GRAY, thickness=_THICKNESS_FINGER)
}

hand_landmark_style = {}
for k, v in _HAND_LANDMARK_STYLE.items():
    for landmark in k:
        hand_landmark_style[landmark] = v

hand_connection_style = {}
for k, v in _HAND_CONNECTION_STYLE.items():
    for connection in k:
        hand_connection_style[connection] = v

def landmarks(image, landmarks):
    mp_drawing.draw_landmarks(
        image,
        landmarks,
        mp_hands.HAND_CONNECTIONS,
        hand_landmark_style,
        hand_connection_style)