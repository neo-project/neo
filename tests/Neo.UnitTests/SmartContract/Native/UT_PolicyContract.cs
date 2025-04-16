// Copyright (C) 2015-2025 The Neo Project.
//
// UT_PolicyContract.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM.Types;
using System;
using System.Numerics;
using Boolean = Neo.VM.Types.Boolean;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_PolicyContract
    {
        private DataCache _snapshotCache;

        [TestInitialize]
        public void TestSetup()
        {
            _snapshotCache = TestBlockchain.GetTestSnapshotCache();

            ApplicationEngine engine = ApplicationEngine.Create(TriggerType.OnPersist, null, _snapshotCache, new Block { Header = new Header() }, settings: TestBlockchain.TheNeoSystem.Settings, gas: 0);
            NativeContract.ContractManagement.OnPersistAsync(engine);
        }

        [TestMethod]
        public void Check_Default()
        {
            var snapshot = _snapshotCache.CloneCache();

            var ret = NativeContract.Policy.Call(snapshot, "getFeePerByte");
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(1000, ret.GetInteger());

            ret = NativeContract.Policy.Call(snapshot, "getAttributeFee", new ContractParameter(ContractParameterType.Integer) { Value = (BigInteger)(byte)TransactionAttributeType.Conflicts });
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(PolicyContract.DefaultAttributeFee, ret.GetInteger());

            Assert.ThrowsExactly<InvalidOperationException>(() => _ = NativeContract.Policy.Call(snapshot, "getAttributeFee", new ContractParameter(ContractParameterType.Integer) { Value = (BigInteger)byte.MaxValue }));
        }

        [TestMethod]
        public void Check_SetAttributeFee()
        {
            var snapshot = _snapshotCache.CloneCache();

            // Fake blockchain
            Block block = new()
            {
                Header = new Header
                {
                    Index = 1000,
                    PrevHash = UInt256.Zero
                }
            };

            var attr = new ContractParameter(ContractParameterType.Integer) { Value = (BigInteger)(byte)TransactionAttributeType.Conflicts };

            // Without signature
            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(), block,
                "setAttributeFee", attr, new ContractParameter(ContractParameterType.Integer) { Value = 100500 });
            });

            var ret = NativeContract.Policy.Call(snapshot, "getAttributeFee", attr);
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(0, ret.GetInteger());

            // With signature, wrong value
            UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                    "setAttributeFee", attr, new ContractParameter(ContractParameterType.Integer) { Value = 11_0000_0000 });
            });

            ret = NativeContract.Policy.Call(snapshot, "getAttributeFee", attr);
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(0, ret.GetInteger());

            // Proper set
            ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "setAttributeFee", attr, new ContractParameter(ContractParameterType.Integer) { Value = 300300 });
            Assert.IsTrue(ret.IsNull);

            ret = NativeContract.Policy.Call(snapshot, "getAttributeFee", attr);
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(300300, ret.GetInteger());

            // Set to zero
            ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "setAttributeFee", attr, new ContractParameter(ContractParameterType.Integer) { Value = 0 });
            Assert.IsTrue(ret.IsNull);

            ret = NativeContract.Policy.Call(snapshot, "getAttributeFee", attr);
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(0, ret.GetInteger());
        }

        [TestMethod]
        public void Check_SetFeePerByte()
        {
            var snapshot = _snapshotCache.CloneCache();

            // Fake blockchain

            Block block = new()
            {
                Header = new Header
                {
                    Index = 1000,
                    PrevHash = UInt256.Zero
                }
            };

            // Without signature

            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(), block,
                "setFeePerByte", new ContractParameter(ContractParameterType.Integer) { Value = 1 });
            });

            var ret = NativeContract.Policy.Call(snapshot, "getFeePerByte");
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(1000, ret.GetInteger());

            // With signature
            UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);
            ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "setFeePerByte", new ContractParameter(ContractParameterType.Integer) { Value = 1 });
            Assert.IsTrue(ret.IsNull);

            ret = NativeContract.Policy.Call(snapshot, "getFeePerByte");
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(1, ret.GetInteger());
        }

        [TestMethod]
        public void Check_SetBaseExecFee()
        {
            var snapshot = _snapshotCache.CloneCache();

            // Fake blockchain

            Block block = new()
            {
                Header = new Header
                {
                    Index = 1000,
                    PrevHash = UInt256.Zero
                }
            };

            // Without signature

            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(), block,
                "setExecFeeFactor", new ContractParameter(ContractParameterType.Integer) { Value = 50 });
            });

            var ret = NativeContract.Policy.Call(snapshot, "getExecFeeFactor");
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(30, ret.GetInteger());

            // With signature, wrong value
            UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                    "setExecFeeFactor", new ContractParameter(ContractParameterType.Integer) { Value = 100500 });
            });

            ret = NativeContract.Policy.Call(snapshot, "getExecFeeFactor");
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(30, ret.GetInteger());

            // Proper set
            ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "setExecFeeFactor", new ContractParameter(ContractParameterType.Integer) { Value = 50 });
            Assert.IsTrue(ret.IsNull);

            ret = NativeContract.Policy.Call(snapshot, "getExecFeeFactor");
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(50, ret.GetInteger());
        }

        [TestMethod]
        public void Check_SetStoragePrice()
        {
            var snapshot = _snapshotCache.CloneCache();

            // Fake blockchain

            Block block = new()
            {
                Header = new Header
                {
                    Index = 1000,
                    PrevHash = UInt256.Zero
                }
            };

            // Without signature

            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(), block,
                "setStoragePrice", new ContractParameter(ContractParameterType.Integer) { Value = 100500 });
            });

            var ret = NativeContract.Policy.Call(snapshot, "getStoragePrice");
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(100000, ret.GetInteger());

            // With signature, wrong value
            UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                    "setStoragePrice", new ContractParameter(ContractParameterType.Integer) { Value = 100000000 });
            });

            ret = NativeContract.Policy.Call(snapshot, "getStoragePrice");
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(100000, ret.GetInteger());

            // Proper set
            ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "setStoragePrice", new ContractParameter(ContractParameterType.Integer) { Value = 300300 });
            Assert.IsTrue(ret.IsNull);

            ret = NativeContract.Policy.Call(snapshot, "getStoragePrice");
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(300300, ret.GetInteger());
        }

        [TestMethod]
        public void Check_SetMaxValidUntilBlockIncrement()
        {
            var snapshot = _snapshotCache.CloneCache();

            // Fake blockchain
            Block block = new()
            {
                Header = new Header
                {
                    Index = 1000,
                    PrevHash = UInt256.Zero
                }
            };

            // Without signature
            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(), block,
                "setMaxValidUntilBlockIncrement", new ContractParameter(ContractParameterType.Integer) { Value = 123 });
            });

            var ret = NativeContract.Policy.Call(snapshot, "getMaxValidUntilBlockIncrement");
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(5760, ret.GetInteger());

            // With signature, wrong value
            UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                    "setMaxValidUntilBlockIncrement", new ContractParameter(ContractParameterType.Integer) { Value = 100000000 });
            });

            ret = NativeContract.Policy.Call(snapshot, "getMaxValidUntilBlockIncrement");
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(5760, ret.GetInteger());

            // Proper set
            ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "setMaxValidUntilBlockIncrement", new ContractParameter(ContractParameterType.Integer) { Value = 123 });
            Assert.IsTrue(ret.IsNull);

            ret = NativeContract.Policy.Call(snapshot, "getMaxValidUntilBlockIncrement");
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(123, ret.GetInteger());

            // Update MaxTraceableBlocks value for further test.
            ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "setMaxTraceableBlocks", new ContractParameter(ContractParameterType.Integer) { Value = 6000 });
            Assert.IsTrue(ret.IsNull);

            // Set MaxValudUntilBlockIncrement to be larger or equal to MaxTraceableBlocks, it should fail.
            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                    "setMaxValidUntilBlockIncrement", new ContractParameter(ContractParameterType.Integer) { Value = 6000 });
            });
        }

        [TestMethod]
        public void Check_SetMillisecondsPerBlock()
        {
            var snapshot = _snapshotCache.CloneCache();

            // Fake blockchain.
            Block block = new()
            {
                Header = new Header
                {
                    Index = 1000,
                    PrevHash = UInt256.Zero
                }
            };

            // Without signature.
            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(), block,
                "setMillisecondsPerBlock", new ContractParameter(ContractParameterType.Integer) { Value = 123 });
            });

            var ret = NativeContract.Policy.Call(snapshot, "getMillisecondsPerBlock");
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(15_000, ret.GetInteger());

            // With signature, too big value.
            UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                    "setMillisecondsPerBlock", new ContractParameter(ContractParameterType.Integer) { Value = 30_001 });
            });

            // With signature, too small value.
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                    "setMillisecondsPerBlock", new ContractParameter(ContractParameterType.Integer) { Value = 0 });
            });

            // Ensure value is not changed.
            ret = NativeContract.Policy.Call(snapshot, "getMillisecondsPerBlock");
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(15_000, ret.GetInteger());

            // Proper set.
            ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "setMillisecondsPerBlock", new ContractParameter(ContractParameterType.Integer) { Value = 3_000 });
            Assert.IsTrue(ret.IsNull);

            // Ensure value is updated.
            ret = NativeContract.Policy.Call(snapshot, "getMillisecondsPerBlock");
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(3_000, ret.GetInteger());
        }

        [TestMethod]
        public void Check_BlockAccount()
        {
            var snapshot = _snapshotCache.CloneCache();

            // Fake blockchain

            Block block = new()
            {
                Header = new Header
                {
                    Index = 1000,
                    PrevHash = UInt256.Zero
                }
            };

            // Without signature

            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(UInt160.Zero), block,
                "blockAccount",
                new ContractParameter(ContractParameterType.ByteArray) { Value = UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01").ToArray() });
            });

            // With signature

            UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);
            var ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
              "blockAccount",
              new ContractParameter(ContractParameterType.ByteArray) { Value = UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01").ToArray() });
            Assert.IsInstanceOfType(ret, typeof(Boolean));
            Assert.IsTrue(ret.GetBoolean());

            // Same account
            ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "blockAccount",
                new ContractParameter(ContractParameterType.ByteArray) { Value = UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01").ToArray() });
            Assert.IsInstanceOfType(ret, typeof(Boolean));
            Assert.IsFalse(ret.GetBoolean());

            // Account B

            ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "blockAccount",
                new ContractParameter(ContractParameterType.ByteArray) { Value = UInt160.Parse("0xb400ff00ff00ff00ff00ff00ff00ff00ff00ff01").ToArray() });
            Assert.IsInstanceOfType(ret, typeof(Boolean));
            Assert.IsTrue(ret.GetBoolean());

            // Check

            Assert.IsFalse(NativeContract.Policy.IsBlocked(snapshot, UInt160.Zero));
            Assert.IsTrue(NativeContract.Policy.IsBlocked(snapshot, UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01")));
            Assert.IsTrue(NativeContract.Policy.IsBlocked(snapshot, UInt160.Parse("0xb400ff00ff00ff00ff00ff00ff00ff00ff00ff01")));
        }

        [TestMethod]
        public void Check_Block_UnblockAccount()
        {
            var snapshot = _snapshotCache.CloneCache();

            // Fake blockchain

            Block block = new()
            {
                Header = new Header
                {
                    Index = 1000,
                    PrevHash = UInt256.Zero
                }
            };
            UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);

            // Block without signature

            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                var ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(), block,
                "blockAccount", new ContractParameter(ContractParameterType.Hash160) { Value = UInt160.Zero });
            });

            Assert.IsFalse(NativeContract.Policy.IsBlocked(snapshot, UInt160.Zero));

            // Block with signature

            var ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "blockAccount", new ContractParameter(ContractParameterType.Hash160) { Value = UInt160.Zero });
            Assert.IsInstanceOfType(ret, typeof(Boolean));
            Assert.IsTrue(ret.GetBoolean());

            Assert.IsTrue(NativeContract.Policy.IsBlocked(snapshot, UInt160.Zero));

            // Unblock without signature

            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(), block,
                "unblockAccount", new ContractParameter(ContractParameterType.Hash160) { Value = UInt160.Zero });
            });

            Assert.IsTrue(NativeContract.Policy.IsBlocked(snapshot, UInt160.Zero));

            // Unblock with signature

            ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "unblockAccount", new ContractParameter(ContractParameterType.Hash160) { Value = UInt160.Zero });
            Assert.IsInstanceOfType(ret, typeof(Boolean));
            Assert.IsTrue(ret.GetBoolean());

            Assert.IsFalse(NativeContract.Policy.IsBlocked(snapshot, UInt160.Zero));
        }

        [TestMethod]
        public void Check_SetMaxTraceableBlocks()
        {
            var snapshot = _snapshotCache.CloneCache();

            // Fake blockchain.
            Block block = new()
            {
                Header = new Header
                {
                    Index = 1000,
                    PrevHash = UInt256.Zero
                }
            };

            // Without signature.
            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(), block,
                "setMaxTraceableBlocks", new ContractParameter(ContractParameterType.Integer) { Value = 123 });
            });

            var ret = NativeContract.Policy.Call(snapshot, "getMaxTraceableBlocks");
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(2_102_400, ret.GetInteger());

            // With signature, too big value.
            UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                    "setMaxTraceableBlocks", new ContractParameter(ContractParameterType.Integer) { Value = 2_102_401 });
            });

            // With signature, too small value.
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                    "setMaxTraceableBlocks", new ContractParameter(ContractParameterType.Integer) { Value = 0 });
            });

            // With signature, lower or equal to MaxValidUntilBlockIncrement.
            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                    "setMaxTraceableBlocks", new ContractParameter(ContractParameterType.Integer) { Value = 5760 });
            });

            // Ensure value is not changed.
            ret = NativeContract.Policy.Call(snapshot, "getMaxTraceableBlocks");
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(2102400, ret.GetInteger());

            // Proper set.
            ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "setMaxTraceableBlocks", new ContractParameter(ContractParameterType.Integer) { Value = 5761 });
            Assert.IsTrue(ret.IsNull);

            // Ensure value is updated.
            ret = NativeContract.Policy.Call(snapshot, "getMaxTraceableBlocks");
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(5761, ret.GetInteger());

            // Larger value should be prohibited.
            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                    "setMaxTraceableBlocks", new ContractParameter(ContractParameterType.Integer) { Value = 5762 });
            });
        }
    }
}
