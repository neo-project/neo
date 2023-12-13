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

namespace Neo.ConsoleService
{
    public class ConsoleColorSet
    {
        public ConsoleColor Foreground;
        public ConsoleColor Background;

        /// <summary>
        /// Create a new color set with the current console colors
        /// </summary>
        public ConsoleColorSet() : this(Console.ForegroundColor, Console.BackgroundColor) { }

        /// <summary>
        /// Create a new color set
        /// </summary>
        /// <param name="foreground">Foreground color</param>
        public ConsoleColorSet(ConsoleColor foreground) : this(foreground, Console.BackgroundColor) { }

        /// <summary>
        /// Create a new color set
        /// </summary>
        /// <param name="foreground">Foreground color</param>
        /// <param name="background">Background color</param>
        public ConsoleColorSet(ConsoleColor foreground, ConsoleColor background)
        {
            Foreground = foreground;
            Background = background;
        }

        /// <summary>
        /// Apply the current set
        /// </summary>
        public void Apply()
        {
            Console.ForegroundColor = Foreground;
            Console.BackgroundColor = Background;
        }
    }
}
