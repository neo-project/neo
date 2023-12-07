using System;

namespace Neo.ConsoleService
{
    public static class ConsoleHelper
    {
        private static readonly ConsoleColorSet InfoColor = new(ConsoleColor.Cyan);
        private static readonly ConsoleColorSet WarningColor = new(ConsoleColor.Yellow);
        private static readonly ConsoleColorSet ErrorColor = new(ConsoleColor.Red);

        /// <summary>
        /// Info handles message in the format of "[tag]:[message]",
        /// avoid using Info if the `tag` is too long
        /// </summary>
        /// <param name="values">The log message in pairs of (tag, message)</param>
        public static void Info(params string[] values)
        {
            var currentColor = new ConsoleColorSet();

            for (int i = 0; i < values.Length; i++)
            {
                if (i % 2 == 0)
                    InfoColor.Apply();
                else
                    currentColor.Apply();
                Console.Write(values[i]);
            }
            currentColor.Apply();
            Console.WriteLine();
        }

        /// <summary>
        /// Use warning if something unexpected happens
        /// or the execution result is not correct.
        /// Also use warning if you just want to remind
        /// user of doing something.
        /// </summary>
        /// <param name="msg">Warning message</param>
        public static void Warning(string msg)
        {
            Log("Warning", WarningColor, msg);
        }

        /// <summary>
        /// Use Error if the verification or input format check fails
        /// or exception that breaks the execution of interactive
        /// command throws.
        /// </summary>
        /// <param name="msg">Error message</param>
        public static void Error(string msg)
        {
            Log("Error", ErrorColor, msg);
        }

        private static void Log(string tag, ConsoleColorSet colorSet, string msg)
        {
            var currentColor = new ConsoleColorSet();

            colorSet.Apply();
            Console.Write($"{tag}: ");
            currentColor.Apply();
            Console.WriteLine(msg);
        }
    }
}
