// Copyright (C) 2015-2024 The Neo Project.
//
// CommandQuoteToken.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Diagnostics;

namespace Neo.ConsoleService
{
    [DebuggerDisplay("Value={Value}, Value={Value}")]
    internal class CommandQuoteToken : CommandToken
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="offset">Offset</param>
        /// <param name="value">Value</param>
        public CommandQuoteToken(int offset, char value) : base(CommandTokenType.Quote, offset)
        {
            if (value != '\'' && value != '"')
            {
                throw new ArgumentException("Not valid quote");
            }

            Value = value.ToString();
        }

        /// <summary>
        /// Parse command line quotes
        /// </summary>
        /// <param name="commandLine">Command line</param>
        /// <param name="index">Index</param>
        /// <returns>CommandQuoteToken</returns>
        internal static CommandQuoteToken Parse(string commandLine, ref int index)
        {
            var c = commandLine[index];

            if (c == '\'' || c == '"')
            {
                index++;
                return new CommandQuoteToken(index - 1, c);
            }

            throw new ArgumentException("No quote found");
        }
    }
}
