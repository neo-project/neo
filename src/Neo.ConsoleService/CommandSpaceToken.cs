// Copyright (C) 2016-2023 The Neo Project.
// 
// The Neo.ConsoleService is free software distributed under the MIT 
// software license, see the accompanying file LICENSE in the main directory
// of the project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Diagnostics;

namespace Neo.ConsoleService
{
    [DebuggerDisplay("Value={Value}, Count={Count}")]
    internal class CommandSpaceToken : CommandToken
    {
        /// <summary>
        /// Count
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="offset">Offset</param>
        /// <param name="count">Count</param>
        public CommandSpaceToken(int offset, int count) : base(CommandTokenType.Space, offset)
        {
            Value = "".PadLeft(count, ' ');
            Count = count;
        }

        /// <summary>
        /// Parse command line spaces
        /// </summary>
        /// <param name="commandLine">Command line</param>
        /// <param name="index">Index</param>
        /// <returns>CommandSpaceToken</returns>
        internal static CommandSpaceToken Parse(string commandLine, ref int index)
        {
            int offset = index;
            int count = 0;

            for (int ix = index, max = commandLine.Length; ix < max; ix++)
            {
                if (commandLine[ix] == ' ')
                {
                    count++;
                }
                else
                {
                    break;
                }
            }

            if (count == 0) throw new ArgumentException("No spaces found");

            index += count;
            return new CommandSpaceToken(offset, count);
        }
    }
}
