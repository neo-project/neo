// Copyright (C) 2015-2026 The Neo Project.
//
// UnknownHardforkException.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.SmartContract.Native
{
    /// <summary>
    /// Thrown when a committee-signed transaction tries to activate an unknown hardfork.
    /// Block persistence must rethrow this so outdated nodes stop following the chain
    /// instead of only FAULTing the transaction (neo#4580).
    /// </summary>
    public class UnknownHardforkException : InvalidOperationException
    {
        /// <summary>
        /// The hardfork name that was not recognized.
        /// </summary>
        public string HardforkName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnknownHardforkException"/> class.
        /// </summary>
        /// <param name="hardforkName">The unrecognized hardfork name.</param>
        public UnknownHardforkException(string hardforkName)
            : base($"Unknown hardfork: {hardforkName}. Update node software to continue.")
        {
            HardforkName = hardforkName;
        }

        /// <summary>
        /// Returns whether <paramref name="exception"/> (or any inner exception) is an
        /// <see cref="UnknownHardforkException"/>.
        /// </summary>
        public static bool IsInstance(Exception? exception)
        {
            while (exception is not null)
            {
                if (exception is UnknownHardforkException)
                    return true;
                exception = exception.InnerException;
            }

            return false;
        }
    }
}
