using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security;
using System.ServiceProcess;

namespace AntShares.Services
{
    public abstract class ConsoleServiceBase
    {
        protected virtual string Depends => null;

        protected virtual string Prompt => "service";

        public abstract string ServiceName { get; }

        protected virtual bool OnCommand(string[] args)
        {
            switch (args[0].ToLower())
            {
                case "clear":
                    Console.Clear();
                    return true;
                case "exit":
                    return false;
                case "version":
                    Console.WriteLine(Assembly.GetEntryAssembly().GetName().Version);
                    return true;
                default:
                    Console.WriteLine("error");
                    return true;
            }
        }

        protected internal abstract void OnStart();

        protected internal abstract void OnStop();

        public static SecureString ReadSecureString(string prompt)
        {
            const string t = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";
            SecureString securePwd = new SecureString();
            ConsoleKeyInfo key;
            Console.Write(prompt);
            Console.Write(':');
            do
            {
                key = Console.ReadKey(true);
                if (t.IndexOf(key.KeyChar) != -1)
                {
                    securePwd.AppendChar(key.KeyChar);
                    Console.Write('*');
                }
                else if (key.Key == ConsoleKey.Backspace && securePwd.Length > 0)
                {
                    securePwd.RemoveAt(securePwd.Length - 1);
                    Console.Write(key.KeyChar);
                    Console.Write(' ');
                    Console.Write(key.KeyChar);
                }
            } while (key.Key != ConsoleKey.Enter);
            Console.WriteLine();
            securePwd.MakeReadOnly();
            return securePwd;
        }

        public void Run(string[] args)
        {
            if (Environment.UserInteractive)
            {
                if (args.Length > 0)
                {
                    switch (args[0])
                    {
                        case "/install":
                            {
                                string arguments = string.Format("create {0} start= auto binPath= \"{1}\"", ServiceName, Process.GetCurrentProcess().MainModule.FileName);
                                if (!string.IsNullOrEmpty(Depends))
                                {
                                    arguments += string.Format(" depend= {0}", Depends);
                                }
                                Process process = Process.Start(new ProcessStartInfo
                                {
                                    Arguments = arguments,
                                    FileName = Path.Combine(Environment.SystemDirectory, "sc.exe"),
                                    RedirectStandardOutput = true,
                                    UseShellExecute = false
                                });
                                process.WaitForExit();
                                Console.Write(process.StandardOutput.ReadToEnd());
                            }
                            break;
                        case "/uninstall":
                            {
                                Process process = Process.Start(new ProcessStartInfo
                                {
                                    Arguments = string.Format("delete {0}", ServiceName),
                                    FileName = Path.Combine(Environment.SystemDirectory, "sc.exe"),
                                    RedirectStandardOutput = true,
                                    UseShellExecute = false
                                });
                                process.WaitForExit();
                                Console.Write(process.StandardOutput.ReadToEnd());
                            }
                            break;
                    }
                }
                else
                {
                    OnStart();
                    RunConsole();
                    OnStop();
                }
            }
            else
            {
                ServiceBase.Run(new ServiceProxy(this));
            }
        }

        private void RunConsole()
        {
            bool running = true;
            Console.Title = ServiceName;
            while (running)
            {
                Console.Write($"{Prompt}>");
                string line = Console.ReadLine().Trim().ToLower();
                string[] args = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (args.Length == 0)
                    continue;
                try
                {
                    running = OnCommand(args);
                }
                catch
                {
                    Console.WriteLine("error");
                }
            }
        }
    }
}
