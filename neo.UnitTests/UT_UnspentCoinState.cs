using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Core;
using Neo.IO.Json;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_UnspentCoinState
    {
        UnspentCoinState uut;

        [TestInitialize]
        public void TestSetup()
        {
            uut = new UnspentCoinState();
        }

        [TestMethod]
        public void Items_Get()
        {
            uut.Items.Should().BeNull();
        }

        [TestMethod]
        public void Items_Set()
        {
            CoinState item1 = CoinState.Confirmed;
            CoinState item2 = CoinState.Locked;
            CoinState[] val = new CoinState[] { item1, item2 };
            uut.Items = val;
            uut.Items.Length.Should().Be(val.Length);
            uut.Items[0].Should().Be(item1);
            uut.Items[1].Should().Be(item2);
        }

        [TestMethod]
        public void Size_Get_1()
        {
            CoinState item1 = CoinState.Confirmed;
            CoinState[] val = new CoinState[] { item1 };
            uut.Items = val;

            uut.Size.Should().Be(3); // 1 + 2
        }

        [TestMethod]
        public void Size_Get_3()
        {
            CoinState item1 = CoinState.Confirmed;
            CoinState item2 = CoinState.Locked;
            CoinState item3 = CoinState.Spent;
            CoinState[] val = new CoinState[] { item1, item2, item3 };
            uut.Items = val;

            uut.Size.Should().Be(5); // 1 + 4
        }

        [TestMethod]
        public void Deserialize()
        {
            byte[] data = new byte[] { 0, 2, 1, 16 };
            int index = 0;
            using (MemoryStream ms = new MemoryStream(data, index, data.Length - index, false))
            {
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    uut.Deserialize(reader);
                }
            }
            uut.Items.Length.Should().Be(2);
            uut.Items[0].Should().Be(CoinState.Confirmed);
            uut.Items[1].Should().Be(CoinState.Locked);
        }

        [TestMethod]
        public void Serialize()
        {
            CoinState item1 = CoinState.Confirmed;
            CoinState item2 = CoinState.Locked;
            CoinState[] val = new CoinState[] { item1, item2 };
            uut.Items = val;

            byte[] data;
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII, true))
                {
                    uut.Serialize(writer);
                    data = stream.ToArray();
                }
            }

            byte[] requiredData = new byte[] { 0, 2, 1, 16 };

            data.Length.Should().Be(requiredData.Length);
            for (int i = 0; i < requiredData.Length; i++)
            {
                data[i].Should().Be(requiredData[i]);
            }
        }

    }
}
