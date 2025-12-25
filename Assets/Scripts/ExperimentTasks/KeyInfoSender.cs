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
    private SharedData<Vector2[][]> keys_pos;
    private string pipeName = "KeyInfoPipe";
    private readonly object sendLock = new object();
    static readonly byte[] MAGIC = Encoding.ASCII.GetBytes("KSF1PIPE");
    private uint payloadSize = 4 + 29 * 4 * 2 * 4;


    public KeyInfoSender(SharedData<uint> frame_id, SharedData<Vector2[][]> keys_pos)
    {
        this.frame_id = frame_id;
        this.keys_pos = keys_pos;
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
                if (this.frame_id.TryGet(out uint fid) && this.keys_pos.TryGet(out Vector2[][] keys_rotated_pos))
                {
                    //UnityLogger.Log("frame_id: " + fid);
                    TrySendKeyPos(fid, keys_rotated_pos);
                }
            }
        }
    }

    public void TrySendKeyPos(uint fId, Vector2[][] keys_total_pos)
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

                    // ---- 座標データ ----
                    for (int i = 0; i < keys_total_pos.Length; i++)
                    {
                        Vector2[] corners = keys_total_pos[i];
                        if (corners == null || corners.Length != 4)
                        {
                            // 無効キー → (0,0) を4頂点分送る
                            for (int j = 0; j < 4; j++)
                            {
                                ms.Write(BitConverter.GetBytes(0f), 0, 4);
                                ms.Write(BitConverter.GetBytes(0f), 0, 4);
                            }
                            continue;
                        }

                        for (int j = 0; j < 4; j++)
                        {
                            ms.Write(BitConverter.GetBytes(corners[j].x), 0, 4);
                            ms.Write(BitConverter.GetBytes(corners[j].y), 0, 4);
                        }
                    }

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