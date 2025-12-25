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
    private SharedData<float> keysize_Px;
    private SharedData<Vector2[]> keys_angle;
    private SharedData<Vector2[]> keys_pos;
    private string pipeName = "KeyInfoPipe";
    private readonly object sendLock = new object();
    static readonly byte[] MAGIC = Encoding.ASCII.GetBytes("KSF1PIPE");
    private uint payloadSize = 4 + 29 * 4 * 2 * 4;


    public KeyInfoSender(SharedData<uint> frame_id, SharedData<float> keysize_Px, SharedData<Vector2[]> keys_angle, SharedData<Vector2[]> keys_pos)
    {
        this.frame_id = frame_id;
        this.keysize_Px = keysize_Px;
        this.keys_angle = keys_angle;
        this.keys_pos = keys_pos;
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
                if (this.frame_id.TryGet(out uint fid) && this.keysize_Px.TryGet(out float keysize) && this.keys_angle.TryGet(out Vector2[] angle) && this.keys_pos.TryGet(out Vector2[] keys_center_pos))
                {
                    TrySendKeyPos(fid, keysize, angle, keys_center_pos);
                }
            }
        }
    }

    public void TrySendKeyPos(uint fId, float sizeKey, Vector2[] angle_key, Vector2[] keys_total_pos)
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
                    ms.Write(BitConverter.GetBytes(sizeKey), 0, 4);

                    for (int i = 0; i < angle_key.Length; i++)
                    {
                        ms.Write(BitConverter.GetBytes(angle_key[i].x), 0, 4);
                        ms.Write(BitConverter.GetBytes(angle_key[i].y), 0, 4);
                    }

                    // ---- 座標データ ----
                    for (int i = 0; i < keys_total_pos.Length; i++)
                    {
                        if (keys_total_pos[i] == null)
                        {
                            ms.Write(BitConverter.GetBytes(0f), 0, 4);
                            ms.Write(BitConverter.GetBytes(0f), 0, 4);
                        }
                        else
                        {
                            ms.Write(BitConverter.GetBytes(keys_total_pos[i].x), 0, 4);
                            ms.Write(BitConverter.GetBytes(keys_total_pos[i].y), 0, 4);
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