// Copyright (C) 2015-2024 The Neo Project.
//
// TestUtils.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Json;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using System;
using System.Linq;
using System.Numerics;

namespace Neo.UnitTests
{
    public static partial class TestUtils
    {
        public static readonly Random TestRandom = new Random(1337); // use fixed seed for guaranteed determinism

        public static UInt256 RandomUInt256()
        {
            byte[] data = new byte[32];
            TestRandom.NextBytes(data);
            return new UInt256(data);
        }

        public static UInt160 RandomUInt160()
        {
            byte[] data = new byte[20];
            TestRandom.NextBytes(data);
            return new UInt160(data);
        }

        public static ContractManifest CreateDefaultManifest()
        {
            return new ContractManifest()
            {
                Name = "testManifest",
                Groups = [],
                SupportedStandards = [],
                Abi = new ContractAbi()
                {
                    Events = [],
                    Methods =
                    [
                        new ContractMethodDescriptor
                        {
                            Name = "testMethod",
                            Parameters = [],
                            ReturnType = ContractParameterType.Void,
                            Offset = 0,
                            Safe = true
                        }
                    ]
                },
                Permissions = [ContractPermission.DefaultPermission],
                Trusts = WildcardContainer<ContractPermissionDescriptor>.Create(),
                Extra = null
            };
        }

        public static ContractManifest CreateManifest(string method, ContractParameterType returnType, params ContractParameterType[] parameterTypes)
        {
            ContractManifest manifest = CreateDefaultManifest();
            manifest.Abi.Methods =
            [
                new ContractMethodDescriptor()
                {
                    Name = method,
                    Parameters = parameterTypes.Select((p, i) => new ContractParameterDefinition
                    {
                        Name = $"p{i}",
                        Type = p
                    }).ToArray(),
                    ReturnType = returnType
                }
            ];
            return manifest;
        }

        public static StorageKey CreateStorageKey(this NativeContract contract, byte prefix, ISerializable key = null)
        {
            var k = new KeyBuilder(contract.Id, prefix);
            if (key != null) k = k.Add(key);
            return k;
        }

        public static StorageKey CreateStorageKey(this NativeContract contract, byte prefix, uint value)
        {
            return new KeyBuilder(contract.Id, prefix).AddBigEndian(value);
        }

        public static byte[] GetByteArray(int length, byte firstByte)
        {
            byte[] array = new byte[length];
            array[0] = firstByte;
            for (int i = 1; i < length; i++)
            {
                array[i] = 0x20;
            }
            return array;
        }

        public static NEP6Wallet GenerateTestWallet(string password)
        {
            JObject wallet = new JObject();
            wallet["name"] = "noname";
            wallet["version"] = new Version("1.0").ToString();
            wallet["scrypt"] = new ScryptParameters(2, 1, 1).ToJson();
            wallet["accounts"] = new JArray();
            wallet["extra"] = null;
            wallet.ToString().Should().Be("{\"name\":\"noname\",\"version\":\"1.0\",\"scrypt\":{\"n\":2,\"r\":1,\"p\":1},\"accounts\":[],\"extra\":null}");
            return new NEP6Wallet(null, password, TestProtocolSettings.Default, wallet);
        }

        public static Transaction CreateValidTx(DataCache snapshot, NEP6Wallet wallet, WalletAccount account)
        {
            return CreateValidTx(snapshot, wallet, account.ScriptHash, (uint)new Random().Next());
        }

        public static Transaction CreateValidTx(DataCache snapshot, NEP6Wallet wallet, UInt160 account, uint nonce)
        {
            var tx = wallet.MakeTransaction(snapshot, [
                    new TransferOutput()
                    {
                        AssetId = NativeContract.GAS.Hash,
                        ScriptHash = account,
                        Value = new BigDecimal(BigInteger.One, 8)
                    }
                ],
                account);

            tx.Nonce = nonce;

            var data = new ContractParametersContext(snapshot, tx, TestProtocolSettings.Default.Network);
            Assert.IsNull(data.GetSignatures(tx.Sender));
            Assert.IsTrue(wallet.Sign(data));
            Assert.IsTrue(data.Completed);
            Assert.AreEqual(1, data.GetSignatures(tx.Sender).Count());

            tx.Witnesses = data.GetWitnesses();
            return tx;
        }


        public static Transaction GetTransaction(UInt160 sender)
        {
            return new Transaction
            {
                Script = new byte[] { (byte)OpCode.PUSH2 },
                Attributes = [],
                Signers =
                [
                    new Signer()
                    {
                        Account = sender,
                        Scopes = WitnessScope.CalledByEntry,
                        AllowedContracts = [],
                        AllowedGroups = [],
                        Rules = [],
                    }
                ],
                Witnesses =
                [
                    new Witness
                    {
                        InvocationScript = Array.Empty<byte>(),
                        VerificationScript = Array.Empty<byte>()
                    }
                ]
            };
        }

        public static ContractState GetContract(string method = "test", int parametersCount = 0)
        {
            NefFile nef = new()
            {
                Compiler = "",
                Source = "",
                Tokens = [],
                Script = new byte[] { 0x01, 0x01, 0x01, 0x01 }
            };
            nef.CheckSum = NefFile.ComputeChecksum(nef);
            return new ContractState
            {
                Id = 0x43000000,
                Nef = nef,
                Hash = nef.Script.Span.ToScriptHash(),
                Manifest = CreateManifest(method, ContractParameterType.Any, Enumerable.Repeat(ContractParameterType.Any, parametersCount).ToArray())
            };
        }

        internal static ContractState GetContract(byte[] script, ContractManifest manifest = null)
        {
            NefFile nef = new()
            {
                Compiler = "",
                Source = "",
                Tokens = [],
                Script = script
            };
            nef.CheckSum = NefFile.ComputeChecksum(nef);
            return new ContractState
            {
                Id = 1,
                Hash = script.ToScriptHash(),
                Nef = nef,
                Manifest = manifest ?? CreateDefaultManifest()
            };
        }

        internal static StorageItem GetStorageItem(byte[] value)
        {
            return new StorageItem
            {
                Value = value
            };
        }

        internal static StorageKey GetStorageKey(int id, byte[] keyValue)
        {
            return new StorageKey
            {
                Id = id,
                Key = keyValue
            };
        }

        public static void StorageItemAdd(DataCache snapshot, int id, byte[] keyValue, byte[] value)
        {
            snapshot.Add(new StorageKey
            {
                Id = id,
                Key = keyValue
            }, new StorageItem(value));
        }

        public static Transaction CreateRandomHashTransaction()
        {
            var randomBytes = new byte[16];
            TestRandom.NextBytes(randomBytes);
            return new Transaction
            {
                Script = randomBytes,
                Attributes = [],
                Signers = [new Signer() { Account = UInt160.Zero }],
                Witnesses =
                [
                    new Witness
                    {
                        InvocationScript = new byte[0],
                        VerificationScript = new byte[0]
                    }
                ]
            };
        }

        public static T CopyMsgBySerialization<T>(T serializableObj, T newObj) where T : ISerializable
        {
            MemoryReader reader = new(serializableObj.ToArray());
            newObj.Deserialize(ref reader);
            return newObj;
        }

        public static bool EqualsTo(this StorageItem item, StorageItem other)
        {
            return item.Value.Span.SequenceEqual(other.Value.Span);
        }
    }
}
