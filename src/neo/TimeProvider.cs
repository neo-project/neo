using System;

namespace Neo
{
    /// <summary>
    /// The time provider for the NEO system.
    /// </summary>
    public class TimeProvider
    {
        private static readonly TimeProvider Default = new();

        /// <summary>
        /// The currently used <see cref="TimeProvider"/> instance.
        /// </summary>
        public static TimeProvider Current { get; internal set; } = Default;

        /// <summary>
        /// Gets the current time expressed as the Coordinated Universal Time (UTC).
        /// </summary>
        public virtual DateTime UtcNow => DateTime.UtcNow;

        internal static void ResetToDefault()
        {
            Current = Default;
        }
    }
}
