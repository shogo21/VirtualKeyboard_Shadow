using System.Threading;
using System;
using UnityEngine;

// 別スレッドでRun()を実行するクラス

public abstract class ThreadRunner
{
    private bool isAlive = false;
    private CancellationTokenSource tokenSource;
    protected CancellationToken token;

    private Thread thread;

    public ThreadRunner()
    {
        this.tokenSource = new CancellationTokenSource();
        this.token = this.tokenSource.Token;
    }

    public void Start()
    {
        if (!this.isAlive)
        {
            this.thread = new Thread(this.Run);
            this.thread.IsBackground = true;
            this.thread.Start();
            this.isAlive = true;
        }
        else
        {
            throw new InvalidOperationException();
        }
    }

    public void Stop()
    {
        this.tokenSource.Cancel();
    }

    protected abstract void Run();
}
