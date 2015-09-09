using System;
using System.Threading;

namespace AntShares.Threading
{
    internal class TimeoutAction : IDisposable
    {
        private Timer timer;
        private Action action;
        private int action_done;
        private TimeSpan dueTime;
        private DateTime timeout;
        private Func<bool> predicate_timeout;
        private Func<bool> predicate_force;

        public TimeoutAction(Action action, TimeSpan dueTime, Func<bool> predicate_timeout, Func<bool> predicate_force)
        {
            this.timer = new Timer(Timer_Callback);
            this.action = action;
            this.dueTime = dueTime;
            this.predicate_timeout = predicate_timeout;
            this.predicate_force = predicate_force;
            Reset();
        }

        public bool CheckPredicate()
        {
            if ((DateTime.Now < timeout || !predicate_timeout()) && !predicate_force())
                return false;
            if (Interlocked.Exchange(ref action_done, 1) == 1)
                return false;
            timer.Change(Timeout.Infinite, Timeout.Infinite);
            action();
            return true;
        }

        public void Dispose()
        {
            timer.Dispose();
        }

        public void Reset()
        {
            if (dueTime == Timeout.InfiniteTimeSpan)
                timeout = DateTime.MaxValue;
            else
                timeout = DateTime.Now + dueTime;
            timer.Change(dueTime, Timeout.InfiniteTimeSpan);
            action_done = 0;
        }

        public void Timer_Callback(object state)
        {
            CheckPredicate();
        }
    }
}
