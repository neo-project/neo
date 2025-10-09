// Copyright (C) 2015-2025 The Neo Project.
//
// ProgramRootCommand.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.App.Extensions;
using Neo.App.Options;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.App.Commands
{
    internal class ProgramRootCommand : RootCommand
    {
        public ProgramRootCommand() : base("Neo Blockchain Command-line Interface")
        {

        }

        internal new sealed class Handler(NeoConfigurationOptions neoConfiguration) : ICommandHandler
        {
            private readonly NeoConfigurationOptions _neoConfiguration = neoConfiguration;

            public int Invoke(InvocationContext context) =>
                InvokeAsync(context).GetAwaiter().GetResult();

            public Task<int> InvokeAsync(InvocationContext context)
            {
                context.Console.WriteLine("       ============ Network ============");
                context.Console.WriteLine("                    Listen: {0}", _neoConfiguration.NetworkConfiguration.Listen);
                context.Console.WriteLine("                      Port: {0}", _neoConfiguration.NetworkConfiguration.Port);
                context.Console.WriteLine("         EnableCompression: {0}", _neoConfiguration.NetworkConfiguration.EnableCompression);
                context.Console.WriteLine("     MinDesiredConnections: {0}", _neoConfiguration.NetworkConfiguration.MinDesiredConnections);
                context.Console.WriteLine("            MaxConnections: {0}", _neoConfiguration.NetworkConfiguration.MaxConnections);
                context.Console.WriteLine("            MaxKnownHashes: {0}", _neoConfiguration.NetworkConfiguration.MaxKnownHashes);
                context.Console.WriteLine("  MaxConnectionsPerAddress: {0}", _neoConfiguration.NetworkConfiguration.MaxConnectionsPerAddress);
                context.Console.WriteLine();
                context.Console.WriteLine("       ============ Storage ============");
                context.Console.WriteLine("                    Engine: {0}", _neoConfiguration.StorageConfiguration.Engine);
                context.Console.WriteLine("                      Path: {0}", _neoConfiguration.StorageConfiguration.Path);
                context.Console.WriteLine();
                context.Console.WriteLine("       ============ Protocol ===========");
                context.Console.WriteLine("                   Network: {0}", _neoConfiguration.ProtocolConfiguration.Network);
                context.Console.WriteLine("            AddressVersion: {0}", _neoConfiguration.ProtocolConfiguration.AddressVersion);
                context.Console.WriteLine("      MillisecondsPerBlock: {0}", _neoConfiguration.ProtocolConfiguration.MillisecondsPerBlock);
                context.Console.WriteLine("   MaxTransactionsPerBlock: {0}", _neoConfiguration.ProtocolConfiguration.MaxTransactionsPerBlock);
                context.Console.WriteLine(" MemoryPoolMaxTransactions: {0}", _neoConfiguration.ProtocolConfiguration.MemoryPoolMaxTransactions);
                context.Console.WriteLine("        MaxTraceableBlocks: {0}", _neoConfiguration.ProtocolConfiguration.MaxTraceableBlocks);
                context.Console.WriteLine("                 HardForks: [{0}]", string.Join(", ", _neoConfiguration.ProtocolConfiguration.HardForks.Select(static s => $"\"{s.Key}:{s.Value}\"")));
                context.Console.WriteLine("    InitialGasDistribution: {0}", _neoConfiguration.ProtocolConfiguration.InitialGasDistribution);
                context.Console.WriteLine("           ValidatorsCount: {0}", _neoConfiguration.ProtocolConfiguration.ValidatorsCount);
                context.Console.WriteLine("                  SeedList: [{0}]", string.Join(", ", _neoConfiguration.ProtocolConfiguration.SeedList.Select(static s => $"\"{s}\"")));
                context.Console.WriteLine("          StandbyCommittee: [{0}]", string.Join(", ", _neoConfiguration.ProtocolConfiguration.StandbyCommittee.Select(static s => $"\"{s}\"")));

                var token = context.GetCancellationToken();
                while (token.IsCancellationRequested == false)
                    Thread.Sleep(1000);

                return Task.FromResult(0);
            }
        }
    }
}
