using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_Notary
    {
        private DataCache _snapshot;
        private Block _persistingBlock;

        [TestInitialize]
        public void TestSetup()
        {
            _snapshot = TestBlockchain.GetTestSnapshot();
            _persistingBlock = new Block { Header = new Header() };
        }

        [TestMethod]
        public void Check_MaxNotValidBeforeDeltaAndNotaryServiceFeePerKey()
        {
            var snapshot = _snapshot.CreateSnapshot();
            NativeContract.Notary.GetMaxNotValidBeforeDelta(snapshot).Should().Be(5760);
            NativeContract.Notary.GetNotaryServiceFeePerKey(snapshot).Should().Be(140);
        }

        internal static StorageKey CreateStorageKey(byte prefix, uint key)
        {
            return CreateStorageKey(prefix, BitConverter.GetBytes(key));
        }

        internal static StorageKey CreateStorageKey(byte prefix, byte[] key = null)
        {
            StorageKey storageKey = new()
            {
                Id = NativeContract.NEO.Id,
                Key = new byte[sizeof(byte) + (key?.Length ?? 0)]
            };
            storageKey.Key[0] = prefix;
            key?.CopyTo(storageKey.Key.AsSpan(1));
            return storageKey;
        }
    }
}
