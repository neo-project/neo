// Copyright (C) 2015-2026 The Neo Project.
//
// UT_OracleResponse.cs file belongs to the neo project and is free
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
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_OracleResponse
    {
        [TestMethod]
        public void Serialize_Deserialize_Success_WithResult()
        {
            var original = new OracleResponse
            {
                Id = 42,
                Code = OracleResponseCode.Success,
                Result = (byte[])[0x01, 0x02, 0x03]
            };

            Assert.AreEqual(TransactionAttributeType.OracleResponse, original.Type);
            Assert.IsFalse(original.AllowMultiple);
            Assert.AreEqual(1 + sizeof(ulong) + sizeof(OracleResponseCode) + original.Result.GetVarSize(), original.Size);

            var bytes = original.ToArray();
            var reader = new MemoryReader(bytes);
            var clone = (OracleResponse)TransactionAttribute.DeserializeFrom(ref reader);

            Assert.AreEqual(original.Id, clone.Id);
            Assert.AreEqual(original.Code, clone.Code);
            Assert.AreSequenceEqual(original.Result.ToArray(), clone.Result.ToArray());
        }

        [TestMethod]
        public void Serialize_Deserialize_NonSuccess_EmptyResult()
        {
            var original = new OracleResponse
            {
                Id = 7,
                Code = OracleResponseCode.Timeout,
                Result = ReadOnlyMemory<byte>.Empty
            };

            var bytes = original.ToArray();
            var reader = new MemoryReader(bytes);
            var clone = (OracleResponse)TransactionAttribute.DeserializeFrom(ref reader);

            Assert.AreEqual(OracleResponseCode.Timeout, clone.Code);
            Assert.AreEqual(0, clone.Result.Length);
        }

        [TestMethod]
        public void Deserialize_NonSuccess_WithResult_Throws()
        {
            using var ms = new System.IO.MemoryStream();
            using var writer = new System.IO.BinaryWriter(ms);
            writer.Write((byte)TransactionAttributeType.OracleResponse);
            writer.Write(1UL);
            writer.Write((byte)OracleResponseCode.Timeout);
            writer.WriteVarBytes([0x01]);
            writer.Flush();
            var data = ms.ToArray();

            void Act()
            {
                var reader = new MemoryReader(data);
                TransactionAttribute.DeserializeFrom(ref reader);
            }
            Assert.ThrowsExactly<FormatException>(Act);
        }

        [TestMethod]
        public void ToJson_IncludesFields()
        {
            var response = new OracleResponse
            {
                Id = 9,
                Code = OracleResponseCode.Success,
                Result = (byte[])[0x0A, 0x0B]
            };

            var json = response.ToJson();
            Assert.AreEqual(9UL, (ulong)json["id"].GetNumber());
            // Enum may serialize as number or string depending on JToken conversion
            Assert.IsNotNull(json["code"]);
            Assert.AreEqual(Convert.ToBase64String([0x0A, 0x0B]), json["result"].GetString());
        }

        [TestMethod]
        public void FixedScript_IsNonEmpty()
        {
            Assert.IsTrue(OracleResponse.FixedScript.Length > 0);
        }

        [TestMethod]
        public void Verify_AllowsResponseAfterOriginalSignerIsBlocked()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache().CloneCache();
            var account = UInt160.Parse("0x0000000000000000000000000000000000001234");
            var otherAccount = UInt160.Parse("0x0000000000000000000000000000000000005678");
            var original = new Transaction
            {
                Script = new byte[] { (byte)OpCode.RET },
                Signers =
                [
                    new Signer { Account = account, Scopes = WitnessScope.Global },
                    new Signer { Account = otherAccount, Scopes = WitnessScope.Global }
                ],
                Attributes = [],
                Witnesses = [],
                ValidUntilBlock = 100
            };
            snapshot.Add(NativeContract.Ledger.CreateStorageKey(11, original.Hash),
                new StorageItem(new TransactionState { BlockIndex = 0, Transaction = original }));
            const ulong requestId = 7;
            const long gasForResponse = 10_00000000;
            snapshot.Add(new KeyBuilder(NativeContract.Oracle.Id, 7).AddBigEndian(requestId),
                StorageItem.CreateSealed(new OracleRequest
                {
                    OriginalTxid = original.Hash,
                    GasForResponse = gasForResponse,
                    Url = "https://example.com",
                    CallbackContract = UInt160.Zero,
                    CallbackMethod = "callback",
                    UserData = []
                }));

            var oracleKey = new Neo.Wallets.KeyPair(Enumerable.Repeat((byte)0x22, 32).ToArray());
            var committee = NativeContract.NEO.GetCommitteeAddress(snapshot);
            var block = new Block
            {
                Header = new Header
                {
                    Index = 0,
                    Timestamp = 10_000,
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    NextConsensus = UInt160.Zero,
                    Witness = Witness.Empty
                },
                Transactions = []
            };
            NativeContract.RoleManagement.Call(snapshot,
                new Nep17NativeContractExtensions.ManualWitness(committee), block, "designateAsRole",
                new ContractParameter(ContractParameterType.Integer) { Value = (int)Role.Oracle },
                new ContractParameter(ContractParameterType.Array)
                {
                    Value = new List<ContractParameter>
                    {
                        new(ContractParameterType.PublicKey) { Value = oracleKey.PublicKey }
                    }
                });

            var response = new OracleResponse { Id = requestId, Code = OracleResponseCode.Timeout };
            var responseTx = new Transaction
            {
                Script = OracleResponse.FixedScript,
                Signers =
                [
                    new Signer
                    {
                        Account = Contract.GetBFTAddress([oracleKey.PublicKey]),
                        Scopes = WitnessScope.None
                    },
                    new Signer { Account = NativeContract.Oracle.Hash, Scopes = WitnessScope.None }
                ],
                Attributes = [response],
                Witnesses = [],
                SystemFee = gasForResponse,
                ValidUntilBlock = 100
            };

            Assert.IsTrue(response.Verify(snapshot, responseTx));
            snapshot.Add(NativeContract.Policy.CreateStorageKey(15, account), new StorageItem(new byte[] { 1 }));
            Assert.IsTrue(response.Verify(snapshot, responseTx));
            snapshot.Delete(NativeContract.Policy.CreateStorageKey(15, account));
            Assert.IsTrue(response.Verify(snapshot, responseTx));
            snapshot.Add(NativeContract.Policy.CreateStorageKey(15, otherAccount), new StorageItem(new byte[] { 1 }));
            Assert.IsTrue(response.Verify(snapshot, responseTx));
        }

        [TestMethod]
        public void Verify_WithoutDesignatedOracleNodes_ReturnsFalse()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache().CloneCache();
            var request = new OracleRequest
            {
                OriginalTxid = UInt256.Zero,
                GasForResponse = 10_00000000,
                Url = "https://example.com",
                CallbackContract = UInt160.Zero,
                CallbackMethod = "callback",
                UserData = []
            };
            snapshot.Add(new KeyBuilder(NativeContract.Oracle.Id, 7).AddBigEndian(42UL), StorageItem.CreateSealed(request));

            Assert.AreEqual(0, NativeContract.RoleManagement.GetDesignatedByRole(
                snapshot, Role.Oracle, NativeContract.Ledger.CurrentIndex(snapshot) + 1).Length);

            var response = new OracleResponse { Id = 42 };
            var tx = new Transaction
            {
                Script = OracleResponse.FixedScript,
                Signers = [new Signer { Account = UInt160.Zero, Scopes = WitnessScope.None }],
                NetworkFee = request.GasForResponse,
                Attributes = [response],
                Witnesses = []
            };

            Assert.IsFalse(response.Verify(snapshot, tx));
        }
    }
}
