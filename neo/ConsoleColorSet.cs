using System;

namespace Neo
{
    internal class ConsoleColorSet
    {
        #region Constants

        public static readonly ConsoleColorSet Debug = new ConsoleColorSet(ConsoleColor.Cyan);
        public static readonly ConsoleColorSet Info = new ConsoleColorSet(ConsoleColor.White);
        public static readonly ConsoleColorSet Warning = new ConsoleColorSet(ConsoleColor.Yellow);
        public static readonly ConsoleColorSet Error = new ConsoleColorSet(ConsoleColor.Red);
        public static readonly ConsoleColorSet Fatal = new ConsoleColorSet(ConsoleColor.Red);

        #endregion

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