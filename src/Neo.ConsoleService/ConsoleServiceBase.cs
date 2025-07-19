// Copyright (C) 2015-2025 The Neo Project.
//
// ConsoleServiceBase.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Loader;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.ConsoleService
{
    public abstract class ConsoleServiceBase
    {
        const int HistorySize = 100;

        protected virtual string? Depends => null;
        protected virtual string Prompt => "service";

        public abstract string ServiceName { get; }

        protected bool ShowPrompt { get; set; } = true;

        private bool _running;
        private readonly CancellationTokenSource _shutdownTokenSource = new();
        private readonly CountdownEvent _shutdownAcknowledged = new(1);
        private readonly Dictionary<string, List<ConsoleCommandMethod>> _verbs = new();
        private readonly Dictionary<string, object> _instances = new();
        private readonly Dictionary<Type, Func<IList<CommandToken>, bool, object>> _handlers = new();

        private readonly List<string> _commandHistory = new();

        /// <summary>
        /// Parse sequential arguments.
        /// For example, if a method defined as `void Method(string arg1, int arg2, bool arg3)`,
        /// the arguments will be parsed as `"arg1" 2 true`.
        /// </summary>
        /// <param name="method">the MethodInfo of the called method</param>
        /// <param name="args">the raw arguments</param>
        /// <returns>the parsed arguments</returns>
        /// <exception cref="ArgumentException">Missing argument</exception>
        internal object?[] ParseSequentialArguments(MethodInfo method, IList<CommandToken> args)
        {
            var parameters = method.GetParameters();
            var arguments = new List<object?>();
            foreach (var parameter in parameters)
            {
                if (TryProcessValue(parameter.ParameterType, args, parameter == parameters.Last(), out var value))
                {
                    arguments.Add(value);
                }
                else
                {
                    if (!parameter.HasDefaultValue)
                        throw new ArgumentException($"Missing value for parameter: {parameter.Name}");
                    arguments.Add(parameter.DefaultValue);
                }
            }
            return arguments.ToArray();
        }

        /// <summary>
        /// Parse indicator arguments.
        /// For example, if a method defined as `void Method(string arg1, int arg2, bool arg3)`,
        /// the arguments will be parsed as `Method --arg1 "arg1" --arg2 2 --arg3`.
        /// </summary>
        /// <param name="method">the MethodInfo of the called method</param>
        /// <param name="args">the raw arguments</param>
        /// <returns>the parsed arguments</returns>
        internal object?[] ParseIndicatorArguments(MethodInfo method, IList<CommandToken> args)
        {
            var parameters = method.GetParameters();
            if (parameters is null || parameters.Length == 0) return [];

            var arguments = parameters.Select(p => p.HasDefaultValue ? p.DefaultValue : null).ToArray();
            var noValues = parameters.Where(p => !p.HasDefaultValue).Select(p => p.Name).ToHashSet();
            for (int i = 0; i < args.Count; i++)
            {
                var token = args[i];
                if (!token.IsIndicator) continue;

                var paramName = token.Value.Substring(2); // Remove "--"
                var parameter = parameters.FirstOrDefault(p => string.Equals(p.Name, paramName));
                if (parameter == null) throw new ArgumentException($"Unknown parameter: {paramName}");

                var paramIndex = Array.IndexOf(parameters, parameter);
                if (i + 1 < args.Count && args[i + 1].IsWhiteSpace) i += 1; // Skip the white space token
                if (i + 1 < args.Count && !args[i + 1].IsIndicator) // Check if next token is a value (not an indicator)
                {
                    var valueToken = args[i + 1]; // Next token is the value for this parameter
                    if (!TryProcessValue(parameter.ParameterType, [args[i + 1]], false, out var value))
                        throw new ArgumentException($"Cannot parse value for parameter {paramName}: {valueToken.Value}");
                    arguments[paramIndex] = value;
                    noValues.Remove(paramName);
                    i += 1; // Skip the value token in next iteration
                }
                else
                {
                    if (parameter.ParameterType != typeof(bool)) // If parameter is not a bool and no value is provided
                        throw new ArgumentException($"Missing value for parameter: {paramName}");
                    arguments[paramIndex] = true;
                    noValues.Remove(paramName);
                }
            }

            if (noValues.Count > 0)
                throw new ArgumentException($"Missing value for parameters: {string.Join(',', noValues)}");
            return arguments;
        }

        private bool OnCommand(string commandLine)
        {
            if (string.IsNullOrEmpty(commandLine)) return true;

            var possibleHelp = "";
            var tokens = commandLine.Tokenize();
            var availableCommands = new List<(ConsoleCommandMethod Command, object?[] Arguments)>();
            foreach (var entries in _verbs.Values)
            {
                foreach (var command in entries)
                {
                    var consumed = command.IsThisCommand(tokens);
                    if (consumed <= 0) continue;

                    var args = tokens.Skip(consumed).ToList().Trim();
                    try
                    {
                        if (args.Any(u => u.IsIndicator))
                            availableCommands.Add((command, ParseIndicatorArguments(command.Method, args)));
                        else
                            availableCommands.Add((command, ParseSequentialArguments(command.Method, args)));
                    }
                    catch (Exception ex)
                    {
                        // Skip parse errors
                        possibleHelp = command.Key;
                        ConsoleHelper.Error($"{ex.InnerException?.Message ?? ex.Message}");
                    }
                }
            }

            if (availableCommands.Count == 0)
            {
                if (!string.IsNullOrEmpty(possibleHelp))
                {
                    OnHelpCommand(possibleHelp);
                    return true;
                }
                return false;
            }

            if (availableCommands.Count == 1)
            {
                var (command, arguments) = availableCommands[0];
                object? result = command.Method.Invoke(command.Instance, arguments);

                if (result is Task task) task.Wait();
                return true;
            }

            // Show Ambiguous call
            var ambiguousCommands = availableCommands.Select(u => u.Command.Key).Distinct().ToList();
            throw new ArgumentException($"Ambiguous calls for: {string.Join(',', ambiguousCommands)}");
        }

        private bool TryProcessValue(Type parameterType, IList<CommandToken> args, bool consumeAll, out object? value)
        {
            if (args.Count > 0)
            {
                if (_handlers.TryGetValue(parameterType, out var handler))
                {
                    value = handler(args, consumeAll);
                    return true;
                }

                if (parameterType.IsEnum)
                {
                    value = Enum.Parse(parameterType, args[0].Value, true);
                    return true;
                }
            }

            value = null;
            return false;
        }

        #region Commands

        private static string ParameterGuide(ParameterInfo info)
        {
            if (info.HasDefaultValue)
            {
                var defaultValue = info.DefaultValue?.ToString();
                return string.IsNullOrEmpty(defaultValue) ?
                    $"[ --{info.Name} {info.ParameterType.Name} ]" :
                    $"[ --{info.Name} {info.ParameterType.Name}({defaultValue}) ]";
            }
            return $"--{info.Name} {info.ParameterType.Name}";
        }

        /// <summary>
        /// Process "help" command
        /// </summary>
        [ConsoleCommand("help", Category = "Base Commands")]
        protected void OnHelpCommand(string key = "")
        {
            var withHelp = new List<ConsoleCommandMethod>();

            // Try to find a plugin with this name
            if (_instances.TryGetValue(key.Trim().ToLowerInvariant(), out var instance))
            {
                // Filter only the help of this plugin
                key = "";
                foreach (var commands in _verbs.Values)
                {
                    withHelp.AddRange(commands.Where(u => !string.IsNullOrEmpty(u.HelpCategory) && u.Instance == instance));
                }
            }
            else
            {
                // Fetch commands
                foreach (var commands in _verbs.Values)
                {
                    withHelp.AddRange(commands.Where(u => !string.IsNullOrEmpty(u.HelpCategory)));
                }
            }

            // Sort and show
            withHelp.Sort((a, b) =>
            {
                var category = string.Compare(a.HelpCategory, b.HelpCategory, StringComparison.Ordinal);
                return category == 0 ? string.Compare(a.Key, b.Key, StringComparison.Ordinal) : category;
            });

            if (string.IsNullOrEmpty(key) || key.Equals("help", StringComparison.InvariantCultureIgnoreCase))
            {
                string? last = null;
                foreach (var command in withHelp)
                {
                    if (last != command.HelpCategory)
                    {
                        Console.WriteLine($"{command.HelpCategory}:");
                        last = command.HelpCategory;
                    }

                    Console.Write($"\t{command.Key}");
                    Console.WriteLine(" " + string.Join(' ', command.Method.GetParameters().Select(ParameterGuide)));
                }
            }
            else
            {
                ShowHelpForCommand(key, withHelp);
            }
        }

        /// <summary>
        /// Show help for a specific command
        /// </summary>
        /// <param name="key">Command key</param>
        /// <param name="withHelp">List of commands</param>
        private void ShowHelpForCommand(string key, List<ConsoleCommandMethod> withHelp)
        {
            bool found = false;
            string helpMessage = string.Empty;
            string lastKey = string.Empty;
            foreach (var command in withHelp.Where(u => u.Key == key))
            {
                found = true;
                if (helpMessage != command.HelpMessage)
                {
                    Console.WriteLine($"{command.HelpMessage}");
                    helpMessage = command.HelpMessage;
                }

                if (lastKey != command.Key)
                {
                    Console.WriteLine("You can call this command like this:");
                    lastKey = command.Key;
                }

                Console.Write($"\t{command.Key}");
                Console.WriteLine(" " + string.Join(' ', command.Method.GetParameters().Select(ParameterGuide)));

                var parameters = command.Method.GetParameters();
                if (parameters.Length > 0)  // Show parameter info for this command
                {
                    Console.WriteLine($"Parameters for command `{command.Key}`:");
                    foreach (var item in parameters)
                    {
                        var info = item.HasDefaultValue ? $"(optional, default: {item.DefaultValue?.ToString() ?? "null"})" : "(required)";
                        Console.WriteLine($"\t{item.Name}: {item.ParameterType.Name} {info}");
                    }
                }
            }

            if (!found)
                throw new ArgumentException($"Command '{key}' not found. Use 'help' to see available commands.");

            Console.WriteLine();
            Console.WriteLine("You can also use 'how to input' to see how to input arguments.");
        }

        /// <summary>
        /// Show `how to input` guide
        /// </summary>
        [ConsoleCommand("how to input", Category = "Base Commands")]
        internal void OnHowToInput()
        {
            Console.WriteLine("""
            1. Sequential Arguments (Positional)
            Arguments are provided in the order they appear in the method signature.
            Usage:
            > create wallet "path/to/wallet"
            > create wallet "path/to/wallet" "wif-or-file" "wallet-name"

            Note: String values can be quoted or unquoted. Use quotes for values with spaces.

            2. Indicator Arguments (Named Parameters)
            Arguments are provided with parameter names prefixed with "--", and parameter order doesn't matter.
            Usage:
            > create wallet --path "path/to/wallet"
            > create wallet --path "path/to/wallet" --wifOrFile "wif-or-file" --walletName "wallet-name"

            3. Tips:
             - String: Can be quoted or unquoted, use quotes for spaces. It's recommended to use quotes for complex values.
             - String[]: Use comma-separated or space-separated values, If space separated, it must be the last argument.
             - UInt160, UInt256: Specified in hex format, for example: 0x1234567890abcdef1234567890abcdef12345678
             - Numeric: Standard number parsing
             - Boolean: Can be specified without a value (defaults to true), true/false, 1/0, yes/no, y/n
             - Enum: Case-insensitive enum value names
             - JSON: Input as JSON string
             - Escape characters: \\, \", \', \n, \r, \t, \v, \b, \f, \a, \e, \0, \ (whitespace).
                If want to input without escape, quote the value with backtick(`).
            """);
        }

        /// <summary>
        /// Process "clear" command
        /// </summary>
        [ConsoleCommand("clear", Category = "Base Commands", Description = "Clear is used in order to clean the console output.")]
        protected void OnClear()
        {
            Console.Clear();
        }

        /// <summary>
        /// Process "version" command
        /// </summary>
        [ConsoleCommand("version", Category = "Base Commands", Description = "Show the current version.")]
        protected void OnVersion()
        {
            Console.WriteLine(Assembly.GetEntryAssembly()!.GetName().Version);
        }

        /// <summary>
        /// Process "exit" command
        /// </summary>
        [ConsoleCommand("exit", Category = "Base Commands", Description = "Exit the node.")]
        protected void OnExit()
        {
            _running = false;
        }

        #endregion

        public virtual bool OnStart(string[] args)
        {
            // Register sigterm event handler
            AssemblyLoadContext.Default.Unloading += SigTermEventHandler;
            // Register sigint event handler
            Console.CancelKeyPress += CancelHandler;
            return true;
        }

        public virtual void OnStop()
        {
            _shutdownAcknowledged.Signal();
        }

        private void TriggerGracefulShutdown()
        {
            if (!_running) return;
            _running = false;
            _shutdownTokenSource.Cancel();
            // Wait for us to have triggered shutdown.
            _shutdownAcknowledged.Wait();
        }

        private void SigTermEventHandler(AssemblyLoadContext obj)
        {
            TriggerGracefulShutdown();
        }

        private void CancelHandler(object? sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            TriggerGracefulShutdown();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        protected ConsoleServiceBase()
        {
            // Register self commands
            RegisterCommandHandler<string>((args, consumeAll) =>
            {
                return consumeAll ? args.ConsumeAll() : args.Consume();
            });

            RegisterCommandHandler<string[]>((args, consumeAll) =>
            {
                return consumeAll
                    ? args.ConsumeAll().Split([',', ' '], StringSplitOptions.RemoveEmptyEntries)
                    : args.Consume().Split(',', ' ');
            });

            RegisterCommandHandler<string, byte>(false, str => byte.Parse(str));
            RegisterCommandHandler<string, bool>(false, str => str == "1" || str == "yes" || str == "y" || bool.Parse(str));
            RegisterCommandHandler<string, ushort>(false, str => ushort.Parse(str));
            RegisterCommandHandler<string, uint>(false, str => uint.Parse(str));
            RegisterCommandHandler<string, IPAddress>(false, IPAddress.Parse);
        }

        /// <summary>
        /// Register command handler
        /// </summary>
        /// <typeparam name="TRet">Return type</typeparam>
        /// <param name="handler">Handler</param>
        private void RegisterCommandHandler<TRet>(Func<IList<CommandToken>, bool, object> handler)
        {
            _handlers[typeof(TRet)] = handler;
        }

        /// <summary>
        /// Register command handler
        /// </summary>
        /// <typeparam name="T">Base type</typeparam>
        /// <typeparam name="TRet">Return type</typeparam>
        /// <param name="canConsumeAll">Can consume all</param>
        /// <param name="handler">Handler</param>
        public void RegisterCommandHandler<T, TRet>(bool canConsumeAll, Func<T, object> handler)
        {
            _handlers[typeof(TRet)] = (args, _) =>
            {
                var value = (T)_handlers[typeof(T)](args, canConsumeAll);
                return handler(value);
            };
        }

        /// <summary>
        /// Register command handler
        /// </summary>
        /// <typeparam name="T">Base type</typeparam>
        /// <typeparam name="TRet">Return type</typeparam>
        /// <param name="handler">Handler</param>
        public void RegisterCommandHandler<T, TRet>(Func<T, object> handler)
        {
            _handlers[typeof(TRet)] = (args, consumeAll) =>
            {
                var value = (T)_handlers[typeof(T)](args, consumeAll);
                return handler(value);
            };
        }

        /// <summary>
        /// Register commands
        /// </summary>
        /// <param name="instance">Instance</param>
        /// <param name="name">Name</param>
        public void RegisterCommand(object instance, string? name = null)
        {
            if (!string.IsNullOrEmpty(name))
            {
                _instances.Add(name.ToLowerInvariant(), instance);
            }

            foreach (var method in instance.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                foreach (var attribute in method.GetCustomAttributes<ConsoleCommandAttribute>())
                {
                    // Check handlers
                    if (!method.GetParameters().All(u => u.ParameterType.IsEnum || _handlers.ContainsKey(u.ParameterType)))
                    {
                        throw new ArgumentException($"Handler not found for the command: {method}");
                    }

                    // Add command
                    var command = new ConsoleCommandMethod(instance, method, attribute);
                    if (!_verbs.TryGetValue(command.Key, out var commands))
                    {
                        _verbs.Add(command.Key, [command]);
                    }
                    else
                    {
                        commands.Add(command);
                    }
                }
            }
        }

        public void Run(string[] args)
        {
            if (Environment.UserInteractive)
            {
                if (args.Length == 1 && args[0] == "/install")
                {
                    if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                    {
                        ConsoleHelper.Warning("Only support for installing services on Windows.");
                        return;
                    }

                    var fileName = Process.GetCurrentProcess().MainModule!.FileName;
                    var arguments = $"create {ServiceName} start= auto binPath= \"{fileName}\"";
                    if (!string.IsNullOrEmpty(Depends))
                    {
                        arguments += $" depend= {Depends}";
                    }

                    Process? process = Process.Start(new ProcessStartInfo
                    {
                        Arguments = arguments,
                        FileName = Path.Combine(Environment.SystemDirectory, "sc.exe"),
                        RedirectStandardOutput = true,
                        UseShellExecute = false
                    });
                    if (process is null)
                    {
                        ConsoleHelper.Error("Error installing the service with sc.exe.");
                    }
                    else
                    {
                        process.WaitForExit();
                        Console.Write(process.StandardOutput.ReadToEnd());
                    }
                }
                else if (args.Length == 1 && args[0] == "/uninstall")
                {
                    if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                    {
                        ConsoleHelper.Warning("Only support for installing services on Windows.");
                        return;
                    }
                    Process? process = Process.Start(new ProcessStartInfo
                    {
                        Arguments = string.Format("delete {0}", ServiceName),
                        FileName = Path.Combine(Environment.SystemDirectory, "sc.exe"),
                        RedirectStandardOutput = true,
                        UseShellExecute = false
                    });
                    if (process is null)
                    {
                        ConsoleHelper.Error("Error installing the service with sc.exe.");
                    }
                    else
                    {
                        process.WaitForExit();
                        Console.Write(process.StandardOutput.ReadToEnd());
                    }
                }
                else
                {
                    if (OnStart(args)) RunConsole();
                    OnStop();
                }
            }
            else
            {
                Debug.Assert(Environment.OSVersion.Platform == PlatformID.Win32NT);
#pragma warning disable CA1416
                ServiceBase.Run(new ServiceProxy(this));
#pragma warning restore CA1416
            }
        }

        private string? ReadTask()
        {
            var historyIndex = -1;
            var input = new StringBuilder();
            var cursor = 0;
            var promptLength = ShowPrompt ? Prompt.Length + 2 /* '> ' */ : 0;
            var rewrite = () =>
            {
                if (Console.WindowWidth > 0) Console.Write("\r" + new string(' ', Console.WindowWidth - 1));
                if (ShowPrompt)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($"\r{Prompt}> ");
                }

                Console.ForegroundColor = ConsoleColor.Yellow;
                if (input.Length > 0) Console.Write(input);
                Console.SetCursorPosition(promptLength + cursor, Console.CursorTop);
            };

            while (true)
            {
                var key = Console.ReadKey();
                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    var result = input.ToString();
                    if (!string.IsNullOrWhiteSpace(result)) _commandHistory.Add(result);
                    if (_commandHistory.Count > HistorySize) _commandHistory.RemoveAt(0);
                    return result;
                }
                else if (key.Key == ConsoleKey.Escape)
                {
                    Console.WriteLine('\r');
                    return string.Empty;
                }
                else if (key.Key == ConsoleKey.UpArrow)
                {
                    if (historyIndex < _commandHistory.Count - 1)
                    {
                        historyIndex++;
                        input.Clear();
                        input.Append(_commandHistory[_commandHistory.Count - 1 - historyIndex]);
                        cursor = input.Length;
                        rewrite();
                    }
                }
                else if (key.Key == ConsoleKey.DownArrow)
                {
                    if (historyIndex > 0)
                    {
                        historyIndex--;
                        input.Clear();
                        input.Append(_commandHistory[_commandHistory.Count - 1 - historyIndex]);
                        cursor = input.Length;
                        rewrite();
                    }
                    else
                    {
                        historyIndex = -1;
                        input.Clear();
                        cursor = 0;
                        rewrite();
                    }
                }
                else if (key.Key == ConsoleKey.LeftArrow)
                {
                    if (cursor > 0)
                    {
                        cursor--;
                        Console.SetCursorPosition(promptLength + cursor, Console.CursorTop);
                    }
                }
                else if (key.Key == ConsoleKey.RightArrow)
                {
                    if (cursor < input.Length)
                    {
                        cursor++;
                        Console.SetCursorPosition(promptLength + cursor, Console.CursorTop);
                    }
                }
                else if (key.Key == ConsoleKey.Backspace)
                {
                    if (cursor > 0)
                    {
                        input.Remove(cursor - 1, 1);
                        cursor--;
                    }
                    rewrite();
                }
                else
                {
                    input.Insert(cursor, key.KeyChar);
                    cursor++;
                    if (cursor < input.Length) rewrite();
                }
            }
        }

        protected string? ReadLine()
        {
            var isWin = Environment.OSVersion.Platform == PlatformID.Win32NT;
            Task<string?> readLineTask = !isWin ? Task.Run(ReadTask) : Task.Run(Console.ReadLine);
            try
            {
                readLineTask.Wait(_shutdownTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                return null;
            }

            return readLineTask.Result;
        }

        public virtual void RunConsole()
        {
            _running = true;
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                try
                {
                    Console.Title = ServiceName;
                }
                catch { }
            }

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.SetIn(new StreamReader(Console.OpenStandardInput(), Console.InputEncoding, false, ushort.MaxValue));

            while (_running)
            {
                if (ShowPrompt)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($"{Prompt}> ");
                }

                Console.ForegroundColor = ConsoleColor.Yellow;
                var line = ReadLine()?.Trim();
                if (line == null) break;
                Console.ForegroundColor = ConsoleColor.White;

                try
                {
                    if (!OnCommand(line))
                    {
                        ConsoleHelper.Error("Command not found");
                    }
                }
                catch (TargetInvocationException ex) when (ex.InnerException is not null)
                {
                    ConsoleHelper.Error(ex.InnerException.Message);
                }
                catch (Exception ex)
                {
                    ConsoleHelper.Error(ex.Message);
                }
            }

            Console.ResetColor();
        }
    }
}
