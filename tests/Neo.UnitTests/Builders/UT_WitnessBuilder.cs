// Copyright (C) 2015-2025 The Neo Project.
//
// UT_WitnessBuilder.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Builders;

namespace Neo.UnitTests.Builders
{
    [TestClass]
    public class UT_WitnessBuilder
    {
        [TestMethod]
        public void TestCreateEmpty()
        {
            var wb = WitnessBuilder.CreateEmpty();
            Assert.IsNotNull(wb);
        }

        [TestMethod]
        public void TestAddInvocationWithScriptBuilder()
        {
            var witness = WitnessBuilder.CreateEmpty()
                .AddInvocation(sb =>
                {
                    sb.Emit(VM.OpCode.NOP);
                    sb.Emit(VM.OpCode.NOP);
                    sb.Emit(VM.OpCode.NOP);
                })
                .Build();

            Assert.IsNotNull(witness);
            Assert.AreEqual(3, witness.InvocationScript.Length);
            CollectionAssert.AreEqual(new byte[] { 0x21, 0x21, 0x21 }, witness.InvocationScript.ToArray());
        }

        [TestMethod]
        public void TestAddInvocation()
        {
            var witness = WitnessBuilder.CreateEmpty()
                .AddInvocation(new byte[] { 0x01, 0x02, 0x03 })
                .Build();

            Assert.IsNotNull(witness);
            Assert.AreEqual(3, witness.InvocationScript.Length);
            CollectionAssert.AreEqual(new byte[] { 0x01, 0x02, 0x03 }, witness.InvocationScript.ToArray());
        }

        [TestMethod]
        public void TestAddVerificationWithScriptBuilder()
        {
            var witness = WitnessBuilder.CreateEmpty()
                .AddVerification(sb =>
                {
                    sb.Emit(VM.OpCode.NOP);
                    sb.Emit(VM.OpCode.NOP);
                    sb.Emit(VM.OpCode.NOP);
                })
                .Build();

            Assert.IsNotNull(witness);
            Assert.AreEqual(3, witness.VerificationScript.Length);
            CollectionAssert.AreEqual(new byte[] { 0x21, 0x21, 0x21 }, witness.VerificationScript.ToArray());
        }

        [TestMethod]
        public void TestAddVerification()
        {
            var witness = WitnessBuilder.CreateEmpty()
                .AddVerification(new byte[] { 0x01, 0x02, 0x03 })
                .Build();

            Assert.IsNotNull(witness);
            Assert.AreEqual(3, witness.VerificationScript.Length);
            CollectionAssert.AreEqual(new byte[] { 0x01, 0x02, 0x03 }, witness.VerificationScript.ToArray());
        }
    }
}
