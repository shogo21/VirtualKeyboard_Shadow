using UnityEngine;
using System;

public class LandmarksReceiver : ThreadRunner
{
    private SharedData<Vector2[]> sh_landmarks;

    public LandmarksReceiver(SharedData<Vector2[]> sh_landmarks)
    {
        this.sh_landmarks = sh_landmarks;
    }

    protected override void Run()
    {
        using (NamedPipeServer pipe = new NamedPipeServer("LandmarksPipe"))
        {
            var _ = pipe.WakeUp();
            while (true)
            {
                try
                {
                    if (this.token.IsCancellationRequested) break;
                    if (pipe.status == NamedPipeServer.Status.Connected)
                    {
                        byte[] bytes = pipe.Read(5 * 2 * 4);
                        if (bytes == null) break;
                        this.sh_landmarks.Set(BytesToVectors(bytes));
                    }
                }
                catch (Exception e)
                {
                    break;
                }
            }
        }
    }

    private Vector2[] BytesToVectors(byte[] bytes)
    {
        Vector2[] vectors = new Vector2[bytes.Length / 8];

        for (int i = 0; i < vectors.Length; i++)
        {
            vectors[i] = new Vector2(
                BitConverter.ToSingle(bytes, i * 8),
                BitConverter.ToSingle(bytes, i * 8 + 4));
        }

        return vectors;
    }
}
