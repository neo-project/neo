// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ContractManagement_Queries.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM.Types;
using System.Numerics;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_ContractManagement_Queries
    {
        private DataCache _snapshot;

        [TestInitialize]
        public void Setup()
        {
            _snapshot = TestBlockchain.GetTestSnapshotCache().CloneCache();
        }

        [TestMethod]
        public void GetContract_NativeNeo_Exists()
        {
            var ret = NativeContract.ContractManagement.Call(_snapshot, "getContract",
                new ContractParameter(ContractParameterType.Hash160) { Value = NativeContract.NEO.Hash });
            Assert.IsFalse(ret.IsNull);
        }

        [TestMethod]
        public void GetContract_Missing_ReturnsNull()
        {
            var ret = NativeContract.ContractManagement.Call(_snapshot, "getContract",
                new ContractParameter(ContractParameterType.Hash160) { Value = UInt160.Zero });
            Assert.IsTrue(ret.IsNull);
        }

        [TestMethod]
        public void GetMinimumDeploymentFee_IsPositive()
        {
            var ret = NativeContract.ContractManagement.Call(_snapshot, "getMinimumDeploymentFee");
            Assert.IsInstanceOfType<Integer>(ret);
            Assert.IsTrue(ret.GetInteger() > 0);
        }

        [TestMethod]
        public void HasMethod_NativeSymbol_True()
        {
            var ret = NativeContract.ContractManagement.Call(_snapshot, "hasMethod",
                new ContractParameter(ContractParameterType.Hash160) { Value = NativeContract.NEO.Hash },
                new ContractParameter(ContractParameterType.String) { Value = "symbol" },
                new ContractParameter(ContractParameterType.Integer) { Value = (BigInteger)0 });
            Assert.IsInstanceOfType<Boolean>(ret);
            Assert.IsTrue(ret.GetBoolean());
        }

        [TestMethod]
        public void HasMethod_MissingMethod_False()
        {
            var ret = NativeContract.ContractManagement.Call(_snapshot, "hasMethod",
                new ContractParameter(ContractParameterType.Hash160) { Value = NativeContract.NEO.Hash },
                new ContractParameter(ContractParameterType.String) { Value = "noSuchMethod" },
                new ContractParameter(ContractParameterType.Integer) { Value = (BigInteger)0 });
            Assert.IsFalse(ret.GetBoolean());
        }

        [TestMethod]
        public void GetContractById_NativeIds()
        {
            // Native contracts use negative ids; NEO is typically -5 or similar depending on order.
            var neo = NativeContract.ContractManagement.GetContract(_snapshot, NativeContract.NEO.Hash);
            Assert.IsNotNull(neo);
            var byId = NativeContract.ContractManagement.Call(_snapshot, "getContractById",
                new ContractParameter(ContractParameterType.Integer) { Value = neo.Id });
            Assert.IsFalse(byId.IsNull);
        }
    }
}
