//１フレーム分の29枚の画像を一斉におくるコード
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using System.Collections.Concurrent;
using System.Threading;

public class KeyboardImageSender : ThreadRunner
{
    private NamedPipeServer pipe;

    private ConcurrentQueue<List<byte[]>> framesQueue = new ConcurrentQueue<List<byte[]>>();

    private int frameCounter;

    public KeyboardImageSender(string pipeName = "MultiImagePipe")
    {
        pipe = new NamedPipeServer(pipeName);
    }

    public void EnqueueFrame(List<byte[]> cropsBytes)
    {
        // 受け取ったフレームをキューに積む（スレッド安全）
        this.framesQueue.Enqueue(cropsBytes);
    }

    protected override void Run()
    {
        try
        {
            pipe.WakeUp();
        }
        catch (Exception e)
        {
            UnityLogger.Log("Pipe WakeUp failed: " + e.Message);
            return;
        }

        frameCounter = 0;

        while (true)
        {
            if (token.IsCancellationRequested) break;

            if (pipe.status == NamedPipeServer.Status.Connected)
            {
                frameCounter++;
                TrySendFrame();
            }
            Thread.Sleep(10);
        }
    }

    public void TrySendFrame()
    {
        if (!framesQueue.TryDequeue(out var cropsBytes))
        {
            UnityLogger.Log("KeyboardImageSender don't get crop_image.");
            return; // フレームが来てない
        }
        else
        {
            UnityLogger.Log("KeyboardImageSender get crop_image.");
        }

        if (pipe == null || pipe.status != NamedPipeServer.Status.Connected)
        {
            UnityLogger.Log("[Capture] Pipe not connected.");
            return;
        }else
        {
            UnityLogger.Log("[Capture] Pipe connected.");
        }

        try
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // 1フレーム番号を書き込む
                ms.Write(BitConverter.GetBytes(frameCounter), 0, 4);

                // 29 枚送信
                foreach (var bytes in cropsBytes)
                {
                    if (bytes == null)
                    {
                        // 安全のため 0 サイズを送る
                        ms.Write(BitConverter.GetBytes(0), 0, 4);
                        continue;
                    }

                    // サイズ
                    ms.Write(BitConverter.GetBytes(bytes.Length), 0, 4);

                    // 本体
                    ms.Write(bytes, 0, bytes.Length);
                }

                byte[] packet = ms.ToArray();
                pipe.Write(packet);
                UnityLogger.Log("Send to Python from Unity is success");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[Capture] Exception: " + ex.Message);
        }

        // ---- フレーム内のバッファをクリア（メモリ解放）----
        for (int i = 0; i < cropsBytes.Count; i++)
            cropsBytes[i] = null;
        cropsBytes.Clear();
    }
}