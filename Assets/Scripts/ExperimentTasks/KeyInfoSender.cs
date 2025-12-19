//１フレーム分の29枚の画像を一斉におくるコード
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Text;

public class KeyInfoSender : ThreadRunner
{
    private NamedPipeServer pipe;
    private SharedData<uint> frame_id;
    private string pipeName = "KeyInfoPipe";
    private readonly object sendLock = new object();
    static readonly byte[] MAGIC = Encoding.ASCII.GetBytes("KSF1PIPE");


    public KeyInfoSender(SharedData<uint> frame_id)
    {
        this.frame_id = frame_id;
        this.pipe = new NamedPipeServer(this.pipeName);
    }

    /*public void EnqueueFrame(List<byte[]> cropsBytes)
    {
        // 受け取ったフレームをキューに積む（スレッド安全）
        //this.framesQueue.Enqueue(cropsBytes);
        this.latestFrame = cropsBytes; // 上書き
    }*/

    protected override void Run()
    {
        try
        {
            this.pipe.WakeUp();
        }
        catch (Exception e)
        {
            //UnityLogger.Log("Pipe WakeUp failed: " + e.Message);
            return;
        }
        while (true)
        {
            if (token.IsCancellationRequested) break;
            if (this.pipe.status == NamedPipeServer.Status.Connected)
            {
                if (this.frame_id.TryGet(out uint fid))
                {
                    //UnityLogger.Log("frame_id: " + fid);
                    TrySendKeyPos(fid);
                }
            }
        }
    }

    public void TrySendKeyPos(uint fId)
    {
        // ---- 二重送信・割り込み防止 ----
        lock (sendLock)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    ms.Write(MAGIC, 0, MAGIC.Length);

                    ms.Write(BitConverter.GetBytes(fId), 0, 4); // uint32

                    // ★ 一気に送信（超重要）
                    this.pipe.Write(ms.ToArray());
                }
            }
            catch (Exception e)
            {
                //UnityLogger.Log("[SendOneFrame] ERROR: " + e.Message);
            }
        }
    }
}