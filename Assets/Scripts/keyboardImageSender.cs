using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

public class KeyboardImageSender : ThreadRunner
{
    public Camera arCamera;           // AR HMD カメラ
    public RenderTexture rt;          // カメラの RenderTexture
    public KeyboardUI keyboard;       // KeyboardUI スクリプト
    private List<KeyState> allKeys;
    private List<char> keyChars;


    private int frameCounter;

    private NamedPipeServer pipe;

    public KeyboardImageSender(
        IEnumerable<KeyState> mainKeys,   // keyboard.keys.Values
        KeyState deleteKey,
        KeyState enterKey,
        KeyState spaceKey,
        IEnumerable<char> mainKeyChars    // keyboard.keys.Keys
    )
    {
        // --- KeyState の初期化 ---
        this.allKeys = new List<KeyState>();
        this.allKeys.AddRange(mainKeys);  // アルファベット26キー
        this.allKeys.Add(deleteKey);      // Delete
        this.allKeys.Add(enterKey);       // Enter
        this.allKeys.Add(spaceKey);       // Space

        // --- char の初期化 ---
        this.keyChars = new List<char>();
        this.keyChars.AddRange(mainKeyChars); // アルファベット
        this.keyChars.Add('#');               // Delete
        this.keyChars.Add('&');               // Enter
        this.keyChars.Add('%');               // Space

        this.pipe = new NamedPipeServer("MultiImagePipe");
    }

    public void setKeys(IEnumerable<KeyState> mainKeys,   // keyboard.keys.Values
        KeyState deleteKey,
        KeyState enterKey,
        KeyState spaceKey
    )
    {
        // --- 前のキーを全てクリア ---
        this.allKeys.Clear();

        // --- 新しいキーセットを追加 ---
        this.allKeys.AddRange(mainKeys);  // アルファベット
        this.allKeys.Add(deleteKey);      // Delete
        this.allKeys.Add(enterKey);       // Enter
        this.allKeys.Add(spaceKey);       // Space
    }

    protected override void Run()
    {
        this.pipe.WakeUp();
        frameCounter = 0;

        while (true)
        {
            try
            {
                if (this.token.IsCancellationRequested) break;

                if (pipe.status == NamedPipeServer.Status.Connected)
                {
                    frameCounter++;

                    // --- SendAllKeys のループ部分 ---
                    for (int i = 0; i < this.allKeys.Count; i++)
                    {
                        KeyState ks = this.allKeys[i];
                        char keyChar = this.keyChars[i];

                        // RectTransform からスクリーン座標を取得
                        Vector3[] worldCorners = new Vector3[4];
                        ks.rectTransform.GetWorldCorners(worldCorners);
                        Vector2 min = RectTransformUtility.WorldToScreenPoint(arCamera, worldCorners[0]);
                        Vector2 max = RectTransformUtility.WorldToScreenPoint(arCamera, worldCorners[2]);

                        int width = Mathf.CeilToInt(max.x - min.x);
                        int height = Mathf.CeilToInt(max.y - min.y);
                        if (width <= 0 || height <= 0) continue;

                        // RenderTexture からピクセルを読み取る
                        RenderTexture.active = rt;
                        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                        tex.ReadPixels(new Rect(min.x, min.y, width, height), 0, 0);
                        tex.Apply();
                        RenderTexture.active = null;

                        // 回転補正
                        float angle = ks.rectTransform.localRotation.eulerAngles.z;
                        Texture2D normalized = RotateTexture(tex, -angle);

                        // 64x64 にリサイズ
                        Texture2D resized = ResizeTexture(normalized, 64, 64);

                        // PNG に変換
                        byte[] pngBytes = resized.EncodeToPNG();

                        // フレーム番号 + データ長 + PNG データの順で送信
                        using (MemoryStream ms = new MemoryStream())
                        {
                            ms.Write(BitConverter.GetBytes(frameCounter), 0, 4);
                            ms.Write(BitConverter.GetBytes(pngBytes.Length), 0, 4);
                            ms.Write(pngBytes, 0, pngBytes.Length);

                            try
                            {
                                //pipe.Write(ms.ToArray());
                            }
                            catch (Exception e)
                            {
                                Debug.LogError("Pipe write failed: " + e.Message);
                            }
                        }
                    }
                    // --- SendAllKeys の処理ここまで ---
                }
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
                Debug.Log(e.StackTrace);
                break;
            }

            // 少し待つ（CPU負荷軽減）
            System.Threading.Thread.Sleep(10);
        }

        Debug.Log("Thread end");
    }


    private Texture2D ResizeTexture(Texture2D src, int targetW, int targetH)
    {
        var rt = RenderTexture.GetTemporary(targetW, targetH, 0, RenderTextureFormat.ARGB32);
        // src を RenderTexture に描く
        RenderTexture active = RenderTexture.active;
        var tmp = new RenderTexture(src.width, src.height, 0);
        Graphics.Blit(src, tmp);
        RenderTexture.active = rt;
        // Draw scaled
        Graphics.Blit(tmp, rt);
        Texture2D outTex = new Texture2D(targetW, targetH, TextureFormat.RGBA32, false);
        outTex.ReadPixels(new Rect(0, 0, targetW, targetH), 0, 0);
        outTex.Apply();
        RenderTexture.active = active;
        tmp.Release();
        RenderTexture.ReleaseTemporary(rt);
        return outTex;
    }

    // 既存の RotateTexture (改良版：RGBA対応、補間はnearest的だが十分)
    private Texture2D RotateTexture(Texture2D original, float angleDeg)
    {
        int w = original.width;
        int h = original.height;
        Texture2D rotated = new Texture2D(w, h, TextureFormat.RGBA32, false);

        float cx = (w - 1) / 2f;
        float cy = (h - 1) / 2f;
        float rad = angleDeg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);

        Color32[] srcPixels = original.GetPixels32();
        Color32[] dst = new Color32[srcPixels.Length];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float u = cos * dx + sin * dy + cx;
                float v = -sin * dx + cos * dy + cy;
                int ui = Mathf.RoundToInt(u);
                int vi = Mathf.RoundToInt(v);

                int dstIdx = y * w + x;
                if (ui >= 0 && ui < w && vi >= 0 && vi < h)
                    dst[dstIdx] = srcPixels[vi * w + ui];
                else
                    dst[dstIdx] = new Color32(0, 0, 0, 0); // 透明
            }
        }

        rotated.SetPixels32(dst);
        rotated.Apply();
        return rotated;
    }
}
