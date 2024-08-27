// Copyright (C) 2015-2024 The Neo Project.
//
// UT_PipeUnmanagedPayload.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Plugins.Models.Payloads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo.Plugins.NamedPipeService.Tests.Payloads
{
    [TestClass]
    public class UT_PipeUnmanagedPayload
    {
        [TestMethod]
        public void IPipeMessage_FromArray_And_ToArray_Int32()
        {
            var expected = new PipeUnmanagedPayload<int>() { Value = 1 };
            var expectedBytes = expected.ToByteArray();

            var actual = new PipeUnmanagedPayload<int>();
            actual.FromByteArray(expectedBytes);

            var actualBytes = actual.ToByteArray();

            CollectionAssert.AreEqual(expectedBytes, actualBytes);
            Assert.AreEqual(1, actual.Value);
        }
    }
}
