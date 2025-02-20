using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using UnityEngine.UI;

public class TimerStateLog
{
    [SerializeField] private string state;
    [SerializeField] private int pick_index;

    public TimerStateLog(string _state, int _pick_index)
    {
        this.state = _state;
        this.pick_index = _pick_index;
    }
}

public class ButtonLog
{
    [SerializeField] private float[] position = new float[2];
    [SerializeField] private float angle;
    [SerializeField] private float size_dots;
    [SerializeField] private float size_mm;
    [SerializeField] private float index;

    public ButtonLog(Vector2 _pos, float _angle, float _size_dots, float _size_mm, float _index)
    {
        this.position[0] = _pos.x;
        this.position[1] = _pos.y;
        this.angle = _angle;
        this.size_dots = _size_dots;
        this.size_mm = _size_mm;
        this.index = _index;
    }
}

public class TouchToButtonLog
{
    [SerializeField] private int index;
    [SerializeField] private bool is_picked;
    [SerializeField] private float[] position = new float[2];

    public TouchToButtonLog(int _index, bool _is_picked, Vector2 _pos)
    {
        this.position[0] = _pos.x;
        this.position[1] = _pos.y;
        this.index = _index;
        this.is_picked = _is_picked;
    }
}