//１フレーム分の29枚の画像を一斉におくるコード
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Text;

public class KeyboardImageSender : ThreadRunner
{
    private NamedPipeServer pipe;

    private ConcurrentQueue<List<byte[]>> framesQueue = new ConcurrentQueue<List<byte[]>>();
    private List<byte[]> latestFrame = new List<byte[]>();
    private int frameCounter = 0;
    private readonly object sendLock = new object();
    static readonly byte[] MAGIC = Encoding.ASCII.GetBytes("KSF1PIPE");


    public KeyboardImageSender(string pipeName = "MultiImagePipe")
    {
        pipe = new NamedPipeServer(pipeName);
    }

    public void EnqueueFrame(List<byte[]> cropsBytes)
    {
        // 受け取ったフレームをキューに積む（スレッド安全）
        //this.framesQueue.Enqueue(cropsBytes);
        this.latestFrame = cropsBytes; // 上書き
    }

    /*public void setFrameCount (int framecount)
    {
        this.frameCounter = framecount;
    }*/

    protected override void Run()
    {
        try
        {
            pipe.WakeUp();
        }
        catch (Exception e)
        {
            //UnityLogger.Log("Pipe WakeUp failed: " + e.Message);
            return;
        }
        while (true)
        {
            if (token.IsCancellationRequested) break;

            /*if (pipe.status != NamedPipeServer.Status.Connected)
            {
                Thread.Sleep(10);
                continue;
            }*/
            if (pipe.status == NamedPipeServer.Status.Connected)
            {
                // ---- 最新フレームを取得（差し替え式）----
                var frame = this.latestFrame;
                this.latestFrame = null; // 送信中に書き換えられない
                if (frame == null || frame.Count == 0)
                {
                    Thread.Sleep(10);
                    UnityLogger.Log("KeyboardImageSender don't get crop_image.");
                    frameCounter++;
                    continue;
                }
                else
                {
                    //UnityLogger.Log("KeyboardImageSender get crop_image.");
                    UnityLogger.Log("frame count: " + frame.Count);
                    TrySendFrame(frame, frameCounter);
                    //this.latestFrame.Clear();
                    frameCounter++;
                }
            }
        }
    }

    public void TrySendFrame(List<byte[]> frameImages, int frameId)
    {
        // ---- 二重送信・割り込み防止 ----
        lock (sendLock)
        {
            try
            {
                /*using (MemoryStream ms = new MemoryStream())
                {
                    // ---- ① MAGIC ----
                    ms.Write(BitConverter.GetBytes(0x3146534B), 0, 4); // "KSF1"

                    // ---- ② Frame ID ----
                    ms.Write(BitConverter.GetBytes(frameId), 0, 4);

                    // ---- ③ 枚数 ----
                    ms.Write(BitConverter.GetBytes(frameImages.Count), 0, 4);

                    // ---- ④ 各画像 ----
                    foreach (var img in frameImages)
                    {
                        if (img == null)
                        {
                            ms.Write(BitConverter.GetBytes(0), 0, 4);
                            continue;
                        }

                        ms.Write(BitConverter.GetBytes(img.Length), 0, 4);
                        ms.Write(img, 0, img.Length);
                    }

                    // ---- ⑤ 一気に送信（超重要） ----
                    byte[] packet = ms.ToArray();
                    pipe.Write(packet);
                    //UnityLogger.Log("Send to Python from Unity is success");
                }*/
                using (MemoryStream payload = new MemoryStream())
                {
                    // ---------- payload（FRAME_SIZE 対象） ----------
                    // Frame ID
                    payload.Write(BitConverter.GetBytes(frameId), 0, 4);

                    // Image count
                    payload.Write(BitConverter.GetBytes(frameImages.Count), 0, 4);
                    UnityLogger.Log("frameImages.Count: " + frameImages.Count);

                    // Images
                    foreach (var img in frameImages)
                    {
                        if (img == null)
                        {
                            payload.Write(BitConverter.GetBytes(0), 0, 4);
                            continue;
                        }

                        payload.Write(BitConverter.GetBytes(img.Length), 0, 4);
                        payload.Write(img, 0, img.Length);
                        UnityLogger.Log("img.Length: " + img.Length);
                    }

                    byte[] payloadBytes = payload.ToArray();

                    // ---------- packet ----------
                    using (MemoryStream packet = new MemoryStream())
                    {
                        // MAGIC
                        packet.Write(MAGIC, 0, MAGIC.Length);

                        // FRAME_SIZE
                        packet.Write(BitConverter.GetBytes(payloadBytes.Length), 0, 4);

                        // payload 本体
                        packet.Write(payloadBytes, 0, payloadBytes.Length);

                        // ★ 一気に送信（超重要）
                        pipe.Write(packet.ToArray());
                    }
                }
            }
            catch (Exception e)
            {
                //UnityLogger.Log("[SendOneFrame] ERROR: " + e.Message);
            }
        }
    }
}