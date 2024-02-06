// Copyright (C) 2015-2024 The Neo Project.
//
// UT_BlockchainBackup.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Service.IO;

namespace Neo.Service.Tests.IO
{
    public class UT_BlockchainBackup
    {
        private int i;

        [Fact]
        public void Test_Read_Blocks_From_Acc_File()
        {
            var count = 0u;
            var blocks = BlockchainBackup.ReadBlocksFromAccFile();

            foreach (var block in blocks)
            {
                Assert.Equal(++count, block.Index);
            }

            Assert.Equal(999u, count);
        }

        [Fact]
        public void Test_Read_Blocks_From_Barc_File()
        {
            var count = 0u;
            var blocks = BlockchainBackup.ReadBlocksFromBarFile(ProtocolSettings.Default.Network);

            foreach (var block in blocks)
            {
                Assert.Equal(++count, block.Index);
            }

            Assert.Equal(5u, count);
        }
    }
}
