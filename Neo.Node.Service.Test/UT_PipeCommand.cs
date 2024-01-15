// Copyright (C) 2015-2024 The Neo Project.
//
// UT_PipeCommand.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Text.Json;

namespace Neo.Node.Service.Test
{
    public class UT_PipeCommand
    {
        public UT_PipeCommand()
        {

        }

        [Fact]
        public void Test_CheckMethodExists()
        {
            var json = "{\"Command\":238,\"Args\":[]}";

            var commandObj = JsonSerializer.Deserialize<PipeCommand>(json, NodeService.JsonOptions);

            // basic checks
            Assert.NotNull(commandObj);
            Assert.Equal(CommandType.Exit, commandObj.Command);
            Assert.Empty(commandObj.Args);

            // pre-defined command exist
            Assert.True(PipeCommand.Contains(CommandType.Exit));
        }
    }
}
