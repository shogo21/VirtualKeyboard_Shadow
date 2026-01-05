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
    private SharedData<float> keys_size;
    private SharedData<float> keys_angle;
    private SharedData<Vector2[]> keys_centerpos;
    private string pipeName = "KeyInfoPipe";
    private readonly object sendLock = new object();
    static readonly byte[] MAGIC = Encoding.ASCII.GetBytes("KSF1PIPE");


    public KeyInfoSender(SharedData<uint> frame_id, SharedData<float> keys_size, SharedData<float> keys_angle, SharedData<Vector2[]> keys_centerpos)
    {
        this.frame_id = frame_id;
        this.keys_size = keys_size;
        this.keys_angle = keys_angle;
        this.keys_centerpos = keys_centerpos;
        this.pipe = new NamedPipeServer(this.pipeName);
    }

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
                if (this.frame_id.TryGet(out uint frameId) && this.keys_size.TryGet(out float keysSize) && this.keys_angle.TryGet(out float keysAngle) && this.keys_centerpos.TryGet(out Vector2[] keysCenterpos))
                {
                    TrySendKeyPos(frameId, keysSize, keysAngle, keysCenterpos);
                }
            }
        }
    }

    public void TrySendKeyPos(uint frame_Id, float keys_Size, float keys_Angle, Vector2[] keys_Cenerpos)
    {
        // ---- 二重送信・割り込み防止 ----
        lock (sendLock)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    ms.Write(MAGIC, 0, MAGIC.Length);
                    ms.Write(BitConverter.GetBytes(frame_Id), 0, 4); // uint32
                    ms.Write(BitConverter.GetBytes(keys_Size), 0, 4);

                    ms.Write(BitConverter.GetBytes(keys_Angle), 0, 4);

                    // ---- 座標データ ----
                    for (int i = 0; i < keys_Cenerpos.Length; i++)
                    {
                        if (keys_Cenerpos[i] == null)
                        {
                            ms.Write(BitConverter.GetBytes(0f), 0, 4);
                            ms.Write(BitConverter.GetBytes(0f), 0, 4);
                        }
                        else
                        {
                            ms.Write(BitConverter.GetBytes(keys_Cenerpos[i].x), 0, 4);
                            ms.Write(BitConverter.GetBytes(keys_Cenerpos[i].y), 0, 4);
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