using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Core;
using Neo.Cryptography.ECC;
using Neo.IO;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_AccountState
    {
        AccountState uut;

        [TestInitialize]
        public void TestSetup()
        {
            uut = new AccountState();
        }

        [TestMethod]
        public void ScriptHash_Get()
        {
            uut.ScriptHash.Should().BeNull();
        }

        [TestMethod]
        public void ScriptHash_Set()
        {
            UInt160 val = new UInt160();
            uut.ScriptHash = val;
            uut.ScriptHash.Should().Be(val);
        }

        [TestMethod]
        public void IsFrozen_Get()
        {
            uut.IsFrozen.Should().Be(false);
        }

        [TestMethod]
        public void IsFrozen_Set()
        {
            uut.IsFrozen = true;
            uut.IsFrozen.Should().Be(true);
        }

        [TestMethod]
        public void Votes_Get()
        {
            uut.Votes.Should().BeNull();
        }

        [TestMethod]
        public void Votes_Set()
        {
            ECPoint val = new ECPoint();
            ECPoint[] array = new ECPoint[] { val };
            uut.Votes = array;
            uut.Votes[0].Should().Be(val);
        }

        [TestMethod]
        public void Balances_Get()
        {
            uut.Balances.Should().BeNull();
        }

        [TestMethod]
        public void Balances_Set()
        {
            UInt256 key = new UInt256();
            Fixed8 val = new Fixed8();
            Dictionary<UInt256, Fixed8> dict = new Dictionary<UInt256, Fixed8>();
            dict.Add(key, val);
            uut.Balances = dict;
            uut.Balances[key].Should().Be(val);
        }

        [TestMethod]
        public void Size_Get_0_Votes_0_Balances()
        {
            UInt160 val = new UInt160();
            ECPoint[] array = new ECPoint[0];
            Dictionary<UInt256, Fixed8> dict = new Dictionary<UInt256, Fixed8>();

            uut.ScriptHash = val;
            uut.Votes = array;
            uut.Balances = dict;

            uut.Size.Should().Be(24); // 1 + 20 + 1 + 1 + 1 + 0 * (32 + 8)
        }

        [TestMethod]
        public void Size_Get_1_Vote_0_Balances()
        {
            UInt160 val = new UInt160();
            ECPoint[] array = new ECPoint[] { new ECPoint() };
            Dictionary<UInt256, Fixed8> dict = new Dictionary<UInt256, Fixed8>();

            uut.ScriptHash = val;
            uut.Votes = array;
            uut.Balances = dict;

            uut.Size.Should().Be(25); // 1 + 20 + 1 + 2 + 1 + 0 * (32 + 8)
        }

        [TestMethod]
        public void Size_Get_5_Votes_0_Balances()
        {
            UInt160 val = new UInt160();
            ECPoint[] array = new ECPoint[] { new ECPoint(), new ECPoint(), new ECPoint(), new ECPoint(), new ECPoint() };
            Dictionary<UInt256, Fixed8> dict = new Dictionary<UInt256, Fixed8>();

            uut.ScriptHash = val;
            uut.Votes = array;
            uut.Balances = dict;

            uut.Size.Should().Be(29); // 1 + 20 + 1 + 6 + 1 + 0 * (32 + 8)
        }

        [TestMethod]
        public void Size_Get_0_Votes_1_Balance()
        {
            UInt160 val = new UInt160();
            ECPoint[] array = new ECPoint[0];
            Dictionary<UInt256, Fixed8> dict = new Dictionary<UInt256, Fixed8>();
            dict.Add(new UInt256(), new Fixed8());

            uut.ScriptHash = val;
            uut.Votes = array;
            uut.Balances = dict;

            uut.Size.Should().Be(64); // 1 + 20 + 1 + 1 + 1 + 1 * (32 + 8)
        }

        [TestMethod]
        public void Size_Get_0_Votes_5_Balance()
        {
            UInt160 val = new UInt160();
            ECPoint[] array = new ECPoint[0];
            Dictionary<UInt256, Fixed8> dict = new Dictionary<UInt256, Fixed8>();
            dict.Add(new UInt256(), new Fixed8());
            dict.Add(new UInt256(TestUtils.GetByteArray(32, 0x20)), new Fixed8());
            dict.Add(new UInt256(TestUtils.GetByteArray(32, 0x21)), new Fixed8());
            dict.Add(new UInt256(TestUtils.GetByteArray(32, 0x22)), new Fixed8());
            dict.Add(new UInt256(TestUtils.GetByteArray(32, 0x23)), new Fixed8());

            uut.ScriptHash = val;
            uut.Votes = array;
            uut.Balances = dict;

            uut.Size.Should().Be(224); // 1 + 20 + 1 + 1 + 1 + 5 * (32 + 8)
        }

        [TestMethod]
        public void Size_Get_5_Votes_5_Balance()
        {
            UInt160 val = new UInt160();
            ECPoint[] array = new ECPoint[] { new ECPoint(), new ECPoint(), new ECPoint(), new ECPoint(), new ECPoint() };
            Dictionary<UInt256, Fixed8> dict = new Dictionary<UInt256, Fixed8>();
            dict.Add(new UInt256(), new Fixed8());
            dict.Add(new UInt256(TestUtils.GetByteArray(32, 0x20)), new Fixed8());
            dict.Add(new UInt256(TestUtils.GetByteArray(32, 0x21)), new Fixed8());
            dict.Add(new UInt256(TestUtils.GetByteArray(32, 0x22)), new Fixed8());
            dict.Add(new UInt256(TestUtils.GetByteArray(32, 0x23)), new Fixed8());

            uut.ScriptHash = val;
            uut.Votes = array;
            uut.Balances = dict;

            uut.Size.Should().Be(229); // 1 + 20 + 1 + 6 + 1 + 5 * (32 + 8)
        }

        private void setupAccountStateWithValues(AccountState accState, out UInt160 scriptHashVal, out ECPoint votesVal, out UInt256 key, out Fixed8 dictVal)
        {
            scriptHashVal = new UInt160();
            accState.ScriptHash = scriptHashVal;
            accState.IsFrozen = true;
            votesVal = new ECPoint();
            ECPoint[] array = new ECPoint[] { votesVal };
            accState.Votes = array;
            key = new UInt256();
            dictVal = new Fixed8();
            Dictionary<UInt256, Fixed8> dict = new Dictionary<UInt256, Fixed8>();
            dict.Add(key, dictVal);
            accState.Balances = dict;
        }

        [TestMethod]
        public void Clone()
        {
            UInt160 scriptHashVal;
            ECPoint votesVal;
            UInt256 key;
            Fixed8 dictVal;
            setupAccountStateWithValues(uut, out scriptHashVal, out votesVal, out key, out dictVal);

            AccountState newAs = ((ICloneable<AccountState>)uut).Clone();
            newAs.ScriptHash.Should().Be(scriptHashVal);
            newAs.IsFrozen.Should().Be(true);
            newAs.Votes[0].Should().Be(votesVal);
            newAs.Balances.Count.Should().Be(1);
            newAs.Balances.Should().ContainKey(key);
            newAs.Balances[key].Should().Be(dictVal);
        }

        [TestMethod]
        public void FromReplica()
        {
            AccountState accState = new AccountState();
            UInt160 scriptHashVal;
            ECPoint votesVal;
            UInt256 key;
            Fixed8 dictVal;
            setupAccountStateWithValues(accState, out scriptHashVal, out votesVal, out key, out dictVal);

            ((ICloneable<AccountState>)uut).FromReplica(accState);
            uut.ScriptHash.Should().Be(scriptHashVal);
            uut.IsFrozen.Should().Be(true);
            uut.Votes[0].Should().Be(votesVal);
            uut.Balances.Count.Should().Be(1);
            uut.Balances.Should().ContainKey(key);
            uut.Balances[key].Should().Be(dictVal);
        }

        [TestMethod]
        public void Deserialize()
        {
            byte[] data = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0 };
            int index = 0;
            using (MemoryStream ms = new MemoryStream(data, index, data.Length - index, false))
            {
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    uut.Deserialize(reader);
                }
            }
            uut.IsFrozen.Should().Be(true);
        }


        [TestMethod]
        public void Serialize()
        {
            UInt160 scriptHashVal;
            ECPoint votesVal;
            UInt256 key;
            Fixed8 dictVal;
            setupAccountStateWithValues(uut, out scriptHashVal, out votesVal, out key, out dictVal);

            byte[] data;
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII, true))
                {
                    uut.Serialize(writer);
                    data = stream.ToArray();
                }
            }

            byte[] requiredData = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0 };

            data.Length.Should().Be(25);
            for (int i=0; i<25; i++)
            {
                data[i].Should().Be(requiredData[i]);
            }
        }
    }
}
