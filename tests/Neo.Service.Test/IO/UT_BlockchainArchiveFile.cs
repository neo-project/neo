// Copyright (C) 2015-2024 The Neo Project.
//
// UT_BlockchainArchiveFile.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Network.P2P.Payloads;
using Neo.Service.IO;
using Neo.Service.Tests.Helpers;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Neo.Service.Tests.IO
{
    public class UT_BlockchainArchiveFile : IAsyncLifetime
    {
        private static readonly string s_testArchFileName = Path.Combine(Path.GetTempPath(), $"Test.{Random.Shared.Next()}.zip");

        private BlockchainArchiveFile? _blockchainFile;

        public Task InitializeAsync()
        {
            _blockchainFile = UT_Utilities.CreateNewBlockArchiveFile(s_testArchFileName);
            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            _blockchainFile?.Dispose();
            File.Delete(s_testArchFileName);
            return Task.CompletedTask;
        }

        [Fact]
        public void Test_Write_Block()
        {
            var randomBlock = UT_Builder.CreateRandomFilledBlock(1u);
            var result = Record.Exception(() => _blockchainFile?.Write(randomBlock));
            Assert.Null(result);
        }

        [Fact]
        public void Test_Read_Block()
        {
            Block? block = null;
            var result = Record.Exception(() => block = _blockchainFile?.Read(0u));

            Assert.Null(result);
            Assert.NotNull(block);
            Assert.Equal(0u, block.Index);
            Assert.Equal(UInt256.Zero, block.MerkleRoot);
        }
    }
}
