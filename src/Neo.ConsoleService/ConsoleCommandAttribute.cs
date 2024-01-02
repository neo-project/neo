// Copyright (C) 2015-2024 The Neo Project.
//
// ConsoleCommandAttribute.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Diagnostics;
using System.Linq;

namespace Neo.ConsoleService
{
    [DebuggerDisplay("Verbs={string.Join(' ',Verbs)}")]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ConsoleCommandAttribute : Attribute
    {
        /// <summary>
        /// Verbs
        /// </summary>
        public string[] Verbs { get; }

        /// <summary>
        /// Category
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="verbs">Verbs</param>
        public ConsoleCommandAttribute(string verbs)
        {
            Verbs = verbs.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(u => u.ToLowerInvariant()).ToArray();
        }
    }
}
