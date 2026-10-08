// Copyright (C) 2015-2026 The Neo Project.
//
// UT_SmartAccountProtocol.cs file belongs to the neo project and is free
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
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Linq;
using System.IO;
using System.Text.Json;
using System.Numerics;
using System.Text;
using Array = Neo.VM.Types.Array;
using Buffer = Neo.VM.Types.Buffer;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_SmartAccountProtocol
    {
        private const uint Network = 0x12345678;
        private static readonly UInt160 Service = UInt160.Parse("0xd9421d07adf206e9dc4be746a02e8e087fa61741");
        private static readonly UInt160 Account = UInt160.Parse("0x3e25330008563c55fe2853e07868b36ca00020ac");
        private static readonly UInt160 Custody = new(Convert.FromHexString("202122232425262728292a2b2c2d2e2f30313233"));
        private static readonly byte[] Salt = Convert.FromHexString("404142434445464748494a4b4c4d4e4f505152535455565758595a5b5c5d5e5f");
        private const string UnsignedVector = "400628144117a67f088e2ea046e74bdce906f2ad071d42d928087472616e7366657240022814ac2000a06cb36878e05328fe553c56080033253e21012a210021060068e5cf8b012800";

        private static Array Operation() => new([
            Service.ToArray(), "transfer", new Array([Account.ToArray(), 42]),
            BigInteger.Zero, new BigInteger(1700000000000), ByteString.Empty
        ]);

        [TestMethod]
        public void NativeIdentityAndPublishedAccountVectorMatch()
        {
            Assert.AreEqual(Service, SmartAccountProtocol.ServiceHash);
            Assert.AreEqual(Account, SmartAccountProtocol.GetAccountId(Network, Custody, Salt));
            Assert.AreEqual(UInt160.Parse("0x7829f40af6380c00110932c9551ece0916fdcad1"), SmartAccountProtocol.GetAccountAddress(Account));
            Assert.AreEqual("0c14ac2000a06cb36878e05328fe553c56080033253e11c0150c067665726966790c144117a67f088e2ea046e74bdce906f2ad071d42d941627d5b52",
                Convert.ToHexStringLower(SmartAccountProtocol.CreateVerificationScript(Account)));
        }

        [TestMethod]
        public void NativeServiceIdentityCannotBeRetargetedThroughReturnedHash()
        {
            UInt160 exposed = SmartAccountProtocol.ServiceHash;
            MemoryReader reader = new(Account.ToArray());
            exposed.Deserialize(ref reader);
            Assert.AreEqual(Service, SmartAccountProtocol.ServiceHash);
            Assert.AreEqual(Account, SmartAccountProtocol.GetAccountId(Network, Custody, Salt));
        }

        [TestMethod]
        public void IdentityCommitsToNetworkCustodyAndSalt()
        {
            Assert.AreNotEqual(Account, SmartAccountProtocol.GetAccountId(Network + 1, Custody, Salt));
            Assert.AreNotEqual(Account, SmartAccountProtocol.GetAccountId(Network, Service, Salt));
            var changed = Salt.ToArray();
            changed[^1] ^= 1;
            Assert.AreNotEqual(Account, SmartAccountProtocol.GetAccountId(Network, Custody, changed));
            Assert.ThrowsExactly<FormatException>(() => SmartAccountProtocol.GetAccountId(Network, UInt160.Zero, Salt));
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(31)]
        [DataRow(33)]
        public void InvalidSaltLengthIsRejected(int length) =>
            Assert.ThrowsExactly<FormatException>(() => SmartAccountProtocol.GetAccountId(Network, Custody, new byte[length]));

        [TestMethod]
        public void ZeroAccountIdentityIsRejected()
        {
            Assert.ThrowsExactly<FormatException>(() => SmartAccountProtocol.CreateVerificationScript(UInt160.Zero));
            Assert.ThrowsExactly<FormatException>(() => SmartAccountProtocol.GetAuthorizationDomain(Network, UInt160.Zero, 0, 0));
            Assert.ThrowsExactly<FormatException>(() => SmartAccountProtocol.GetAccountKey(UInt160.Zero));
            Assert.ThrowsExactly<FormatException>(() => SmartAccountProtocol.GetNonceKey(UInt160.Zero, 0));
        }

        [TestMethod]
        public void PublishedOperationBytesAndDigestMatch()
        {
            Assert.AreEqual(UnsignedVector, Convert.ToHexStringLower(SmartAccountProtocol.SerializeUnsignedOperation(Operation())));
            Assert.AreEqual("be6546454525ed1382f8d86380c5816fd24cf4f5674e00bc0e601af3a6952ea8",
                Convert.ToHexStringLower(SmartAccountProtocol.GetOperationDigest(Network, Account, Operation(), 0, 0)));
            Assert.AreEqual("4e656f536d6172744163636f756e742f557365724f7065726174696f6e02785634124117a67f088e2ea046e74bdce906f2ad071d42d9ac2000a06cb36878e05328fe553c56080033253e00000000000000000000000000000000",
                Convert.ToHexStringLower(SmartAccountProtocol.GetAuthorizationDomain(Network, Account, 0, 0)));
        }

        [TestMethod]
        public void IndependentAbi2AuthorityAndConfigurationVectorsMatch()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "SmartContract", "Native", "TestFile", "smartaccount-authorization-v2.json");
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            foreach (var vector in document.RootElement.GetProperty("vectors").EnumerateArray())
            {
                ulong epoch = ulong.Parse(vector.GetProperty("authorityEpoch").GetString());
                ulong nonce = ulong.Parse(vector.GetProperty("configurationNonce").GetString());
                Assert.AreEqual(vector.GetProperty("domain").GetString(), Convert.ToHexStringLower(SmartAccountProtocol.GetAuthorizationDomain(Network, Account, epoch, nonce)));
                Assert.AreEqual(vector.GetProperty("digest").GetString(), Convert.ToHexStringLower(SmartAccountProtocol.GetOperationDigest(Network, Account, Operation(), epoch, nonce)));
            }
            var baseline = SmartAccountProtocol.GetOperationDigest(Network, Account, Operation(), 0, 1);
            Assert.IsFalse(baseline.SequenceEqual(SmartAccountProtocol.GetOperationDigest(Network, Account, Operation(), 1, 1)));
            Assert.IsFalse(baseline.SequenceEqual(SmartAccountProtocol.GetOperationDigest(Network, Account, Operation(), 0, 2)));
        }

        [TestMethod]
        public void SignatureIsExcludedWithoutMutatingInputOrReturningAliases()
        {
            Array op = Operation();
            byte[] signature = [1, 2, 3];
            op[5] = signature;
            byte[] serialized = SmartAccountProtocol.SerializeUnsignedOperation(op);
            Assert.AreEqual(UnsignedVector, Convert.ToHexStringLower(serialized));
            Assert.AreSequenceEqual(signature, op[5].GetSpan().ToArray());
            serialized[0] ^= 1;
            Assert.AreEqual(UnsignedVector, Convert.ToHexStringLower(SmartAccountProtocol.SerializeUnsignedOperation(op)));
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        [DataRow(4)]
        public void EverySignedFieldIsCommitted(int field)
        {
            Array op = Operation();
            byte[] before = SmartAccountProtocol.GetOperationDigest(Network, Account, op, 0, 0);
            op[field] = field switch
            {
                0 => Account.ToArray(),
                1 => "balanceOf",
                2 => new Array([Account.ToArray(), 43]),
                3 => new Integer(1),
                _ => new Integer(1700000000001)
            };
            Assert.AreNotEqual(Convert.ToHexString(before), Convert.ToHexString(SmartAccountProtocol.GetOperationDigest(Network, Account, op, 0, 0)));
        }

        [TestMethod]
        public void AuthorizationDomainSeparatesNetworksAndAccounts()
        {
            byte[] initial = SmartAccountProtocol.GetOperationDigest(Network, Account, Operation(), 0, 0);
            Assert.AreNotEqual(Convert.ToHexString(initial), Convert.ToHexString(SmartAccountProtocol.GetOperationDigest(Network + 1, Account, Operation(), 0, 0)));
            Assert.AreNotEqual(Convert.ToHexString(initial), Convert.ToHexString(SmartAccountProtocol.GetOperationDigest(Network, Custody, Operation(), 0, 0)));
        }

        [TestMethod]
        [DataRow("null")]
        [DataRow("struct")]
        [DataRow("short")]
        [DataRow("long")]
        [DataRow("target-zero")]
        [DataRow("target-short")]
        [DataRow("target-buffer")]
        [DataRow("method-empty")]
        [DataRow("method-long")]
        [DataRow("method-utf8")]
        [DataRow("method-buffer")]
        [DataRow("args-struct")]
        [DataRow("args-count")]
        [DataRow("nonce-negative")]
        [DataRow("nonce-bytes")]
        [DataRow("nonce-bool")]
        [DataRow("deadline-negative")]
        [DataRow("deadline-bytes")]
        [DataRow("signature-long")]
        [DataRow("signature-buffer")]
        [DataRow("signature-null")]
        public void MalformedOperationIsRejectedBeforeDigest(string defect)
        {
            Array op = Operation();
            StackItem value = op;
            switch (defect)
            {
                case "null": value = StackItem.Null; break;
                case "struct": value = new Struct(op); break;
                case "short": value = new Array(op.Take(5)); break;
                case "long": op.Add(0); break;
                case "target-zero": op[0] = new byte[20]; break;
                case "target-short": op[0] = new byte[19]; break;
                case "target-buffer": op[0] = new Buffer(Service.ToArray()); break;
                case "method-empty": op[1] = ByteString.Empty; break;
                case "method-long": op[1] = new byte[129]; break;
                case "method-utf8": op[1] = new byte[] { 0xc0, 0xaf }; break;
                case "method-buffer": op[1] = new Buffer(new byte[] { 0x61 }); break;
                case "args-struct": op[2] = new Struct(); break;
                case "args-count": op[2] = new Array(Enumerable.Repeat(StackItem.Null, 65)); break;
                case "nonce-negative": op[3] = -1; break;
                case "nonce-bytes": op[3] = new byte[] { 1 }; break;
                case "nonce-bool": op[3] = true; break;
                case "deadline-negative": op[4] = -1; break;
                case "deadline-bytes": op[4] = new byte[] { 1 }; break;
                case "signature-long": op[5] = new byte[1025]; break;
                case "signature-buffer": op[5] = new Buffer(1); break;
                case "signature-null": op[5] = StackItem.Null; break;
                default: Assert.Fail(defect); break;
            }
            Assert.ThrowsExactly<FormatException>(() => SmartAccountProtocol.SerializeUnsignedOperation(value));
        }

        [TestMethod]
        public void ExactScalarBoundsAndMultibyteUtf8AreAccepted()
        {
            Array op = Operation();
            op[1] = Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("é", 64)));
            op[2] = new Array(Enumerable.Repeat(StackItem.Null, 64));
            op[3] = (BigInteger.One << 255) - 1;
            op[4] = (BigInteger.One << 255) - 1;
            op[5] = new byte[1024];
            Array decoded = (Array)BinarySerializer.Deserialize(SmartAccountProtocol.SerializeUnsignedOperation(op), ExecutionEngineLimits.Default);
            Assert.AreEqual(op[3], decoded[3]);
            Assert.AreEqual(op[4], decoded[4]);
            Assert.AreEqual(ByteString.Empty, decoded[5]);
        }

        [TestMethod]
        [DataRow(8, true)]
        [DataRow(9, false)]
        public void NestedArrayAndStructDepthIsBounded(int depth, bool valid)
        {
            StackItem item = StackItem.Null;
            for (int i = 0; i < depth; i++)
                item = i % 2 == 0 ? new Array([item]) : new Struct([item]);
            Array op = Operation();
            op[2] = new Array([item]);
            if (valid)
                Assert.IsNotEmpty(SmartAccountProtocol.SerializeUnsignedOperation(op));
            else
                Assert.ThrowsExactly<FormatException>(() => SmartAccountProtocol.SerializeUnsignedOperation(op));
        }

        [TestMethod]
        [DataRow(4090, true)]
        [DataRow(4091, false)]
        [DataRow(1048576, false)]
        public void SerializedArgumentSizeIncludesArrayAndVarBytesHeaders(int length, bool valid)
        {
            Array op = Operation();
            op[2] = new Array([new ByteString(new byte[length])]);
            if (valid)
                Assert.IsNotEmpty(SmartAccountProtocol.SerializeUnsignedOperation(op));
            else
                Assert.ThrowsExactly<FormatException>(() => SmartAccountProtocol.SerializeUnsignedOperation(op));
        }

        [TestMethod]
        [DataRow(4090, true)]
        [DataRow(4091, false)]
        public void NestedFanOutIsBoundedBeforeTraversal(int count, bool valid)
        {
            Array op = Operation();
            op[2] = new Array([new Array(Enumerable.Repeat(StackItem.Null, count))]);
            if (valid)
                Assert.IsNotEmpty(SmartAccountProtocol.SerializeUnsignedOperation(op));
            else
                Assert.ThrowsExactly<FormatException>(() => SmartAccountProtocol.SerializeUnsignedOperation(op));
        }

        [TestMethod]
        [DataRow("buffer")]
        [DataRow("map")]
        [DataRow("interop")]
        [DataRow("pointer")]
        [DataRow("cycle")]
        [DataRow("alias")]
        public void UnsupportedOrSharedArgumentValuesAreRejected(string kind)
        {
            Array child = new([1]);
            Array args = new([child]);
            switch (kind)
            {
                case "buffer": args[0] = new Buffer(1); break;
                case "map": args[0] = new Map(); break;
                case "interop": args[0] = new InteropInterface(new object()); break;
                case "pointer": args[0] = new Pointer(new Script(new byte[] { (byte)OpCode.RET }), 0); break;
                case "cycle": child.Add(args); break;
                case "alias": args.Add(child); break;
                default: Assert.Fail(kind); break;
            }
            Array op = Operation();
            op[2] = args;
            Assert.ThrowsExactly<FormatException>(() => SmartAccountProtocol.SerializeUnsignedOperation(op));
        }

        [TestMethod]
        public void IndependentEqualArraysAndAllLeafTypesRetainCanonicalTypes()
        {
            Array op = Operation();
            op[2] = new Array([new Array([1]), new Array([1]), new Struct([StackItem.Null, true, -1, ByteString.Empty])]);
            Array decoded = (Array)BinarySerializer.Deserialize(SmartAccountProtocol.SerializeUnsignedOperation(op), ExecutionEngineLimits.Default);
            Assert.IsInstanceOfType<Struct>(((Array)decoded[2])[2]);
            Assert.AreSequenceEqual(BinarySerializer.Serialize(op[2], ExecutionEngineLimits.Default), BinarySerializer.Serialize(decoded[2], ExecutionEngineLimits.Default));
        }

        [TestMethod]
        public void StorageKeysAreFixedWidthAndDomainSeparated()
        {
            Assert.AreEqual("10ac2000a06cb36878e05328fe553c56080033253e", Convert.ToHexStringLower(SmartAccountProtocol.GetAccountKey(Account)));
            byte[] key = SmartAccountProtocol.GetNonceKey(Account, 1);
            Assert.AreEqual(45, key.Length);
            Assert.AreEqual(0x20, key[0]);
            Assert.AreSequenceEqual(Account.ToArray(), key[1..21]);
            Assert.IsTrue(key[21..44].All(b => b == 0));
            Assert.AreEqual(1, key[44]);
            byte[] maximum = SmartAccountProtocol.GetNonceKey(Account, (BigInteger.One << 191) - 1);
            Assert.AreEqual(0x7f, maximum[21]);
            Assert.IsTrue(maximum[22..].All(b => b == 0xff));
            Assert.ThrowsExactly<FormatException>(() => SmartAccountProtocol.GetNonceKey(Account, -1));
            Assert.ThrowsExactly<FormatException>(() => SmartAccountProtocol.GetNonceKey(Account, BigInteger.One << 191));
        }

        [TestMethod]
        public void NonceDecompositionAndExhaustionDoNotWrap()
        {
            BigInteger maximum = (BigInteger.One << 255) - 1;
            var (channel, sequence) = SmartAccountProtocol.GetNonceParts(maximum);
            Assert.AreEqual((BigInteger.One << 191) - 1, channel);
            Assert.AreEqual(ulong.MaxValue, sequence);
            Assert.AreEqual(BigInteger.One << 64, SmartAccountProtocol.ConsumeNonce(maximum, ulong.MaxValue));
            Assert.AreEqual(BigInteger.One, SmartAccountProtocol.ConsumeNonce(BigInteger.One << 64, 0));
            Assert.ThrowsExactly<InvalidOperationException>(() => SmartAccountProtocol.ConsumeNonce(maximum, BigInteger.One << 64));
            Assert.ThrowsExactly<InvalidOperationException>(() => SmartAccountProtocol.ConsumeNonce(1, 0));
            Assert.ThrowsExactly<FormatException>(() => SmartAccountProtocol.GetNonceParts(-1));
            Assert.ThrowsExactly<FormatException>(() => SmartAccountProtocol.GetNonceParts(BigInteger.One << 255));
            Assert.ThrowsExactly<FormatException>(() => SmartAccountProtocol.ConsumeNonce(0, -1));
            Assert.ThrowsExactly<FormatException>(() => SmartAccountProtocol.ConsumeNonce(0, (BigInteger.One << 64) + 1));
        }
    }
}
