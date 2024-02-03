// Copyright (C) 2015-2024 The Neo Project.
//
// UT_Utilities.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using Neo.Service.IO;
using Xunit.Abstractions;

namespace Neo.Service.Tests.Helpers
{
    internal static class UT_Utilities
    {
        public static ILoggerFactory CreateLogFactory(ITestOutputHelper outputHelper) =>
            LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
                builder.AddProvider(new UT_XUnitLoggerProvider(outputHelper));
            });

        public static BlockchainArchiveFile CreateNewBlockArchiveFile(string fileName)
        {
            // Dispose to force save of the manifest file in archive
            using (var archFile = new BlockchainArchiveFile(fileName, ProtocolSettings.Default.Network))
                archFile.Write(UT_Builder.CreateRandomFilledBlock(0u));
            // Reopens archive file to read saved manifest
            return new BlockchainArchiveFile(fileName, ProtocolSettings.Default.Network);
        }
    }
}
