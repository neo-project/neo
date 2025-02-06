// Copyright (C) 2015-2025 The Neo Project.
//
// TestUtils.cs file belongs to the neo project and is free
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
using Neo.Json;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using System;
using System.Linq;

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
            Assert.AreEqual("{\"name\":\"noname\",\"version\":\"1.0\",\"scrypt\":{\"n\":2,\"r\":1,\"p\":1},\"accounts\":[],\"extra\":null}", wallet.ToString());
            return new NEP6Wallet(null, password, TestProtocolSettings.Default, wallet);
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

        public static void FillMemoryPool(DataCache snapshot, NeoSystem system, NEP6Wallet wallet, WalletAccount account)
        {
            for (int i = 0; i < system.Settings.MemoryPoolMaxTransactions; i++)
            {
                var tx = CreateValidTx(snapshot, wallet, account);
                system.MemPool.TryAdd(tx, snapshot);
            }
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
