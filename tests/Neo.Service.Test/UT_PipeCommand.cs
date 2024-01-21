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

using Neo.Service.Pipes;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Service.Tests
{
    public class UT_PipeCommand
    {

        public UT_PipeCommand()
        {
            PipeCommand.RegisterMethods(this);
        }

        [Fact]
        public void Test_DefinedMethodsExist()
        {
            Assert.True(PipeCommand.Contains(CommandType.Shutdown));
        }

        [Fact]
        public async Task Test_ResultOfMethod()
        {
            var pcom = new PipeCommand()
            {
                Exec = CommandType.Shutdown,
                Arguments = new Dictionary<string, string>(),
            };

            var taskObj = await pcom.ExecuteAsync(CancellationToken.None);

            Assert.NotNull(taskObj);
            Assert.IsType<bool>(taskObj);
            Assert.Equal(true, taskObj);
        }

        [PipeMethod(CommandType.Shutdown, Overwrite = true)]
        private Task<object> TestBoolFunction(IReadOnlyDictionary<string, string> args, CancellationToken cancellationToken) =>
            Task.FromResult<object>(true);
    }
}