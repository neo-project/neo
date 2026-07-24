// Copyright (C) 2015-2026 The Neo Project.
//
// UT_SmartContractEnums.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using Neo.SmartContract.Native;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_SmartContractEnums
    {
        [TestMethod]
        public void CallFlags_Combinations()
        {
            Assert.AreEqual(CallFlags.None, (CallFlags)0);
            Assert.AreEqual(CallFlags.States, CallFlags.ReadStates | CallFlags.WriteStates);
            Assert.AreEqual(CallFlags.ReadOnly, CallFlags.ReadStates | CallFlags.AllowCall);
            Assert.AreEqual(CallFlags.All, CallFlags.States | CallFlags.AllowCall | CallFlags.AllowNotify);
            Assert.IsTrue(CallFlags.All.HasFlag(CallFlags.AllowNotify));
            Assert.IsFalse(CallFlags.ReadOnly.HasFlag(CallFlags.WriteStates));
        }

        [TestMethod]
        public void TriggerType_Combinations()
        {
            Assert.AreEqual(TriggerType.System, TriggerType.OnPersist | TriggerType.PostPersist);
            Assert.AreEqual(TriggerType.All,
                TriggerType.OnPersist | TriggerType.PostPersist | TriggerType.Verification | TriggerType.Application);
            Assert.IsTrue(TriggerType.All.HasFlag(TriggerType.Application));
            Assert.IsFalse(TriggerType.System.HasFlag(TriggerType.Application));
        }

        [TestMethod]
        public void FindOptions_Combinations()
        {
            Assert.AreEqual(FindOptions.None, (FindOptions)0);
            Assert.IsTrue(FindOptions.All.HasFlag(FindOptions.KeysOnly));
            Assert.IsTrue(FindOptions.All.HasFlag(FindOptions.Backwards));
            Assert.IsTrue((FindOptions.PickField0 | FindOptions.DeserializeValues).HasFlag(FindOptions.DeserializeValues));
        }

        [TestMethod]
        public void ContainsTransactionType_Values()
        {
            Assert.AreEqual(0, (int)ContainsTransactionType.NotExist);
            Assert.AreEqual(1, (int)ContainsTransactionType.ExistsInPool);
            Assert.AreEqual(2, (int)ContainsTransactionType.ExistsInLedger);
        }

        [TestMethod]
        public void Role_Values()
        {
            Assert.AreEqual(4, (byte)Role.StateValidator);
            Assert.AreEqual(8, (byte)Role.Oracle);
            Assert.AreEqual(16, (byte)Role.NeoFSAlphabetNode);
            Assert.AreEqual(32, (byte)Role.P2PNotary);
        }

        [TestMethod]
        public void Hardfork_IsDefined()
        {
            Assert.IsTrue(System.Enum.IsDefined(typeof(Hardfork), Hardfork.HF_Huyao));
            Assert.IsTrue(System.Enum.IsDefined(typeof(Hardfork), Hardfork.HF_Aspidochelone));
        }
    }
}
