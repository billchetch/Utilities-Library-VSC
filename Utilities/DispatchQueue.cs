using System;
using System.Collections.Concurrent;

namespace Chetch.Utilities;

public class DispatchQueue<T> : ConcurrentQueue<T>
{
    #region Constants
    public const int DISPATCH_LOOP_WAIT = 500;
    #endregion

    #region Properties
    public Func<bool> CanDequeue { get; set; }
    #endregion


    #region Events
    public event EventHandler<T> Dequeued;
    #endregion

    #region Fields
    ManualResetEvent releaseQueue = new ManualResetEvent(false);

    Task qTask;

    bool flushing = false;
    #endregion
    
    #region Constructors
    public DispatchQueue() : base()
    {}
    
    public DispatchQueue(Func<bool> canDequeue) : base()
    {
        CanDequeue = canDequeue;
    }
    #endregion

    #region Methods
    public bool Enqueue(T qi, bool releaseQueueAfter = true)
    {
        base.Enqueue(qi);
        if(releaseQueueAfter)
        {
            return releaseQueue.Set();
        }
        else
        {
            return false;
        }
    }

    protected void OnDequeue(T qi)
    {
        Dequeued?.Invoke(this, qi);
    }

    public Task Run(CancellationToken ct)
    {
        
        if(CanDequeue == null)
        {
            throw new Exception("Cannot Run without a CanDequeue function, please suppply.");
        }
        
        if(qTask != null && (qTask.Status != TaskStatus.Faulted || qTask.Status != TaskStatus.Canceled))
        {
            throw new Exception(String.Format("Cannot run as task is currently in {0} status", qTask.Status));
        }

        ct.Register(()=>{ releaseQueue.Set(); });

        qTask = Task.Run(async ()=>{
            do
            {
                if(CanDequeue() || flushing)
                {
                    if(IsEmpty && !flushing)
                    {
                        releaseQueue.WaitOne();
                    }
                    try
                    {
                        T qi;
                        while((CanDequeue() || flushing) && TryDequeue(out qi))
                        {
                            OnDequeue(qi);
                        }
                    }
                    finally
                    {
                        releaseQueue.Reset();
                        flushing = false;
                    }
                }
                else
                {
                    await Task.Delay(DISPATCH_LOOP_WAIT, ct);
                }
            } while(!ct.IsCancellationRequested);

            //Console.WriteLine("Dispatch task has ended!");
        }, ct);

        return qTask;
    }

    public void Flush()
    {
        if(IsEmpty)return;
        flushing = true;
    }
#endregion
}