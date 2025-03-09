using System;
using System.Collections.Concurrent;

namespace Chetch.Utilities;

public class DispatchQueue<T> : ConcurrentQueue<T>
    {
        public Func<bool> CanDequeue;

        ManualResetEvent releaseQueue = new ManualResetEvent(false);

        Task qTask;
        
        public DispatchQueue() : base()
        {}
        
        public DispatchQueue(Func<bool> canDequeue) : base()
        {
            CanDequeue = canDequeue;
        }

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
                    if(CanDequeue())
                    {
                        Console.WriteLine("Dequeuing is possible so waiting for a release...");
                        if(IsEmpty)
                        {
                            releaseQueue.WaitOne();
                        }
                        
                        T qi;
                        while(CanDequeue() && TryDequeue(out qi))
                        {
                            Console.WriteLine("Dequeing gets {0}", qi);
                            await Task.Delay(100, ct);
                        }
                        releaseQueue.Reset();
                        Console.WriteLine("Queue cannot be dequeued any more there are {0} items left", Count);
                    }
                    else
                    {
                        Console.WriteLine("Dequeueing not yet possible so waiting...");
                        await Task.Delay(1000, ct);
                    }
                } while(!ct.IsCancellationRequested);

                Console.WriteLine("Dispatch task has ended!");
            }, ct);

            return qTask;
        }
    }
