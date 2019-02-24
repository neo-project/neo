using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Ledger;
using Neo.Ledger.MPT;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_MerklePatriciaDataCache
    {
        private Random random;

        [TestInitialize]
        public void StartUp()
        {
            random = new Random();
        }

        private byte[] randByteArray(uint size = 32)
        {
            var resp = new byte[size];
            random.NextBytes(resp);
            return resp;
        }

        private StorageKey randStorageKey() =>
            new StorageKey {Key = randByteArray(12), ScriptHash = new UInt160(randByteArray(20))};

        private StorageItem randStorageItem() =>
            new StorageItem {Value = randByteArray(64), IsConstant = random.Next() % 2 == 0};

        private static void Add(MerklePatricia mp, StorageKey key, StorageItem item) =>
            mp[key.ToArray()] = item.ToArray();

        private static StorageItem Get(MerklePatricia mp, StorageKey key) =>
            mp[key.ToArray()].ToStorageItem();

        private static bool Remove(MerklePatricia mp, StorageKey key) =>
            mp.Remove(key.ToArray());

        private static bool ContainsKey(MerklePatricia mp, StorageKey key) =>
            mp.ContainsKey(key.ToArray());

        [TestMethod]
        public void StorageKeyStorageItem()
        {
            var mp = new[]
            {
                new MerklePatriciaDataCache(new TestDataCache<MPTKey, MerklePatriciaNode>(), null),
                new MerklePatriciaDataCache(new TestDataCache<MPTKey, MerklePatriciaNode>(), null)
            };

            const int size = 50;
            var key = new StorageKey[size];
            var item = new StorageItem[size];
            for (var i = 0; i < size; ++i)
            {
                key[i] = randStorageKey();
                item[i] = randStorageItem();
            }

            for (var i = 0; i < size; ++i)
            {
                Assert.AreEqual(mp[0], mp[1]);
                if (ContainsKey(mp[0], key[i])) continue;

                Add(mp[0], key[i], item[i]);
                Assert.AreNotEqual(mp[0], mp[1]);
                Add(mp[1], key[i], item[i]);
                Assert.AreEqual(mp[0], mp[1]);

                Assert.AreEqual(item[i], Get(mp[0], key[i]));
                Assert.AreEqual(item[i], Get(mp[1], key[i]));
            }

            for (var i = 0; i < 2 * size; ++i)
            {
                Assert.AreEqual(mp[0], mp[1]);
                var index = random.Next() % size;
                if (!ContainsKey(mp[0], key[index])) continue;
                Remove(mp[0], key[index]);
                Assert.AreNotEqual(mp[0], mp[1]);
                Remove(mp[1], key[index]);
                Assert.AreEqual(mp[0], mp[1]);

                if (ContainsKey(mp[0], key[index])) continue;
                Add(mp[0], key[index], item[index]);
                Assert.AreNotEqual(mp[0], mp[1]);
                Add(mp[1], key[index], item[index]);
                Assert.AreEqual(mp[0], mp[1]);
            }

        }
    }
}