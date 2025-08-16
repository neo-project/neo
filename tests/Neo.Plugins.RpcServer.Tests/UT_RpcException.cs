// Copyright (C) 2015-2025 The Neo Project.
//
// UT_RpcException.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Neo.Plugins.RpcServer.Tests
{
    [TestClass]
    public class UT_RpcException
    {
        [TestMethod]
        public void TestThrow()
        {
            var rpcDataError = "RpcError_DATA";
            string nullParamFake = null;
            var paramFakeA = 0;
            var paramFakeB = 0;

            // Test 01 - ThrowIfNull with no data
            var exNullNoData = Assert.Throws<RpcException>(() => RpcException.ThrowIfNull(nullParamFake, nameof(nullParamFake), RpcError.InvalidParams));
            Assert.AreEqual(RpcError.InvalidParams.Code, exNullNoData.HResult);
            Assert.AreEqual("Invalid params - Parameter 'nullParamFake' is null", exNullNoData.Message);

            // Test 02 - ThrowIfNull with data
            var exNullWithData = Assert.Throws<RpcException>(() => RpcException.ThrowIfNull(nullParamFake, nameof(nullParamFake), RpcError.InvalidParams.WithData(rpcDataError)));
            Assert.AreEqual(RpcError.InvalidParams.Code, exNullWithData.HResult);
            Assert.AreEqual("Invalid params - RpcError_DATA", exNullWithData.Message);

            // Test 03 - ThrowIfTrue with no data
            var exCondTrueNoData = Assert.Throws<RpcException>(() => RpcException.ThrowIfTrue(paramFakeA == paramFakeB, RpcError.InvalidParams));
            Assert.AreEqual(RpcError.InvalidParams.Code, exCondTrueNoData.HResult);
            Assert.AreEqual("Invalid params - Condition paramFakeA == paramFakeB is true", exCondTrueNoData.Message);

            // Test 04 - ThrowIfTrue with data
            var exCondTrueWithData = Assert.Throws<RpcException>(() => RpcException.ThrowIfTrue(paramFakeA == paramFakeB, RpcError.InvalidParams.WithData(rpcDataError)));
            Assert.AreEqual(RpcError.InvalidParams.Code, exCondTrueWithData.HResult);
            Assert.AreEqual("Invalid params - RpcError_DATA", exCondTrueWithData.Message);

            // Test 05 - ThrowIfFalse with no data
            var exCondFalseNoData = Assert.Throws<RpcException>(() => RpcException.ThrowIfFalse(paramFakeA == (paramFakeB + 1), RpcError.InvalidParams));
            Assert.AreEqual(RpcError.InvalidParams.Code, exCondFalseNoData.HResult);
            Assert.AreEqual("Invalid params - Condition paramFakeA == (paramFakeB + 1) is false", exCondFalseNoData.Message);

            // Test 06 - ThrowIfFalse with data
            var exCondFalseWithData = Assert.Throws<RpcException>(() => RpcException.ThrowIfFalse(paramFakeA == (paramFakeB + 1), RpcError.InvalidParams.WithData(rpcDataError)));
            Assert.AreEqual(RpcError.InvalidParams.Code, exCondFalseWithData.HResult);
            Assert.AreEqual("Invalid params - RpcError_DATA", exCondFalseWithData.Message);
        }
    }
}
