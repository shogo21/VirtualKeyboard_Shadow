using UnityEngine;
using System;

public class ImageReceiver : ThreadRunner
{
    private SharedData<Color32[]> sh_background;
    private SharedData<Color32[]> sh_foreground;

    public ImageReceiver(SharedData<Color32[]> sh_background, SharedData<Color32[]> sh_foreground)
    {
        this.sh_background = sh_background;
        this.sh_foreground = sh_foreground;
    }

    protected override void Run()
    {
        using (NamedPipeServer pipe = new NamedPipeServer("ImagePipe"))
        {
            var _ = pipe.WakeUp();
            while (true)
            {
                try
                {
                    if (this.token.IsCancellationRequested) break;
                    if (pipe.status == NamedPipeServer.Status.Connected)
                    {
                        byte[] bytes = pipe.Read(640 * 480 * 4);
                        if (bytes == null) break;
                        this.sh_background.Set(BytesToColorsNotMasked(bytes));
                        this.sh_foreground.Set(BytesToColors(bytes));
                    }
                }
                catch (Exception e)
                {
                    Debug.Log(e.Message);
                    Debug.Log(e.StackTrace);
                    break;
                }
            }
            Debug.Log("Loop end");
        }
        Debug.Log("Thread end");
    }

    private Color32[] BytesToColors(byte[] bytes)
    {
        Color32[] colors = new Color32[bytes.Length / 4];

        for (int i = 0; i < colors.Length; i++)
        {
            colors[i].r = bytes[4 * i + 0];
            colors[i].g = bytes[4 * i + 1];
            colors[i].b = bytes[4 * i + 2];
            colors[i].a = bytes[4 * i + 3];
        }
        return colors;
    }

    private Color32[] BytesToColorsNotMasked(byte[] bytes)
    {
        Color32[] colors = new Color32[bytes.Length / 4];

        for (int i = 0; i < colors.Length; i++)
        {
            colors[i].r = bytes[4 * i + 0];
            colors[i].g = bytes[4 * i + 1];
            colors[i].b = bytes[4 * i + 2];
            colors[i].a = 255;
        }
        return colors;
    }
}
