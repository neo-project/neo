using System;
using System.Threading;
using System.Threading.Tasks;

namespace AntShares.Threading
{
    internal class CancellableTask
    {
        private Func<CancellationToken, Task> action;
        private Task task_current;
        private CancellationTokenSource cancel_current;

        public CancellableTask(Func<CancellationToken, Task> action)
        {
            this.action = action;
        }

        public void Cancel()
        {
            if (task_current == null || task_current.IsCompleted)
                return;
            cancel_current.Cancel();
        }

        public async void Run()
        {
            if (task_current?.IsCompleted == false)
                throw new InvalidOperationException();
            cancel_current = new CancellationTokenSource();
            task_current = action(cancel_current.Token);
            try
            {
                await task_current;
            }
            catch (OperationCanceledException) { }
        }

        public void Wait()
        {
            if (task_current == null)
                throw new InvalidOperationException();
            task_current.Wait();
        }
    }
}
