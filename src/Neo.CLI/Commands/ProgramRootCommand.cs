// Copyright (C) 2015-2024 The Neo Project.
//
// ProgramRootCommand.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.CLI.Extensions;
using Neo.CLI.Hosting;
using Neo.IO.Pipes;
using System;
using System.CommandLine;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.IO;
using System.IO.Pipelines;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.CLI.Commands
{
    internal class ProgramRootCommand : RootCommand
    {
        public ProgramRootCommand() : base("Neo N3 Command-Line Tool")
        {

        }

        public new sealed class Handler : ICommandHandler
        {
            private const int MinAllocBufferSize = 4096;

            private readonly NamedPipeClientStream _namedPipeClientStream;

            private readonly IDuplexPipe _applicationPipe;
            private readonly IDuplexPipe _transportPipe;

            private Task _receivingTask = Task.CompletedTask;
            private Task _sendingTask = Task.CompletedTask;

            public Handler()
            {
                _namedPipeClientStream = new NamedPipeClientStream(".", @"neo-cli", PipeDirection.InOut, System.IO.Pipes.PipeOptions.Asynchronous | System.IO.Pipes.PipeOptions.CurrentUserOnly | System.IO.Pipes.PipeOptions.WriteThrough);

                var pair = DuplexPipe.CreateConnectionPair();

                _transportPipe = pair.Transport;
                _applicationPipe = pair.Application;
            }

            public int Invoke(InvocationContext context)
            {
                throw new NotImplementedException();
            }

            public async Task<int> InvokeAsync(InvocationContext context)
            {
                var stoppingToken = context.GetCancellationToken();
                var host = context.GetHost();

                return await RunConsolePrompt(context, stoppingToken);
            }

            private static void PrintPrompt(IConsole console)
            {
                console.SetTerminalForegroundColor(ConsoleColor.Green);
                console.Write($"{NeoDefaults.ConsolePromptName} ");
                console.SetTerminalForegroundColor(ConsoleColor.White);
            }

            private async Task<int> RunConsolePrompt(
                InvocationContext context,
                CancellationToken cancellationToken)
            {
                context.Console.Clear();

                await _namedPipeClientStream.ConnectAsync(cancellationToken);

                _receivingTask = DoReceiveAsync();
                _sendingTask = DoSendAsync();

                while (cancellationToken.IsCancellationRequested == false)
                {

                    PrintPrompt(context.Console);

                    var inputLine = context.Console.ReadLine()?.Trim();

                    if (string.IsNullOrEmpty(inputLine) || string.IsNullOrWhiteSpace(inputLine))
                        continue;

                    if (inputLine.Equals("exit", StringComparison.InvariantCultureIgnoreCase) || inputLine.Equals("quit", StringComparison.InvariantCultureIgnoreCase))
                        return 0;

                    var output = _transportPipe.Output.AsStream();
                    var sw = new StreamWriter(output) { AutoFlush = true, };
                    sw.WriteLine(inputLine);

                    var input = _transportPipe.Input.AsStream();
                    var sr = new StreamReader(input);

                    string? receivedLine;
                    while ((receivedLine = sr.ReadLine()) != null)
                    {
                        if (receivedLine == "<END/>") break;
                        context.Console.WriteLine(receivedLine);
                    }
                }

                return 0;
            }

            private async Task DoReceiveAsync()
            {
                Exception? error = null;

                try
                {
                    var input = _applicationPipe.Output;

                    while (true)
                    {
                        var buffer = input.GetMemory();
                        var bytesReceived = await _namedPipeClientStream.ReadAsync(buffer);

                        if (bytesReceived == 0)
                            break;

                        input.Advance(bytesReceived);

                        var result = await input.FlushAsync();

                        if (result.IsCompleted || result.IsCanceled)
                            break;
                    }
                }
                catch (Exception ex)
                {
                    error = ex;
                }
                finally
                {
                    _applicationPipe.Output.Complete(error);
                }
            }

            private async Task DoSendAsync()
            {
                Exception? unexpectedError = null;

                try
                {
                    while (true)
                    {
                        var output = _applicationPipe.Input;
                        var result = await output.ReadAsync();

                        if (result.IsCanceled)
                            break;

                        var buffer = result.Buffer;
                        if (buffer.IsSingleSegment)
                            await _namedPipeClientStream.WriteAsync(buffer.First);
                        else
                        {
                            foreach (var segment in buffer)
                                await _namedPipeClientStream.WriteAsync(segment);
                        }

                        output.AdvanceTo(buffer.End);

                        if (result.IsCompleted)
                            break;
                    }
                }
                catch (ObjectDisposedException)
                {
                }
                catch (Exception ex)
                {
                    unexpectedError = ex;
                }
                finally
                {
                    _namedPipeClientStream.Close();

                    _applicationPipe.Input.Complete(unexpectedError);
                    _applicationPipe.Output.CancelPendingFlush();
                }
            }
        }
    }
}
