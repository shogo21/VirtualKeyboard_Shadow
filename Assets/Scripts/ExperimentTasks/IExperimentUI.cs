using System.Collections;
using System.Collections.Generic;
using UnityEngine;

interface IExperimentUI
{
    void CalcHoverKey(Vector2[] fingertipAnchoredPositions);
    void Press(int index);
    void Release(int index);
    void NotifyWristPosition(Vector2 pos);
}
