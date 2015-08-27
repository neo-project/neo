using System;
using System.Threading;
using System.Threading.Tasks;

namespace AntShares.Threading
{
    internal class Worker : IDisposable
    {
        private readonly Action<CancellationToken> workAction;
        private readonly CancellationTokenSource shutdownToken;
        private Task task_current;

        private int started = 0;
        private int disposed = 0;

        public TimeSpan WaitTime { get; set; }

        public Worker(Action<CancellationToken> workAction, TimeSpan waitTime)
        {
            this.workAction = workAction;
            this.WaitTime = waitTime;
            this.shutdownToken = new CancellationTokenSource();
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0)
            {
                shutdownToken.Cancel();
                if (task_current != null)
                    task_current.Wait();
                shutdownToken.Dispose();
            }
        }

        private async Task DoWork()
        {
            await Task.Delay(WaitTime, shutdownToken.Token);
            if (shutdownToken.IsCancellationRequested) return;
            await Task.Run(() => workAction(shutdownToken.Token), shutdownToken.Token);
        }

        public async void Start()
        {
            if (Interlocked.Exchange(ref started, 1) == 0)
            {
                while (!shutdownToken.IsCancellationRequested)
                {
                    task_current = DoWork();
                    await task_current;
                }
            }
        }

        public async Task WaitAsync()
        {
            if (task_current == null) return;
            await task_current;
        }
    }
}
