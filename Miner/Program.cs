using AntShares.Miner;
using System;
using System.IO;

namespace AntShares
{
    static class Program
    {
        private static readonly object LogSync = new object();

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
#if DEBUG
            Exception ex = (Exception)e.ExceptionObject;
            using (FileStream fs = new FileStream("error.log", FileMode.Create, FileAccess.Write, FileShare.None))
            using (StreamWriter w = new StreamWriter(fs))
            {
                w.WriteLine(ex.Message);
                w.WriteLine(ex.StackTrace);
                AggregateException ex2 = ex as AggregateException;
                if (ex2 != null)
                {
                    foreach (Exception inner in ex2.InnerExceptions)
                    {
                        w.WriteLine();
                        w.WriteLine(inner.Message);
                        w.WriteLine(inner.StackTrace);
                    }
                }
            }
#endif
        }

        internal static void Log(string message)
        {
            DateTime now = DateTime.Now;
            string line = $"[{now.TimeOfDay:hh\\:mm\\:ss}] {message}";
            Console.WriteLine(line);
            lock (LogSync)
            {
                string path = Path.Combine(AppContext.BaseDirectory, "Logs");
                Directory.CreateDirectory(path);
                path = Path.Combine(path, $"{now:yyyy-MM-dd}.log");
                File.AppendAllLines(path, new[] { line });
            }
        }

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            new MinerService().Run(args);
        }
    }
}
