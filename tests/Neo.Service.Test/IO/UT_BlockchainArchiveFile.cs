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

        [Theory]
        [InlineData(1u)]
        [InlineData(2u)]
        [InlineData(3u)]
        [InlineData(4u)]
        [InlineData(uint.MaxValue)]
        public void Test_Write_Block(uint blockIndex)
        {
            Assert.NotNull(_blockchainFile);

            var randomBlock = UT_Utilities.CreateRandomFilledBlock(blockIndex);
            var result = Record.Exception(() => _blockchainFile.Write(randomBlock));
            Assert.Null(result);
        }

        [Fact]
        public void Test_Read_Block()
        {
            Assert.NotNull(_blockchainFile);

            var blockResult = _blockchainFile.Read(0u);

            Assert.NotNull(blockResult);
            Assert.Equal(0u, blockResult.Index);
            Assert.Equal(UInt256.Zero, blockResult.MerkleRoot);
        }
    }
}
