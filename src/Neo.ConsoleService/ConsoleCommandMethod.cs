// Copyright (C) 2016-2023 The Neo Project.
// 
// The Neo.ConsoleService is free software distributed under the MIT 
// software license, see the accompanying file LICENSE in the main directory
// of the project or http://www.opensource.org/licenses/mit-license.php 
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
        /// Is this command
        /// </summary>
        /// <param name="tokens">Tokens</param>
        /// <param name="consumedArgs">Consumed Arguments</param>
        /// <returns>True if is this command</returns>
        public bool IsThisCommand(CommandToken[] tokens, out int consumedArgs)
        {
            int checks = Verbs.Length;
            bool quoted = false;
            var tokenList = new List<CommandToken>(tokens);

            while (checks > 0 && tokenList.Count > 0)
            {
                switch (tokenList[0])
                {
                    case CommandSpaceToken _:
                        {
                            tokenList.RemoveAt(0);
                            break;
                        }
                    case CommandQuoteToken _:
                        {
                            quoted = !quoted;
                            tokenList.RemoveAt(0);
                            break;
                        }
                    case CommandStringToken str:
                        {
                            if (Verbs[^checks] != str.Value.ToLowerInvariant())
                            {
                                consumedArgs = 0;
                                return false;
                            }

                            checks--;
                            tokenList.RemoveAt(0);
                            break;
                        }
                }
            }

            if (quoted && tokenList.Count > 0 && tokenList[0].Type == CommandTokenType.Quote)
            {
                tokenList.RemoveAt(0);
            }

            // Trim start

            while (tokenList.Count > 0 && tokenList[0].Type == CommandTokenType.Space) tokenList.RemoveAt(0);

            consumedArgs = tokens.Length - tokenList.Count;
            return checks == 0;
        }
    }
}
