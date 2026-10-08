// Copyright (C) 2015-2026 The Neo Project.
//
// UT_SmartAccountEnvelope.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Array = Neo.VM.Types.Array;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_SmartAccountEnvelope
    {
        private static readonly UInt160 Account = UInt160.Parse("0x3e25330008563c55fe2853e07868b36ca00020ac");
        private static readonly UInt160 Service = UInt160.Parse("0xd9421d07adf206e9dc4be746a02e8e087fa61741");
        private static Array Operation(BigInteger nonce = default) => new([
            Service.ToArray(), "ping", new Array(), nonce, BigInteger.Zero, ByteString.Empty
        ]);

        private static byte[] Tail(UInt160 account, string method = "executeUserOp", CallFlags flags = CallFlags.All)
        {
            using ScriptBuilder builder = new();
            builder.EmitPush(account).EmitPush(2).Emit(OpCode.PACK);
            builder.EmitPush(flags).EmitPush(method).EmitPush(Service);
            builder.EmitSysCall(ApplicationEngine.System_Contract_Call);
            return builder.ToArray();
        }

        private static byte[] WithPrefix(byte[] prefix) => [.. prefix, .. Tail(Account)];
        private static byte[] Prefix() => Convert.FromHexString("0c001010c20c0470696e670c144117a67f088e2ea046e74bdce906f2ad071d42d916c0");

        private static byte[] Serialized(StackItem item) => BinarySerializer.Serialize(item, Transaction.MaxTransactionSize, Transaction.MaxTransactionSize);

        [TestMethod]
        public void SingleEnvelopeMatchesIndependentGoldenBytes()
        {
            byte[] expected = WithPrefix(Prefix());
            Assert.AreSequenceEqual(expected, SmartAccountEnvelope.CreateApplicationScript(Account, Operation(), false));
            var envelope = SmartAccountEnvelope.Parse(Account, expected);
            Assert.AreEqual(Account, envelope.AccountId);
            Assert.IsFalse(envelope.IsBatch);
            Assert.AreEqual(1, envelope.Count);
            Assert.AreSequenceEqual(Serialized(Operation()), Serialized(envelope.GetOperation(0)));
        }

        [TestMethod]
        public void IndependentLanguageFixturesMatchBuilderParserAndVm()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "SmartContract", "Native", "TestFile", "smartaccount-envelope-v1.json");
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
            foreach (JsonElement vector in document.RootElement.GetProperty("vectors").EnumerateArray())
            {
                byte[] serialized = Convert.FromHexString(vector.GetProperty("serializedPayload").GetString());
                Array payload = (Array)BinarySerializer.Deserialize(serialized, ExecutionEngineLimits.Default);
                bool batch = vector.GetProperty("isBatch").GetBoolean();
                byte[] script = Convert.FromHexString(vector.GetProperty("applicationScript").GetString());
                Assert.AreSequenceEqual(script, SmartAccountEnvelope.CreateApplicationScript(Account, payload, batch));
                AssertInitializerValue(script, payload, batch);
            }
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(32)]
        public void BatchPreservesOrderAndNonceShadow(int count)
        {
            Array batch = new(Enumerable.Range(0, count).Select(n => Operation(n)));
            byte[] script = SmartAccountEnvelope.CreateApplicationScript(Account, batch, true);
            var envelope = SmartAccountEnvelope.Parse(Account, script);
            Assert.IsTrue(envelope.IsBatch);
            Assert.AreEqual(count, envelope.Count);
            int reads = 0;
            envelope.ValidateNonces(channel => { Assert.AreEqual(BigInteger.Zero, channel); reads++; return 0; });
            Assert.AreEqual(1, reads);
            for (int i = 0; i < count; i++)
                Assert.AreEqual(new BigInteger(i), envelope.GetOperation(i)[3].GetInteger());
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(33)]
        public void BatchCountOutsideProfileIsRejected(int count)
        {
            Array batch = new(Enumerable.Range(0, count).Select(n => Operation(n)));
            Assert.ThrowsExactly<FormatException>(() => SmartAccountEnvelope.CreateApplicationScript(Account, batch, true));
        }

        [TestMethod]
        public void IndividuallyValidOperationsCanStillExceedTransactionSize()
        {
            Array op = Operation();
            op[2] = new Array([new byte[4090]]);
            op[5] = new byte[1024];
            Array batch = new(Enumerable.Repeat(op, 32));
            Assert.ThrowsExactly<FormatException>(() => SmartAccountEnvelope.CreateApplicationScript(Account, batch, true));
        }

        [TestMethod]
        public void InvalidPayloadTypesAndNullHostArgumentsAreRejected()
        {
            Assert.ThrowsExactly<FormatException>(() => SmartAccountEnvelope.CreateApplicationScript(Account, new Struct(Operation()), false));
            Assert.ThrowsExactly<FormatException>(() => SmartAccountEnvelope.CreateApplicationScript(Account, new Array([StackItem.Null]), true));
            Assert.ThrowsExactly<ArgumentNullException>(() => SmartAccountEnvelope.CreateApplicationScript(Account, null, false));
            Assert.ThrowsExactly<ArgumentNullException>(() => SmartAccountEnvelope.Parse(null, WithPrefix(Prefix())));
            var envelope = SmartAccountEnvelope.Parse(Account, WithPrefix(Prefix()));
            Assert.ThrowsExactly<ArgumentNullException>(() => envelope.ValidateNonces(null));
        }

        [TestMethod]
        public void ReturnedOperationsAndSourceBytesCannotRetargetEnvelope()
        {
            Array op = Operation();
            byte[] signature = [1, 2, 3];
            op[5] = signature;
            byte[] script = SmartAccountEnvelope.CreateApplicationScript(Account, op, false);
            var envelope = SmartAccountEnvelope.Parse(Account, script);
            byte[] initial = Serialized(envelope.GetOperation(0));
            System.Array.Fill(script, (byte)0);
            signature[0] ^= 1;
            op[3] = 999;
            var returned = envelope.GetOperation(0);
            returned[1] = "changed";
            ((Array)returned[2]).Add("changed");
            Assert.AreSequenceEqual(initial, Serialized(envelope.GetOperation(0)));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => envelope.GetOperation(-1));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => envelope.GetOperation(1));
        }

        [TestMethod]
        public void AccountIdentityIsAnOwnedSnapshot()
        {
            UInt160 input = new(Account.ToArray());
            var envelope = SmartAccountEnvelope.Parse(input, WithPrefix(Prefix()));
            MemoryReader reader = new(Service.ToArray());
            input.Deserialize(ref reader);
            Assert.AreEqual(Account, envelope.AccountId);
            UInt160 returned = envelope.AccountId;
            reader = new(Service.ToArray());
            returned.Deserialize(ref reader);
            Assert.AreEqual(Account, envelope.AccountId);
        }

        [TestMethod]
        [DataRow("wrong-account")]
        [DataRow("zero-account")]
        [DataRow("wrong-flags")]
        [DataRow("wrong-method")]
        [DataRow("wrong-service")]
        [DataRow("wrong-syscall")]
        [DataRow("wrong-count")]
        [DataRow("batch-confusion")]
        [DataRow("prefix-nop")]
        [DataRow("suffix-ret")]
        [DataRow("two-calls")]
        [DataRow("unused-value")]
        public void EnvelopeSubstitutionAndExtraInstructionsAreRejected(string defect)
        {
            byte[] script = WithPrefix(Prefix());
            UInt160 expectedAccount = Account;
            switch (defect)
            {
                case "wrong-account": expectedAccount = Service; break;
                case "zero-account": expectedAccount = UInt160.Zero; break;
                case "wrong-flags": script = [.. Prefix(), .. Tail(Account, flags: CallFlags.ReadOnly)]; break;
                case "wrong-method": script = [.. Prefix(), .. Tail(Account, "registerAccount")]; break;
                case "wrong-service": script[^6] ^= 1; break;
                case "wrong-syscall": script[^1] ^= 1; break;
                case "wrong-count": script[Prefix().Length + 22] = (byte)OpCode.PUSH3; break;
                case "batch-confusion": script = [.. Prefix(), .. Tail(Account, "executeUserOps")]; break;
                case "prefix-nop": script = [(byte)OpCode.NOP, .. script]; break;
                case "suffix-ret": script = [.. script, (byte)OpCode.RET]; break;
                case "two-calls": script = [.. script, .. script]; break;
                case "unused-value": script = [(byte)OpCode.PUSHNULL, .. script]; break;
                default: Assert.Fail(defect); break;
            }
            Assert.ThrowsExactly<FormatException>(() => SmartAccountEnvelope.Parse(expectedAccount, script));
        }

        [TestMethod]
        [DataRow("wide-zero")]
        [DataRow("wide-data")]
        [DataRow("empty-pack")]
        [DataRow("convert")]
        [DataRow("dup")]
        [DataRow("call")]
        [DataRow("syscall")]
        public void EquivalentAndExecutableInitializersAreRejected(string defect)
        {
            byte[] prefix = Prefix();
            prefix = defect switch
            {
                "wide-zero" => [.. prefix[..2], (byte)OpCode.PUSHINT8, 0, .. prefix[3..]],
                "wide-data" => [(byte)OpCode.PUSHDATA2, 0, 0, .. prefix[2..]],
                "empty-pack" => [.. prefix[..4], (byte)OpCode.PUSH0, (byte)OpCode.PACK, .. prefix[5..]],
                "convert" => [.. prefix, (byte)OpCode.CONVERT, (byte)StackItemType.Array],
                "dup" => [.. prefix, (byte)OpCode.DUP, (byte)OpCode.DROP],
                "call" => [.. prefix, (byte)OpCode.CALL, 0],
                "syscall" => [.. prefix, (byte)OpCode.SYSCALL, 0, 0, 0, 0],
                _ => throw new ArgumentException(defect)
            };
            Assert.ThrowsExactly<FormatException>(() => SmartAccountEnvelope.Parse(Account, WithPrefix(prefix)));
        }

        [TestMethod]
        public void EveryTruncationOfValidEnvelopeIsRejected()
        {
            byte[] script = WithPrefix(Prefix());
            for (int i = 0; i < script.Length; i++)
            {
                byte[] truncated = script[..i];
                Assert.ThrowsExactly<FormatException>(() => SmartAccountEnvelope.Parse(Account, truncated), $"Cut at {i}");
            }
        }

        [TestMethod]
        [DataRow("truncated-data1")]
        [DataRow("truncated-data2")]
        [DataRow("oversized-data4")]
        [DataRow("truncated-integer")]
        [DataRow("missing-pack-count")]
        [DataRow("boolean-pack-count")]
        [DataRow("negative-pack-count")]
        [DataRow("excessive-pack-count")]
        [DataRow("incomplete-pack")]
        [DataRow("empty-prefix")]
        [DataRow("non-array-root")]
        [DataRow("oversized-data2")]
        [DataRow("too-deep")]
        [DataRow("too-large")]
        public void MalformedInitializerFailsWithFormatException(string defect)
        {
            byte[] prefix = defect switch
            {
                "truncated-data1" => [(byte)OpCode.PUSHDATA1],
                "truncated-data2" => [(byte)OpCode.PUSHDATA2, 0],
                "oversized-data4" => [(byte)OpCode.PUSHDATA4, 255, 255, 255, 255],
                "truncated-integer" => [(byte)OpCode.PUSHINT256, 1],
                "missing-pack-count" => [(byte)OpCode.PACK],
                "boolean-pack-count" => [(byte)OpCode.PUSHT, (byte)OpCode.PACK],
                "negative-pack-count" => [(byte)OpCode.PUSHM1, (byte)OpCode.PACK],
                "excessive-pack-count" => [(byte)OpCode.PUSHINT32, 255, 255, 255, 127, (byte)OpCode.PACK],
                "incomplete-pack" => [(byte)OpCode.PUSH1, (byte)OpCode.PACK],
                "empty-prefix" => [],
                "non-array-root" => [(byte)OpCode.PUSH1],
                "oversized-data2" => [(byte)OpCode.PUSHDATA2, 1, 16],
                "too-deep" => [(byte)OpCode.PUSHNULL, .. Enumerable.Repeat(new byte[] { (byte)OpCode.PUSH1, (byte)OpCode.PACK }, 20000).SelectMany(x => x)],
                "too-large" => new byte[Transaction.MaxTransactionSize + 1],
                _ => throw new ArgumentException(defect)
            };
            Assert.ThrowsExactly<FormatException>(() => SmartAccountEnvelope.Parse(Account, WithPrefix(prefix)));
        }

        [TestMethod]
        public void CanonicalInitializerMatchesActualVmForAllSupportedTypes()
        {
            Array op = Operation();
            op[2] = new Array([
                StackItem.Null, true, false, -1, 0, 16, 17, 127, 128, -128, -129,
                short.MaxValue, ushort.MaxValue, int.MaxValue, uint.MaxValue,
                new Integer(long.MinValue), new Integer(long.MaxValue),
                new Integer(BigInteger.One << 100), new Integer((BigInteger.One << 255) - 1), new Integer(-(BigInteger.One << 255)),
                ByteString.Empty, new byte[255], new byte[256], new Array(), new Struct(),
                new Struct([true, new Array([1, "nested"])])
            ]);
            byte[] script = SmartAccountEnvelope.CreateApplicationScript(Account, op, false);
            AssertInitializerValue(script, op, false);
        }

        private static void AssertInitializerValue(byte[] script, StackItem expected, bool batch)
        {
            byte[] suffix = Tail(Account, batch ? "executeUserOps" : "executeUserOp");
            using var engine = ApplicationEngine.Run(script[..^suffix.Length], TestBlockchain.GetTestSnapshotCache(), settings: TestProtocolSettings.Default);
            Assert.AreEqual(VMState.HALT, engine.State, engine.FaultException?.ToString());
            Assert.AreEqual(1, engine.ResultStack.Count);
            Assert.AreSequenceEqual(Serialized(expected), Serialized(engine.ResultStack.Pop()));
            var parsed = SmartAccountEnvelope.Parse(Account, script);
            StackItem reconstructed = batch ? new Array(Enumerable.Range(0, parsed.Count).Select(parsed.GetOperation)) : parsed.GetOperation(0);
            Assert.AreSequenceEqual(Serialized(expected), Serialized(reconstructed));
        }

        [TestMethod]
        public void DeterministicGeneratedInitializersMatchVmAndRoundTrip()
        {
            Random random = new(742);
            for (int i = 0; i < 96; i++)
            {
                Array op = Operation(i);
                op[2] = new Array([new Integer(random.NextInt64()), random.Next(2) == 1,
                    new byte[random.Next(0, 300)], new Struct([StackItem.Null, random.Next(-20000, 20000)])]);
                op[5] = new byte[random.Next(0, 65)];
                AssertInitializerValue(SmartAccountEnvelope.CreateApplicationScript(Account, op, false), op, false);
            }
            Array batch = new([Operation(0), Operation(1)]);
            AssertInitializerValue(SmartAccountEnvelope.CreateApplicationScript(Account, batch, true), batch, true);
        }

        [TestMethod]
        public void ArbitraryMutationsEitherRejectOrRemainCanonicalVmInitializers()
        {
            byte[] baseline = WithPrefix(Prefix());
            Random random = new(925);
            for (int i = 0; i < 512; i++)
            {
                byte[] mutated = baseline.ToArray();
                mutated[random.Next(mutated.Length)] ^= (byte)(1 << random.Next(8));
                SmartAccountEnvelope parsed;
                try { parsed = SmartAccountEnvelope.Parse(Account, mutated); }
                catch (FormatException) { continue; }
                Assert.IsFalse(parsed.IsBatch);
                Assert.AreSequenceEqual(mutated, SmartAccountEnvelope.CreateApplicationScript(Account, parsed.GetOperation(0), false));
                AssertInitializerValue(mutated, parsed.GetOperation(0), false);
            }
        }

        [TestMethod]
        public void NonceShadowTracksChannelsWithoutMutatingStoredCursors()
        {
            var cursors = new Dictionary<BigInteger, BigInteger> { [0] = 3, [7] = 9 };
            Array batch = new([Operation(3), Operation((new BigInteger(7) << 64) + 9), Operation(4)]);
            var envelope = SmartAccountEnvelope.Parse(Account, SmartAccountEnvelope.CreateApplicationScript(Account, batch, true));
            int reads = 0;
            envelope.ValidateNonces(ch => { reads++; return cursors[ch]; });
            Assert.AreEqual(2, reads);
            Assert.AreEqual(new BigInteger(3), cursors[0]);
            Assert.AreEqual(new BigInteger(9), cursors[7]);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(2)]
        public void DuplicateAndGapWithinBatchAreRejected(int second)
        {
            Array batch = new([Operation(0), Operation(second)]);
            var envelope = SmartAccountEnvelope.Parse(Account, SmartAccountEnvelope.CreateApplicationScript(Account, batch, true));
            Assert.ThrowsExactly<InvalidOperationException>(() => envelope.ValidateNonces(_ => 0));
        }

        [TestMethod]
        public void BatchCannotCrossExhaustionOrWrapToZero()
        {
            Array batch = new([Operation(ulong.MaxValue), Operation(0)]);
            var envelope = SmartAccountEnvelope.Parse(Account, SmartAccountEnvelope.CreateApplicationScript(Account, batch, true));
            Assert.ThrowsExactly<InvalidOperationException>(() => envelope.ValidateNonces(_ => ulong.MaxValue));
            var last = SmartAccountEnvelope.Parse(Account, SmartAccountEnvelope.CreateApplicationScript(Account, Operation(ulong.MaxValue), false));
            last.ValidateNonces(_ => ulong.MaxValue);
            Assert.ThrowsExactly<InvalidOperationException>(() => last.ValidateNonces(_ => BigInteger.One << 64));
            Assert.ThrowsExactly<FormatException>(() => last.ValidateNonces(_ => -1));
        }
    }
}
