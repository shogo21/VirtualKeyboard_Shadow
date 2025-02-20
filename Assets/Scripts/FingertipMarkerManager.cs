using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class FingertipMarkerManager : MonoBehaviour
{
    public GameObject markerPrefab;
    private RectTransform[] markerRectTransforms;

    private SharedData<Vector2[]> sh_landmarks;
    private LandmarksReceiver landmarksReceiver;

    private SharedData<bool[]> sh_touches;
    private TouchReceiver touchReceiver;

    private float image_width, image_height;

    private IExperimentUI UI;
    private bool[] pre = { false, false, false, false };

    Vector2 wrist_position;

    void Start()
    {
        markerRectTransforms = new RectTransform[4];

        for (int i = 0; i < 4; i++)
        {
            GameObject obj = Instantiate(markerPrefab, new Vector3(0, 0, 0), new Quaternion(0, 0, 0, 0), this.transform);
            obj.SetActive(false);// マーカー非表示
            markerRectTransforms[i] = obj.GetComponent<RectTransform>();
            markerRectTransforms[i].localPosition = Vector3.zero;
        }

        this.sh_landmarks = new SharedData<Vector2[]>();
        this.landmarksReceiver = new LandmarksReceiver(this.sh_landmarks);
        this.landmarksReceiver.Start();

        this.sh_touches = new SharedData<bool[]>();
        this.touchReceiver = new TouchReceiver(this.sh_touches);
        this.touchReceiver.Start();

        this.UI = GameObject.Find("Canvas/Keyboard").GetComponent<KeyboardUI>();
        //this.UI = GameObject.Find("Canvas/Buttons").GetComponent<ButtonUI>();

        RectTransform background = GameObject.Find("Canvas/Background").GetComponent<RectTransform>();
        image_width = 640 * background.localScale.x;
        image_height = 480 * background.localScale.y;
    }


    void Update()
    {
        Vector2[] v;
        if (this.sh_landmarks.TryGet(out v))
        {
            wrist_position = new Vector2(
                (v[0].x - 0.5f) * this.image_width,
                -(v[0].y - 0.5f) * this.image_height);
            for (int i = 0; i < 4; i++)
            {
                this.markerRectTransforms[i].anchoredPosition = new Vector3(
                    (v[i+1].x - 0.5f) * this.image_width,
                    -(v[i+1].y - 0.5f) * this.image_height,
                    0);
            }
        }

        this.UI.CalcHoverKey(
            this.markerRectTransforms.Select(rt => rt.anchoredPosition).ToArray()
            );
        
        this.UI.NotifyWristPosition(this.wrist_position);

        bool[] b;
        if (this.sh_touches.TryGet(out b))
        {
            for (int i = 0; i < 4; i++)
            {
                this.markerRectTransforms[i].localScale = (b[i] ? new Vector3(2, 2, 1) : new Vector3(1, 1, 1));
                if (pre[i] == false && b[i] == true) this.UI.Press(i);
                if (pre[i] == true && b[i] == false) this.UI.Release(i);
            }
            pre = b;
        }
    }

    void OnDestroy()
    {
        this.landmarksReceiver.Stop();
        this.touchReceiver.Stop();
    }
}
