// Copyright (C) 2015-2025 The Neo Project.
//
// ConsoleCommandMethod.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Neo.ConsoleService
{
    [DebuggerDisplay("Key={Key}")]
    internal class ConsoleCommandMethod
    {
        /// <summary>
        /// Verbs
        /// </summary>
        public string[] Verbs { get; }

        /// <summary>
        /// Key
        /// </summary>
        public string Key => string.Join(' ', Verbs);

        /// <summary>
        /// Help category
        /// </summary>
        public string HelpCategory { get; set; }

        /// <summary>
        /// Help message
        /// </summary>
        public string HelpMessage { get; set; }

        /// <summary>
        /// Instance
        /// </summary>
        public object Instance { get; }

        /// <summary>
        /// Method
        /// </summary>
        public MethodInfo Method { get; }

        /// <summary>
        /// Set instance command
        /// </summary>
        /// <param name="instance">Instance</param>
        /// <param name="method">Method</param>
        /// <param name="attribute">Attribute</param>
        public ConsoleCommandMethod(object instance, MethodInfo method, ConsoleCommandAttribute attribute)
        {
            Method = method;
            Instance = instance;
            Verbs = attribute.Verbs;
            HelpCategory = attribute.Category;
            HelpMessage = attribute.Description;
        }

        /// <summary>
        /// Match this command or not
        /// </summary>
        /// <param name="tokens">Tokens</param>
        /// <returns>Tokens consumed, 0 if not match</returns>
        public int IsThisCommand(IReadOnlyList<CommandToken> tokens)
        {
            int matched = 0, consumed = 0;
            for (; matched < Verbs.Length && consumed < tokens.Count; consumed++)
            {
                if (tokens[consumed].IsWhiteSpace) continue;
                if (tokens[consumed].Value != Verbs[matched]) return 0;
                matched++;
            }
            return matched == Verbs.Length ? consumed : 0;
        }
    }
}
