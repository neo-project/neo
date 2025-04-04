// Copyright (C) 2015-2025 The Neo Project.
//
// Utility.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Akka.Event;
using Neo.Extensions;
using System.Text;

namespace Neo
{
    /// <summary>
    /// A utility class that provides common functions.
    /// </summary>
    public static class Utility
    {
        /// <summary>
        /// A strict UTF8 encoding used in NEO system.
        /// </summary>
        public static Encoding StrictUTF8 => StringExtensions.StrictUTF8;
    }
}
