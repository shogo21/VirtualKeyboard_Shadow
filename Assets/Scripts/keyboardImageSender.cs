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
    private CanvasController cc;     // ← キャッシュ
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

        this.dummyTex = new Texture2D(this.targetOutputSize, this.targetOutputSize, TextureFormat.RGB24, false);
        Color[] pixels = new Color[this.targetOutputSize * this.targetOutputSize];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.black;
        this.dummyTex.SetPixels(pixels);
        this.dummyTex.Apply();

        // ここで PNG に変換して保持
        //this.dummyPngBytes = this.dummyTex.EncodeToPNG();

        this.cc = UnityEngine.Object.FindObjectOfType<CanvasController>();  // ← Startで1回だけ取得
        this.detector = UnityEngine.Object.FindObjectOfType<ARMarkerDetector>();
    }

    public void setKeys
    (
        IEnumerable<KeyState> mainKeys,   // keyboard.keys.Values
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
                // メインスレッドで全キーをまとめて処理
                mainThreadActions.Enqueue(() =>
                {
                    try
                    {
                        CaptureAndSendAllKeys();
                    }
                    catch (Exception e)
                    {
                        UnityLogger.Log("CaptureAndSendAllKeys failed: " + e.Message);
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
            UnityLogger.Log("[Capture] Pipe not connected.");
            return;
        }else
        {
            UnityLogger.Log("[Capture] Pipe connected.");
        }

        // 1) 背景（カメラ）テクスチャを取得して Color32[] に
        Texture2D background = null;
        try
        {
            background = this.cc.BackgroundTexture(); // 既存 API と一致させています
        }
        catch (Exception)
        {
            background = null;
        }

        if (background == null)
        {
            UnityLogger.Log("[Capture] BackgroundTexture is null -> send dummies");
            // 全部ダミーを 29 枚送る
            SendFrameAsDummies();
            return;
        }

        // 背景ピクセルをキャッシュ（毎フレーム更新）
        EnsureCameraPixelCache(background);

        // px_per_cm を推定（ARMarkerDetector から取れるなら使う）
        float pxPerCm = this.detector.GetPxPerCm();
        if (pxPerCm <= 0f) pxPerCm = 25f; // フォールバック値（必要なら調整）

        // 2.5cm をピクセルに
        int cropSizePx = Mathf.Max(1, Mathf.RoundToInt(2.5f * pxPerCm));

        using (MemoryStream ms = new MemoryStream())
        {
            // フレーム番号書き込み
            ms.Write(BitConverter.GetBytes(frameCounter), 0, 4);

            // 各キーごとに処理
            foreach (var pair in allKeys.Zip(keyChars, (ks, kc) => new { ks, kc }))
            {
                KeyState ks = pair.ks;

                // (A) キーのスクリーン中心を求める（world corners の中心を screen に変換）
                Vector3[] corners = new Vector3[4];
                ks.rectTransform.GetWorldCorners(corners);
                Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;
                Vector2 screenCenter = RectTransformUtility.WorldToScreenPoint(arCamera, worldCenter);

                // (B) screenCenter を background のピクセル座標とみなす（既存コードに合わせる）
                int cx = Mathf.RoundToInt(screenCenter.x);
                int cy = Mathf.RoundToInt(screenCenter.y);

                // clamp
                cx = Mathf.Clamp(cx, 0, cameraPixelsCacheW - 1);
                cy = Mathf.Clamp(cy, 0, cameraPixelsCacheH - 1);

                // CropSquare 取得（cropSizePx × cropSizePx）
                Color32[] cropped = CropSquareFromCamera(cameraPixelsCache, cameraPixelsCacheW, cameraPixelsCacheH, cx, cy, cropSizePx);

                // キーの回転（RectTransform の Z 回転）を取得して、逆回転で正規化
                float angleDeg = ks.rectTransform.eulerAngles.z;
                float angleRad = -angleDeg * Mathf.Deg2Rad;
                if (Mathf.Abs(angleDeg) > 0.01f)
                {
                    cropped = RotateSquareNearest(cropped, cropSizePx, angleRad);
                }

                // 64x64 にリサイズ（nearest）
                Color32[] out64 = ResizeNearest(cropped, cropSizePx, targetOutputSize);

                // RGB 生データ（3バイト/px）
                byte[] raw = Color32ArrayToRawRGB(out64, targetOutputSize, targetOutputSize);

                // サイズ + raw を書き込む
                ms.Write(BitConverter.GetBytes(raw.Length), 0, 4);
                ms.Write(raw, 0, raw.Length);

                // ここで cropped/out64 を破棄（GC 任せ）
            }

            // Send frame
            byte[] frameBytes = ms.ToArray();
            pipe.Write(frameBytes);
        }
    }

        /*try
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
    }*/

    
    private void SendFrameAsDummies()
    {
        using (MemoryStream ms = new MemoryStream())
        {
            ms.Write(BitConverter.GetBytes(frameCounter), 0, 4);
            byte[] dummyRaw = DummyRawBytes();
            for (int i = 0; i < allKeys.Count; i++)
            {
                ms.Write(BitConverter.GetBytes(dummyRaw.Length), 0, 4);
                ms.Write(dummyRaw, 0, dummyRaw.Length);
            }
            pipe.Write(ms.ToArray());
        }
    }

    // --------------------------
    // ユーティリティ関数群
    // --------------------------

    // 背景テクスチャから Color32[] を得てキャッシュする
    private void EnsureCameraPixelCache(Texture2D background)
    {
        if (cameraPixelsCache == null || cameraPixelsCacheW != background.width || cameraPixelsCacheH != background.height)
        {
            cameraPixelsCacheW = background.width;
            cameraPixelsCacheH = background.height;
            cameraPixelsCache = new Color32[cameraPixelsCacheW * cameraPixelsCacheH];
        }

        // GetPixels32 はメインスレッド呼び出し必須です
        Color32[] tmp = background.GetPixels32();
        // コピー（GetPixels32 は新配列を返すため、参照を使っても良いが安全のためコピー）
        Array.Copy(tmp, cameraPixelsCache, tmp.Length);
    }
    

    // cropSize × cropSize 正方領域を cameraPixels から取得
    private Color32[] CropSquareFromCamera(Color32[] cameraPixels, int camW, int camH, int cx, int cy, int cropSize)
    {
        Color32[] dst = new Color32[cropSize * cropSize];
        int half = cropSize / 2;

        for (int y = 0; y < cropSize; y++)
        {
            int sy = cy - half + y;
            if (sy < 0 || sy >= camH)
            {
                // 統一して黒にする
                for (int x = 0; x < cropSize; x++) dst[y * cropSize + x] = new Color32(0, 0, 0, 255);
                continue;
            }

            int rowStart = sy * camW;
            for (int x = 0; x < cropSize; x++)
            {
                int sx = cx - half + x;
                if (sx < 0 || sx >= camW)
                {
                    dst[y * cropSize + x] = new Color32(0, 0, 0, 255);
                }
                else
                {
                    dst[y * cropSize + x] = cameraPixels[rowStart + sx];
                }
            }
        }
        return dst;
    }

    // 回転（最近傍）: src は size x size 正方画像
    private Color32[] RotateSquareNearest(Color32[] src, int size, float angleRad)
    {
        Color32[] dst = new Color32[size * size];
        float cosA = Mathf.Cos(angleRad);
        float sinA = Mathf.Sin(angleRad);
        float half = (size - 1) * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - half;
                float dy = y - half;

                // 逆回転でソース座標を求める
                float sx = cosA * dx - sinA * dy + half;
                float sy = sinA * dx + cosA * dy + half;

                int ix = Mathf.RoundToInt(sx);
                int iy = Mathf.RoundToInt(sy);

                int dstIdx = y * size + x;
                if (ix >= 0 && ix < size && iy >= 0 && iy < size)
                    dst[dstIdx] = src[iy * size + ix];
                else
                    dst[dstIdx] = new Color32(0, 0, 0, 255);
            }
        }
        return dst;
    }

    // 最近傍でリサイズ srcSize -> dstSize
    private Color32[] ResizeNearest(Color32[] src, int srcSize, int dstSize)
    {
        if (srcSize == dstSize) return src; // 同じなら返す（注意: 参照）
        Color32[] dst = new Color32[dstSize * dstSize];
        float ratio = (float)srcSize / dstSize;
        for (int y = 0; y < dstSize; y++)
        {
            int sy = Mathf.Min(srcSize - 1, Mathf.FloorToInt(y * ratio));
            for (int x = 0; x < dstSize; x++)
            {
                int sx = Mathf.Min(srcSize - 1, Mathf.FloorToInt(x * ratio));
                dst[y * dstSize + x] = src[sy * srcSize + sx];
            }
        }
        return dst;
    }

    // Color32[] -> RGB raw bytes
    private byte[] Color32ArrayToRawRGB(Color32[] arr, int w, int h)
    {
        byte[] raw = new byte[w * h * 3];
        int j = 0;
        for (int i = 0; i < arr.Length; i++)
        {
            raw[j++] = arr[i].r;
            raw[j++] = arr[i].g;
            raw[j++] = arr[i].b;
        }
        return raw;
    }

    private byte[] DummyRawBytes()
    {
        Color32[] d = new Color32[targetOutputSize * targetOutputSize];
        for (int i = 0; i < d.Length; i++) d[i] = new Color32(0, 0, 0, 255);
        return Color32ArrayToRawRGB(d, targetOutputSize, targetOutputSize);
    }

    /*private Texture2D CaptureKey(KeyState ks)
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
    }*/

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