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
using Microsoft.Extensions.Options;
using Neo.Hosting.App.Configuration;
using Neo.Hosting.App.Host.Service;
using Neo.IO;
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
        public class BlocksExportCommand : Command
        {
            public BlocksExportCommand() : base("blocks", "Export blocks to an offline archive file")
            {
                var startOption = new Option<uint>(new[] { "--start", "-s" }, () => 0, "The block height where to begin");
                var countOption = new Option<uint>(new[] { "--count", "-c" }, () => uint.MaxValue, "The total blocks to be written");
                var fileOption = new Option<FileInfo>(new[] { "--file", "-f" }, "The output filename");

                startOption.AddValidator(result =>
                {
                    var startOptionValue = result.GetValueForOption(startOption);
                    if (startOptionValue < 0)
                        result.ErrorMessage = "Must be greater than -1";
                });

                countOption.AddValidator(result =>
                {
                    var countOptionValue = result.GetValueForOption(countOption);
                    if (countOptionValue < 1)
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
                public FileInfo File { get; set; }

                private readonly Progress<uint> _progress;
                private readonly NeoSystemHostedService _neoSystemHostedService;
                private readonly ILoggerFactory _loggerFactory;
                private readonly ILogger? _logger;

                public Handler(
                    NeoSystemHostedService neoSystemService,
                    ILoggerFactory loggerFactory,
                    IOptions<NeoOptions> options)
                {
                    _neoSystemHostedService = neoSystemService;
                    _progress = new Progress<uint>();
                    _progress.ProgressChanged += WriteBlocksToAccFileProgressChanged;

                    var neoOptions = options.Value;
                    var fileName = Path.Combine(neoOptions.Storage.Archive.Path, neoOptions.Storage.Archive.FileName);
                    File = new FileInfo(fileName);

                    _loggerFactory = loggerFactory;
                    _logger = _loggerFactory.CreateLogger(File.FullName ?? typeof(ExportCommand).Name);
                }

                public async Task<int> InvokeAsync(InvocationContext context)
                {
                    var host = context.GetHost();
                    var stoppingToken = context.GetCancellationToken();

                    await host.StartAsync(stoppingToken);
                    await _neoSystemHostedService.StartAsync(stoppingToken);

                    var neoSystem = _neoSystemHostedService.NeoSystem ?? throw new NullReferenceException("NeoSystem");
                    var currentBlockHeight = NativeContract.Ledger.CurrentIndex(neoSystem.StoreView);
                    Count = Math.Min(Count, currentBlockHeight - Start);

                    var writeBlocksToAccFileTask = Task.Run(
                        () => WriteBlocksToAccFile(neoSystem, Start, Count, File.FullName, true, stoppingToken),
                        stoppingToken);

                    await writeBlocksToAccFileTask;
                    await host.StopAsync(stoppingToken);

                    return 0;
                }

                public int Invoke(InvocationContext context)
                {
                    throw new NotImplementedException();
                }

                private void WriteBlocksToAccFile(
                    NeoSystem neoSystem, uint start = 0, uint count = uint.MaxValue,
                    string path = $"chain.0.acc", bool writeStart = true, CancellationToken cancellationToken = default)
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