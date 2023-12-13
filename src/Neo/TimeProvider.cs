// Copyright (C) 2015-2022 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

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
