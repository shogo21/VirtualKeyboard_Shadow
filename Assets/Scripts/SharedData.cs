using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.Concurrent;

public class SharedData<T>
{
    private ConcurrentQueue<T> queue;

    public SharedData()
    {
        this.queue = new ConcurrentQueue<T>();
    }

    public void Set(T data)
    {
        T dummy;
        this.queue.TryDequeue(out dummy);
        this.queue.Enqueue(data);
    }

    public bool TryGet(out T result)
    {
        return this.queue.TryDequeue(out result);
    }
}
