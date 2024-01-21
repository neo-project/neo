// Copyright (C) 2015-2024 The Neo Project.
//
// UT_NodeCommandPipeServer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Service.Tests
{
    public class UT_NodeCommandPipeServer
    {
        [Fact]
        public void Test()
        {
            var client = new UT_NamedPipeClientTests();
            var result = client.TestCommandExit();

            Assert.NotNull(result);
            Assert.True(result.AsValue().GetValue<bool>());
        }
    }
}
