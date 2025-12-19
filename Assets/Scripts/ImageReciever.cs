using UnityEngine;
using System;

public class ImageReceiver : ThreadRunner
{
    private SharedData<(uint, Color32[])> sh_background;
    private SharedData<(uint, Color32[])> sh_foreground;

    const int WIDTH = 640;
    const int HEIGHT = 480;
    const int CHANNELS = 4;

    const int FRAME_ID_SIZE = 4;
    const int IMAGE_SIZE = WIDTH * HEIGHT * CHANNELS;
    const int TOTAL_SIZE = FRAME_ID_SIZE + IMAGE_SIZE;

    public ImageReceiver(SharedData<(uint, Color32[])> sh_background, SharedData<(uint, Color32[])> sh_foreground)
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
                        byte[] buffer = pipe.Read(TOTAL_SIZE);
                        if (buffer == null) break;

                        // ===== frame_id を読む 0 means from first=====
                        uint frameId = BitConverter.ToUInt32(buffer, 0);

                        // ===== image bytes を切り出す =====
                        byte[] imageBytes = new byte[IMAGE_SIZE];
                        Buffer.BlockCopy(
                            buffer,
                            FRAME_ID_SIZE, //the position which starts reading from
                            imageBytes,
                            0,
                            IMAGE_SIZE
                        );

                        // ===== SharedData にセット =====
                        this.sh_background.Set(
                            (frameId, BytesToColorsNotMasked(imageBytes))
                        );

                        this.sh_foreground.Set(
                            (frameId, BytesToColors(imageBytes))
                        );
                        /*byte[] bytes = pipe.Read(640 * 480 * 4);
                        if (bytes == null) break;
                        this.sh_background.Set(BytesToColorsNotMasked(bytes));
                        this.sh_foreground.Set(BytesToColors(bytes));*/
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
