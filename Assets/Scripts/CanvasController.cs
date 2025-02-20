using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CanvasController : MonoBehaviour
{
    private ImageReceiver imageReceiver;
    private SharedData<Color32[]> sh_background;
    private SharedData<Color32[]> sh_foreground;

    private RawImage rawImage;
    private Texture2D background;
    private Texture2D foreground;
    private Color32[] colors;

    private ARMarkerDetector detector;

    void Start()
    {
        this.rawImage = GetComponent<RawImage>();
        this.background = new Texture2D(640, 480);
        this.rawImage.texture = this.background;
        this.foreground = new Texture2D(640, 480);
        GameObject.Find("Canvas/Foreground").GetComponent<RawImage>().texture = this.foreground;
        this.colors = new Color32[0];

        this.sh_background = new SharedData<Color32[]>();
        this.sh_foreground = new SharedData<Color32[]>();
        this.imageReceiver = new ImageReceiver(this.sh_background, this.sh_foreground);
        this.imageReceiver.Start();

        this.detector = GameObject.Find("ARMarkerDetecter").GetComponent<ARMarkerDetector>();
        this.detector.WakeUp(this.background);
    }

    void Update()
    {
        if (this.sh_background.TryGet(out this.colors))
        {
            this.background.SetPixels32(this.colors);
            this.background.Apply();
            this.detector.TextureUpdated();
        }
        if (this.sh_foreground.TryGet(out this.colors))
        {
            this.foreground.SetPixels32(MultiplyTransparency(0.5f, this.colors));
            this.foreground.Apply();
        }
    }

    private Color32[] MultiplyTransparency(float rate, Color32[] colors)
    {
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i].a = (byte)(colors[i].a * rate);
        }
        return colors;
    }

    void OnDestroy()
    {
        this.imageReceiver.Stop();
        Logger.Output();
    }
}
