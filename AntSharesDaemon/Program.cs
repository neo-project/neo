using AntShares.Shell;
using System;
using System.IO;

namespace AntShares
{
    static class Program
    {
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

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            new MainService().Run(args);
        }
    }
}
