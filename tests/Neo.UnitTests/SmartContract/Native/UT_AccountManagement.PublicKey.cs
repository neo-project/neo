// Copyright (C) 2015-2026 The Neo Project.
//
// UT_AccountManagement.PublicKey.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.UnitTests.SmartContract.Native
{
    public partial class UT_AccountManagement
    {
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CanonicalP256PublicKeyStrictlyNormalizesBothEncodings(bool uncompressed)
        {
            byte[] compressed = ECCurve.Secp256r1.G.EncodePoint(true).ToArray();
            byte[] raw = ECCurve.Secp256r1.G.EncodePoint(!uncompressed).ToArray();
            var snapshot = Snapshot();
            var abi = NativeContract.AccountManagement.GetContractState(Settings, 1).Manifest.Abi.GetMethod("canonicalP256PublicKey", 1);
            Assert.IsNotNull(abi);
            Assert.IsTrue(abi.Safe);
            Assert.AreEqual(ContractParameterType.ByteArray, abi.ReturnType);
            Assert.AreEqual(ContractParameterType.ByteArray, abi.Parameters[0].Type);
            using var engine = Invoke(snapshot, "canonicalP256PublicKey", [raw], trigger: TriggerType.Verification, signers: []);
            Assert.AreEqual(VMState.HALT, engine.State, engine.FaultException?.ToString());
            Assert.AreSequenceEqual(compressed, engine.ResultStack.Pop().GetSpan().ToArray());
            Assert.AreEqual(0, engine.Notifications.Count);
            Assert.AreSequenceEqual(raw, ECCurve.Secp256r1.G.EncodePoint(!uncompressed));
        }

        [TestMethod]
        public void CanonicalP256PublicKeyRejectsInvalidPointsAndNonCanonicalShapes()
        {
            byte[] point = ECCurve.Secp256r1.G.EncodePoint(false).ToArray();
            byte[] badY = point.ToArray(); badY[64] ^= 1;
            byte[] hybrid = point.ToArray(); hybrid[0] = 6;
            byte[] outsideField = new byte[] { 2 }.Concat(Enumerable.Repeat((byte)255, 32)).ToArray();
            byte[] noSquareRoot = new byte[33]; noSquareRoot[0] = 2; noSquareRoot[32] = 1;
            var abi = NativeContract.AccountManagement.GetContractState(Settings, 1).Manifest.Abi.GetMethod("canonicalP256PublicKey", 1);
            Assert.IsNotNull(abi, "Negative cases must execute the new method, not fault because it is absent.");
            foreach (byte[] raw in new List<byte[]> { System.Array.Empty<byte>(), new byte[] { 0 }, new byte[32], new byte[34], new byte[64], new byte[66],
                new byte[33], new byte[65], badY, hybrid, outsideField, noSquareRoot })
            {
                using var engine = Invoke(Snapshot(), "canonicalP256PublicKey", [raw], signers: []);
                Assert.AreEqual(VMState.FAULT, engine.State, Convert.ToHexString(raw));
                Assert.AreEqual(0, engine.Notifications.Count);
            }
        }

        [TestMethod]
        public void CanonicalP256PublicKeyRejectsCoercibleVmTypes()
        {
            foreach (StackItem raw in new StackItem[] { Neo.VM.Types.Boolean.True, new Integer(1),
                new Neo.VM.Types.Buffer(ECCurve.Secp256r1.G.EncodePoint(true)), new Neo.VM.Types.Array(), StackItem.Null })
            {
                using var engine = InvokeItems(Snapshot(), "canonicalP256PublicKey", new Neo.VM.Types.Array([raw]), signers: []);
                Rejected(engine, raw.IsNull ? "can't be null" : "exact ByteString");
            }
        }

        [TestMethod]
        public void CanonicalP256PublicKeyCannotRunBeforeActivation()
        {
            var inactive = Settings with { Hardforks = Settings.Hardforks.SetItem(Hardfork.HF_SmartAccountV1, uint.MaxValue) };
            using var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.AccountManagement.Hash, "canonicalP256PublicKey", ECCurve.Secp256r1.G.EncodePoint(true));
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, Snapshot(), Block(1000), inactive, gas: 100_000_000);
            engine.LoadScript(script.ToArray());
            Assert.AreEqual(VMState.FAULT, engine.Execute());
            Assert.AreEqual(0, engine.Notifications.Count);
        }
    }
}
