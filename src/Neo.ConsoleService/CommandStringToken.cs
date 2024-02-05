// Copyright (C) 2015-2024 The Neo Project.
//
// CommandStringToken.cs file belongs to the neo project and is free
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
    [DebuggerDisplay("Value={Value}, RequireQuotes={RequireQuotes}")]
    internal class CommandStringToken : CommandToken
    {
        /// <summary>
        /// Require quotes
        /// </summary>
        public bool RequireQuotes { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="offset">Offset</param>
        /// <param name="value">Value</param>
        public CommandStringToken(int offset, string value) : base(CommandTokenType.String, offset)
        {
            Value = value;
            RequireQuotes = value.IndexOfAny(new char[] { '\'', '"' }) != -1;
        }

        /// <summary>
        /// Parse command line spaces
        /// </summary>
        /// <param name="commandLine">Command line</param>
        /// <param name="index">Index</param>
        /// <param name="quote">Quote (could be null)</param>
        /// <returns>CommandSpaceToken</returns>
        internal static CommandStringToken Parse(string commandLine, ref int index, CommandQuoteToken? quote)
        {
            int end;
            int offset = index;

            if (quote != null)
            {
                var ix = index;

                do
                {
                    end = commandLine.IndexOf(quote.Value[0], ix + 1);

                    if (end == -1)
                    {
                        throw new ArgumentException("String not closed");
                    }

                    if (IsScaped(commandLine, end - 1))
                    {
                        ix = end;
                        end = -1;
                    }
                }
                while (end < 0);
            }
            else
            {
                end = commandLine.IndexOf(' ', index + 1);
            }

            if (end == -1)
            {
                end = commandLine.Length;
            }

            var ret = new CommandStringToken(offset, commandLine.Substring(index, end - index));
            index += end - index;
            return ret;
        }

        private static bool IsScaped(string commandLine, int index)
        {
            // TODO: Scape the scape

            return (commandLine[index] == '\\');
        }
    }
}
