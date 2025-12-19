using System.IO;
using UnityEngine;

public static class UnityLogger
{
    private static string logPath = Application.dataPath + "/../unity_log.txt";

    public static void Log(string message)
    {
        File.AppendAllText(logPath, message + "\n");
    }
}