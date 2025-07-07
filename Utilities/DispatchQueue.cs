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

    int DispatchLoopWait { get; set; } = DISPATCH_LOOP_WAIT;
    
    public TaskStatus RunningStatus => qTask == null ? TaskStatus.WaitingToRun : qTask.Status;
    #endregion


    #region Events
    public event EventHandler<T> Dequeued;
    #endregion

    #region Fields
    ManualResetEvent releaseQueue = new ManualResetEvent(false);

    Task qTask;

    //Used by start and stop methods
    CancellationTokenSource qctSource;

    bool flushing = false;
    #endregion

    #region Constructors
    public DispatchQueue() : base()
    { }

    public DispatchQueue(Func<bool> canDequeue, int dispatchLoopWait = DISPATCH_LOOP_WAIT) : base()
    {
        CanDequeue = canDequeue;
        if (dispatchLoopWait < 0)
        {
            throw new ArgumentException("Dispatch loop wait cannot be less than 0");
        }
        DispatchLoopWait = dispatchLoopWait;
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

    virtual public Task Start()
    {
        if (qctSource == null)
        {
            qctSource = new CancellationTokenSource();
        }

        return Run(qctSource.Token);
    }

    virtual public void Stop()
    {
        qctSource.Cancel();
    }

    public Task Run(CancellationToken ct)
    {

        if (CanDequeue == null)
        {
            throw new Exception("Cannot Run without a CanDequeue function, please suppply.");
        }

        if (qTask != null && (qTask.Status != TaskStatus.Faulted || qTask.Status != TaskStatus.Canceled))
        {
            throw new Exception(String.Format("Cannot run as task is currently in {0} status", qTask.Status));
        }

        ct.Register(() => { releaseQueue.Set(); });

        qTask = Task.Run(async () =>
        {
            do
            {
                if (CanDequeue() || flushing)
                {
                    if (IsEmpty && !flushing)
                    {
                        releaseQueue.WaitOne();
                    }
                    try
                    {
                        T qi;
                        while ((CanDequeue() || flushing) && TryDequeue(out qi))
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
                    await Task.Delay(DispatchLoopWait, ct);
                }
            } while (!ct.IsCancellationRequested);

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