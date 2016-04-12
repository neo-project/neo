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
            File.WriteAllText("error.log", $"{ex.Message}\r\n{ex.StackTrace}");
#endif
        }

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            new MainService().Run(args);
        }
    }
}
