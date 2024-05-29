// Copyright (C) 2015-2024 The Neo Project.
//
// PipeUnmanagedPayload.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Hosting.App.NamedPipes.Protocol.Payloads;
using Neo.Hosting.App.Tests.UTHelpers.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Neo.Hosting.App.Tests.NamedPipes.Protocol.Payloads
{
    public class UT_PipeUnmanagedPayload
        (ITestOutputHelper testOutputHelper)
    {
        private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

        [Fact]
        public void IPipeMessage_FromArray_And_ToArray_Data()
        {
            var expected = new PipeUnmanagedPayload<int>() { Value = 1 };
            var expectedBytes = expected.ToArray();
            var expectedHexString = Convert.ToHexString(expectedBytes);

            var actual = new PipeUnmanagedPayload<int>();
            actual.FromArray(expectedBytes);

            var actualBytes = actual.ToArray();
            var actualHexString = Convert.ToHexString(actualBytes);

            var className = nameof(PipeUnmanagedPayload<int>);
            var methodName = nameof(PipeUnmanagedPayload<int>.ToArray);
            _testOutputHelper.LogDebug(className, methodName, actualHexString, expectedHexString);

            Assert.Equal(expectedBytes, actualBytes);
        }
    }
}
