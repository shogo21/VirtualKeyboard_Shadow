using System.Linq;
using UnityEngine;

public class KeyLog
{
    [SerializeField] string c;
    [SerializeField] Vector2 position;
    [SerializeField] float angle;
    [SerializeField] Vector2 size_dots;
    [SerializeField] float size_mm;
    public KeyLog(char _c, RectTransform _rect_transform, float _size_mm)
    {
        this.c = "" + _c;
        this.position = _rect_transform.anchoredPosition;
        this.angle = _rect_transform.localRotation.eulerAngles.z * Mathf.Deg2Rad;
        this.size_dots = _rect_transform.localScale * _rect_transform.sizeDelta;
        this.size_mm = _size_mm;
    }
}

public class FingerHoverLog
{
    [SerializeField] private Vector2[] positions;
    [SerializeField] private string[] chars;
    public FingerHoverLog(Vector2[] _positions, char[] _chars)
    {
        this.positions = _positions;
        this.chars = _chars.Select(c => "" + c).ToArray();
    }
}

public class PressedKeyLog
{
    [SerializeField] private string key;
    [SerializeField] private int index;
    public PressedKeyLog(char _key, int _index)
    {
        this.key = "" + _key;
        this.index = _index;
    }
}

public class ReleasedKeyLog
{
    [SerializeField] private int index;
    public ReleasedKeyLog(int _index)
    {
        this.index = _index;
    }
}

public class PhraseStateLog
{
    [SerializeField] private int set_index;
    [SerializeField] private int number;
    [SerializeField] private string state;
    [SerializeField] private string phrase;
    public PhraseStateLog(string _state, int _set_index, int _number, string _phrase)
    {
        this.state = _state;
        this.set_index = _set_index;
        this.number = _number;
        this.phrase = _phrase;
    }
}

public class UpdateTextLog
{
    [SerializeField] private string c;
    [SerializeField] private bool correct;
    public UpdateTextLog(string _c, bool _correct)
    {
        this.c = _c;
        this.correct = _correct;
    }
}
