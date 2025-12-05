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
    public Camera arCamera;               // HMD用カメラ
    public int targetOutputSize = 64;     // 出力は 64x64 固定
    public RenderTexture rt;

    private List<KeyState> allKeys;
    private List<char> keyChars;

    private int frameCounter;
    private NamedPipeServer pipe;
    public Texture2D dummyTex;
    public byte[] dummyPngBytes;
    private CanvasController cc;     // ← キャッシュ
    private RectTransform backgroundTransform;
    private ARMarkerDetector detector;    // マーカー情報（px/cm 推定用）

    private ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();

    // Reusable buffers（GCを減らすためにこれらを毎フレーム再利用）
    private Color32[] cameraPixelsCache = null;
    private int cameraPixelsCacheW = 0;
    private int cameraPixelsCacheH = 0;

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
        //this.dummyPngBytes = this.dummyTex.EncodeToPNG();

        this.cc = UnityEngine.Object.FindObjectOfType<CanvasController>();  // ← Startで1回だけ取得
        this.detector = UnityEngine.Object.FindObjectOfType<ARMarkerDetector>();
        this.backgroundTransform =GameObject.Find("Canvas/Background").GetComponent<RectTransform>();
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

                    byte[] rawBytes;

                    if (tex != null)
                    {
                        // PNGより圧倒的に軽い RawTextureData を取得
                        rawBytes = tex.GetRawTextureData();
                    }
                    else
                    {
                        // dummy も PNG ではなく Raw で作る方がよいが、現状はこれでOK
                        rawBytes = dummyTex.GetRawTextureData();
                    }

                    // --- サイズを書き込む（これがあるとPythonで復元可能） ---
                    ms.Write(BitConverter.GetBytes(rawBytes.Length), 0, 4);

                    // --- Rawデータ本体 ---
                    ms.Write(rawBytes, 0, rawBytes.Length);

                    // 不要テクスチャを破棄
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
        Texture2D background = this.cc.BackgroundTexture(); // HMDに映っている最終画像

        if (background == null)
        {
            Console.WriteLine("BackgroundTexture is null.");
            return null;
        }
        else
        {
            Console.WriteLine("BackgroundTexture is not null.");
        }

        Vector3[] corners = new Vector3[4];
        ks.rectTransform.GetWorldCorners(corners);
        Vector2 min = RectTransformUtility.WorldToScreenPoint(arCamera, corners[0]);
        Vector2 max = RectTransformUtility.WorldToScreenPoint(arCamera, corners[2]);

        
        int width = Mathf.CeilToInt(max.x - min.x);
        int height = Mathf.CeilToInt(max.y - min.y);

        // ---- 安全な座標・サイズに補正する ----
        int x = Mathf.Clamp((int)min.x, 0, background.width - 1);
        int y = Mathf.Clamp((int)min.y, 0, background.height - 1);

        int w = Mathf.Clamp(width, 1, background.width - x);
        int h = Mathf.Clamp(height, 1, background.height - y);

        if (w <= 1 || h <= 1)
        {
            return this.dummyTex;
        }

        // ---- 背景 Texture2D から切り抜く ----
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);

        Color[] pixels = background.GetPixels(x, y, w, h);
        tex.SetPixels(pixels);
        tex.Apply();

        // resized を返す
        return tex;
    }

    /*いらない
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
    }*/
}