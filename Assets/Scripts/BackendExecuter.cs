using System.Diagnostics;
using UnityEngine;

public class BackendExecuter : MonoBehaviour
{
    private Process process;

    void Start()
    {
        string exe = System.Environment.GetEnvironmentVariable("ComSpec");
        string file = System.Environment.CurrentDirectory + @"\start_backend.bat";

        this.process = new Process
        {
            StartInfo = new ProcessStartInfo(exe)
            {
                UseShellExecute = true,
                Arguments = "/K " + file
            }
        };
        this.process.Start();
    }

    void OnDestroy()
    {
        // this.process.Kill();
    }
}
