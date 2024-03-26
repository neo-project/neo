// Copyright (C) 2015-2024 The Neo Project.
//
// ExportCommand.Blocks.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using Neo.IO;
using Neo.Hosting.App.Hosting;
using Neo.SmartContract.Native;
using System;
using System.CommandLine;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Hosting.App.CommandLine
{
    internal partial class ExportCommand
    {
        public class BlocksCommand : Command
        {
            public BlocksCommand() : base("blocks", "Export blocks to an offline archive file")
            {
                var startOption = new Option<uint>(new[] { "--start", "-s" }, () => 1, "The block height where to begin");
                var countOption = new Option<uint>(new[] { "--count", "-c" }, () => uint.MaxValue, "The total blocks to be written");
                var fileOption = new Option<FileInfo>(new[] { "--file", "-f" }, () => new FileInfo("chain.1.acc"), "The output filename");

                startOption.AddValidator(result =>
                {
                    var startOptionValue = result.GetValueForOption(startOption);
                    if (startOptionValue == 0)
                        result.ErrorMessage = "Must be greater than 0";
                });

                countOption.AddValidator(result =>
                {
                    var countOptionValue = result.GetValueForOption(countOption);
                    if (countOptionValue == 0)
                        result.ErrorMessage = "Must be greater than 0";
                });

                AddOption(startOption);
                AddOption(countOption);
                AddOption(fileOption);
            }

            public new sealed class Handler : ICommandHandler
            {
                private static uint s_previousPercentValue = 0;

                public uint Start { get; set; }
                public uint Count { get; set; }
                public required FileInfo File { get; set; }

                private readonly Progress<uint> _progress;
                private readonly NeoSystemService _neoSystemService;
                private readonly ILogger<ExportCommand> _logger;

                public Handler(
                    NeoSystemService neoSystemService,
                    ILoggerFactory loggerFactory)
                {
                    _neoSystemService = neoSystemService;
                    _logger = loggerFactory.CreateLogger<ExportCommand>();
                    _progress = new Progress<uint>();
                    _progress.ProgressChanged += WriteBlocksToAccFileProgressChanged;
                }

                public Task<int> InvokeAsync(InvocationContext context)
                {
                    var host = context.GetHost();

                    var neoSystem = _neoSystemService.NeoSystem ?? throw new NullReferenceException("NeoSystem");
                    var currentBlockHeight = NativeContract.Ledger.CurrentIndex(neoSystem.StoreView);
                    Count = Math.Min(Count, currentBlockHeight - Start);

                    var writeBlocksToAccFileTask = Task.Factory.StartNew(
                        () => WriteBlocksToAccFile(neoSystem, Start, Count, File.FullName, true, context.GetCancellationToken()),
                        context.GetCancellationToken());

                    writeBlocksToAccFileTask.Wait();

                    return Task.FromResult(0);
                }

                public int Invoke(InvocationContext context)
                {
                    throw new NotImplementedException();
                }

                private void WriteBlocksToAccFile(
                    NeoSystem neoSystem, uint start = 1, uint count = uint.MaxValue,
                    string path = $"chain.1.acc", bool writeStart = true, CancellationToken cancellationToken = default)
                {
                    var end = start + count - 1;
                    using var fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.WriteThrough);

                    if (fs.Length > 0)
                    {
                        var buffer = new byte[sizeof(uint)];
                        if (writeStart)
                        {
                            fs.Seek(sizeof(uint), SeekOrigin.Begin);
                            fs.Read(buffer, 0, buffer.Length);
                            start += BitConverter.ToUInt32(buffer, 0);
                            fs.Seek(sizeof(uint), SeekOrigin.Begin);
                        }
                        else
                        {
                            fs.Read(buffer, 0, buffer.Length);
                            start = BitConverter.ToUInt32(buffer, 0);
                            fs.Seek(0, SeekOrigin.Begin);
                        }
                    }
                    else
                    {
                        if (writeStart)
                            fs.Write(BitConverter.GetBytes(start), 0, sizeof(uint));
                    }

                    if (start <= end)
                        fs.Write(BitConverter.GetBytes(count), 0, sizeof(uint));

                    fs.Seek(0, SeekOrigin.End);

                    _logger?.LogInformation("Backup Started.");
                    for (var i = start; i <= end; i++, ((IProgress<uint>)_progress).Report((uint)(100.0d * i / end)))
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            _logger?.LogInformation("Backup is shutting down...");
                            break;
                        }

                        var block = NativeContract.Ledger.GetBlock(neoSystem.StoreView, i);
                        var array = block.ToArray();
                        fs.Write(BitConverter.GetBytes(array.Length), 0, sizeof(int));
                        fs.Write(array, 0, array.Length);
                    }
                }

                private void WriteBlocksToAccFileProgressChanged(object? sender, uint e)
                {
                    var shouldDisplay = false;

                    if (s_previousPercentValue + 1 == e)
                    {
                        s_previousPercentValue = e;
                        shouldDisplay = true;
                    }

                    if (shouldDisplay && s_previousPercentValue % 10 == 0)
                        _logger?.LogInformation("Backup {PercentCompleted}% Complete.", e);
                }
            }
        }
    }
}
