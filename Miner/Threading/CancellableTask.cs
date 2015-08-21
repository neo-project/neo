using System;
using System.Threading;
using System.Threading.Tasks;

namespace AntShares.Threading
{
    internal class CancellableTask
    {
        private Action<CancellationToken> action;
        private Task task_current;
        private CancellationTokenSource cancel_current;

        public CancellableTask(Action<CancellationToken> action)
        {
            this.action = action;
        }

        public void Cancel()
        {
            if (task_current == null || task_current.IsCompleted)
                return;
            cancel_current.Cancel();
        }

        public void Run()
        {
            if (task_current?.IsCompleted == false)
                throw new InvalidOperationException();
            cancel_current = new CancellationTokenSource();
            task_current = Task.Run(() => action(cancel_current.Token), cancel_current.Token);
        }

        public void Wait()
        {
            if (task_current == null)
                throw new InvalidOperationException();
            task_current.Wait();
        }
    }
}
