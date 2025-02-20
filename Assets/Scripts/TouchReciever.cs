using UnityEngine;
using System;

public class TouchReceiver : ThreadRunner
{
    private SharedData<bool[]> sh_touches;

    public TouchReceiver(SharedData<bool[]> sh_touches)
    {
        this.sh_touches = sh_touches;
    }

    protected override void Run()
    {
        using (NamedPipeServer pipe = new NamedPipeServer("TouchesPipe"))
        {
            var _ = pipe.WakeUp();
            while (true)
            {
                try
                {
                    if (this.token.IsCancellationRequested) break;
                    if (pipe.status == NamedPipeServer.Status.Connected)
                    {
                        byte[] bytes = pipe.Read(1 * 4);
                        if (bytes == null) break;
                        this.sh_touches.Set(BytesToBooleans(bytes));
                    }
                }
                catch (Exception e)
                {
                    break;
                }
            }
        }
    }

    private bool[] BytesToBooleans(byte[] bytes)
    {
        bool[] booleans = new bool[bytes.Length];

        for (int i = 0; i < booleans.Length; i++)
        {
            booleans[i] = BitConverter.ToBoolean(bytes, i);
        }

        return booleans;
    }
}
