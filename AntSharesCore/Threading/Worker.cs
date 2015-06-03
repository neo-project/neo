using System;
using System.Threading;

namespace AntShares.Threading
{
    internal class Worker : IDisposable
    {
        private readonly Action<CancellationToken> workAction;
        private readonly CancellationTokenSource shutdownToken;
        private readonly Thread workerThread;
        private readonly AutoResetEvent notifyEvent;
        private readonly AutoResetEvent forceNotifyEvent;
        private readonly ManualResetEventSlim idleEvent;
        private readonly ManualResetEventSlim stopEvent;

        private int started = 0;
        private int disposed = 0;

        public Worker(string name, Action<CancellationToken> workAction, bool runOnStart, TimeSpan waitTime, TimeSpan maxIdleTime)
        {
            this.workAction = workAction;
            this.WaitTime = waitTime;
            this.MaxIdleTime = maxIdleTime;
            this.shutdownToken = new CancellationTokenSource();
            this.workerThread = new Thread(WorkerLoop);
            this.workerThread.Name = name;
            this.notifyEvent = new AutoResetEvent(runOnStart);
            this.forceNotifyEvent = new AutoResetEvent(runOnStart);
            this.idleEvent = new ManualResetEventSlim(false);
            this.stopEvent = new ManualResetEventSlim(true);
        }

        public TimeSpan WaitTime { get; set; }

        public TimeSpan MaxIdleTime { get; set; }

        public void Start()
        {
            if (Interlocked.Exchange(ref started, 1) == 0)
            {
                this.workerThread.Start();
            }
            this.stopEvent.Set();
        }

        public void Stop()
        {
            this.stopEvent.Reset();
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0)
            {
                this.notifyEvent.Set();
                this.forceNotifyEvent.Set();
                this.shutdownToken.Cancel();
                this.workerThread.Join();
                this.shutdownToken.Dispose();
                this.notifyEvent.Dispose();
                this.forceNotifyEvent.Dispose();
                this.idleEvent.Dispose();
                this.stopEvent.Dispose();
            }
        }

        public void NotifyWork()
        {
            if (disposed == 0)
            {
                this.notifyEvent.Set();
            }
        }

        public void ForceWork()
        {
            if (disposed == 0)
            {
                this.forceNotifyEvent.Set();
                this.notifyEvent.Set();
            }
        }

        public void ForceWorkAndWait()
        {
            if (disposed == 0)
            {
                // wait for worker to idle
                this.idleEvent.Wait();

                // reset its idle state
                this.idleEvent.Reset();

                // force an execution
                ForceWork();

                // wait for worker to be idle again
                this.idleEvent.Wait();
            }
        }

        private void WorkerLoop()
        {
            var working = false;
            try
            {
                while (true)
                {
                    // cooperative loop
                    if (this.shutdownToken.Token.IsCancellationRequested)
                        break;

                    // notify worker is idle
                    this.idleEvent.Set();

                    // stop execution if requested
                    this.stopEvent.Wait();

                    // delay for the requested wait time, unless forced
                    this.forceNotifyEvent.WaitOne(this.WaitTime);

                    // wait for work notification
                    this.notifyEvent.WaitOne(this.MaxIdleTime - this.WaitTime); // subtract time already spent waiting

                    // cooperative loop
                    if (this.shutdownToken.Token.IsCancellationRequested)
                        break;

                    // notify that work is starting
                    this.idleEvent.Reset();

                    // perform the work
                    working = true;
                    workAction(shutdownToken.Token);
                    working = false;
                }
            }
            catch (ObjectDisposedException)
            {
                if (working)
                    throw;
            }
        }
    }
}
