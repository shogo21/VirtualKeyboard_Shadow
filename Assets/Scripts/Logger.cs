using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Diagnostics;
using System.IO;

// ログ残すクラス
// <日時(ミリ秒まで)>, <Front/Back>, <ログ種別(クラス)>, <呼び出し元情報>, <実データ>
public class Logger
{
    static List<string> logs = new List<string>();
    static int logBatchSize = 5000; // バッチ書き込みサイズ行数


    public static void Logging<T>(T data)
    {
        string timestr = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss:ffffff");
        string datastr = JsonUtility.ToJson(data);
        var caller = new System.Diagnostics.StackFrame(1, false).GetMethod();
        string callerstr = caller.DeclaringType.FullName + "/" + caller.Name;
        string className = data.GetType().Name;

        logs.Add(string.Join(",", new[] { timestr, "Front", className, callerstr, ConvertToCSVFormat(datastr) }));
        // バッチサイズに達したら書き出し
        if (logs.Count >= logBatchSize)
        {
            Output();
            logs.Clear(); // ログをファイルに書き出したらリストをクリア
        }
    }

    private static string ConvertToCSVFormat(string str)
    {
        if (str.Contains(","))
        {
            return "\"" + str.Replace("\"", "\"\"") + "\"";
        }
        else
        {
            return str;
        }
    }

    public static void Output()
    {
        using (var writer = new StreamWriter("./logs.csv", append: true)) // 追記モード
        {
            foreach (var log in logs) writer.Write(log + "\n");
        }
    }
}

public class ARMarkerLog
{
    [SerializeField] private float[] position = new float[2];
    [SerializeField] private float angle = 0.0f;

    public ARMarkerLog(Vector2 _pos, Vector2 _next)
    {
        this.position[0] = _pos.x;
        this.position[1] = _pos.y;
        Vector2 offset = _next - _pos;
        this.angle = Mathf.Atan2(offset.y, offset.x);
    }
}