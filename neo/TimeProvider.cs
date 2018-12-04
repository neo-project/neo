using System;

namespace Neo
{
    public class TimeProvider
    {
        private static readonly TimeProvider Default = new TimeProvider();

        public static TimeProvider Current { get; internal set; } = Default;
        public virtual DateTime UtcNow => DateTime.UtcNow;

        internal static void ResetToDefault()
        {
            Current = Default;
        }
    }
}
