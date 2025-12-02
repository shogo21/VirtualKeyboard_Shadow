//１フレーム分の29枚の画像を一斉におくるコード
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Linq;

public class KeyboardImageSender : ThreadRunner
{
    public Camera arCamera;
    public RenderTexture rt;

    private List<KeyState> allKeys;
    private List<char> keyChars;

    private int frameCounter;
    private NamedPipeServer pipe;
    public Texture2D dummyTex;
    public byte[] dummyPngBytes;

    private ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();

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

        this.dummyTex = new Texture2D(64, 64, TextureFormat.RGB24, false);
        Color[] pixels = new Color[64 * 64];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.black;
        this.dummyTex.SetPixels(pixels);
        this.dummyTex.Apply();

        // ここで PNG に変換して保持
        this.dummyPngBytes = this.dummyTex.EncodeToPNG();
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

    // 64×64 黒画像を1回だけ生成して再利用
    /*private void CreateDummyTexture()
    {
        this.dummyTex = new Texture2D(64, 64, TextureFormat.RGB24, false);
        Color[] pixels = new Color[64 * 64];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.black;
        this.dummyTex.SetPixels(pixels);
        this.dummyTex.Apply();

        // ここで PNG に変換して保持
        this.dummyPngBytes = this.dummyTex.EncodeToPNG();
    }

    public void firstStart()
    {
        CreateDummyTexture();
    }*/


    protected override void Run()
    {
        try
        {
            pipe.WakeUp();
        }
        catch (Exception e)
        {
            Console.WriteLine("Pipe WakeUp failed: " + e.Message);
            return;
        }

        frameCounter = 0;

        while (true)
        {
            if (token.IsCancellationRequested) break;

            if (pipe.status == NamedPipeServer.Status.Connected)
            {
                frameCounter++;
                // メインスレッドで全キーをまとめて処理
                mainThreadActions.Enqueue(() =>
                {
                    try
                    {
                        CaptureAndSendAllKeys();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("CaptureAndSendAllKeys failed: " + e.Message);
                    }
                });
            }

            Thread.Sleep(10);
        }
    }

    public void UpdateOnMainThread()
    {
        while (mainThreadActions.TryDequeue(out var action))
        {
            action.Invoke();
        }
    }

    private void CaptureAndSendAllKeys()
    {
        if (pipe == null || pipe.status != NamedPipeServer.Status.Connected)
        {
            Console.WriteLine("[Capture] Pipe not connected.");
            return;
        }else
        {
            Console.WriteLine("[Capture] Pipe connected.");
        }
        try
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // 1フレーム番号を書き込む
                ms.Write(BitConverter.GetBytes(frameCounter), 0, 4);

                // 29枚まとめて書き込む
                foreach (var pair in allKeys.Zip(keyChars, (ks, kc) => new { ks, kc }))
                {
                    Texture2D tex = CaptureKey(pair.ks);
                    //Texture2D texToSend = tex != null ? tex : dummyTex;
                    byte[] pngBytes = tex != null ? tex.EncodeToPNG() : this.dummyPngBytes;

                    /*if (tex == null)
                    {
                        tex = new Texture2D(64, 64, TextureFormat.RGB24, false);
                        Color[] pixels = new Color[64 * 64];
                        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.black;
                        tex.SetPixels(pixels);
                        tex.Apply();
                    }*/

                    //byte[] pngBytes = tex.EncodeToPNG();

                    ms.Write(BitConverter.GetBytes(pngBytes.Length), 0, 4);
                    ms.Write(pngBytes, 0, pngBytes.Length);

                    // キャプチャしたテクスチャは破棄（dummyTex は破棄しない）
                    if (tex != null && tex != dummyTex)
                    {
                        UnityEngine.Object.Destroy(tex);
                    }
                }
                byte[] frameBytes = ms.ToArray();
                pipe.Write(frameBytes);


                //Console.WriteLine($"[Capture] Frame {frameCounter} sent. Size={frameBytes.Length}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[Capture] Exception: " + ex.Message);
        }
    }

    private Texture2D CaptureKey(KeyState ks)
    {

        Vector3[] corners = new Vector3[4];
        ks.rectTransform.GetWorldCorners(corners);
        Vector2 min = RectTransformUtility.WorldToScreenPoint(arCamera, corners[0]);
        Vector2 max = RectTransformUtility.WorldToScreenPoint(arCamera, corners[2]);

        min.x = Mathf.Clamp(min.x, 0, Screen.width - 1);
        min.y = Mathf.Clamp(min.y, 0, Screen.height - 1);
        max.x = Mathf.Clamp(max.x, 0, Screen.width - 1);
        max.y = Mathf.Clamp(max.y, 0, Screen.height - 1);

        int width = Mathf.CeilToInt(max.x - min.x);
        int height = Mathf.CeilToInt(max.y - min.y);
        /*Vector2[] scr = new Vector2[4];
        for (int i = 0; i < 4; i++)
        {
            scr[i] = RectTransformUtility.WorldToScreenPoint(arCamera, corners[i]);
        }

        float minXf = scr.Min(p => p.x);
        float maxXf = scr.Max(p => p.x);
        float minYf = scr.Min(p => p.y);
        float maxYf = scr.Max(p => p.y);

        // 2) Clamp to screen bounds (use inclusive 0 .. Screen.width/height)
        int minX = Mathf.Clamp(Mathf.FloorToInt(minXf), 0, Screen.width - 1);
        int minY = Mathf.Clamp(Mathf.FloorToInt(minYf), 0, Screen.height - 1);
        int maxX = Mathf.Clamp(Mathf.CeilToInt(maxXf), 0, Screen.width - 1);
        int maxY = Mathf.Clamp(Mathf.CeilToInt(maxYf), 0, Screen.height - 1);

        int width = maxX - minX;
        int height = maxY - minY;*/
        if (width <= 1 || height <= 1) return null;

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(min.x, min.y, width, height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        float angle = ks.rectTransform.localRotation.eulerAngles.z;
        Texture2D normalized = RotateTexture(tex, -angle);
        UnityEngine.Object.Destroy(tex);
        Texture2D resized = ResizeTexture(normalized, 64, 64);

        // normalized も不要なので破棄
        UnityEngine.Object.Destroy(normalized);

        // resized を返す
        return resized;
    }

    private Texture2D ResizeTexture(Texture2D src, int targetW, int targetH)
    {
        var rt = RenderTexture.GetTemporary(targetW, targetH, 0, RenderTextureFormat.ARGB32);
        RenderTexture.active = rt;
        Graphics.Blit(src, rt);
        Texture2D outTex = new Texture2D(targetW, targetH, TextureFormat.RGBA32, false);
        outTex.ReadPixels(new Rect(0, 0, targetW, targetH), 0, 0);
        outTex.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        return outTex;
    }

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
                    dst[dstIdx] = new Color32(0, 0, 0, 0);
            }
        }

        rotated.SetPixels32(dst);
        rotated.Apply();
        return rotated;
    }
}