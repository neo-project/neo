/*
namespace com.github.neoresearch.NeoDataStructureTest
{
    using System.Collections.Generic;
    using NeoDataStructure;
    using Xunit;

    public class MPTTest
    {
        [Fact]
        public void DistinctRoot()
        {
            var mp = new MPT();
            Assert.False(mp == null);
            mp[new byte[] {0x0, 0x0, 0x1}] = new byte[] {0x0, 0x0, 0x1};
            Assert.Equal(new byte[] {0x0, 0x0, 0x1}, mp[new byte[] {0x0, 0x0, 0x1}]);

            mp[new byte[] {0x11, 0x0, 0x2}] = new byte[] {0x11, 0x0, 0x2};
            Assert.Equal(new byte[] {0x11, 0x0, 0x2}, mp[new byte[] {0x11, 0x0, 0x2}]);
        }

        [Fact]
        public void SameRoot()
        {
            var mp = new MPT();
            Assert.False(mp == null);
            mp[new byte[] {0x0, 0x0, 0x1}] = new byte[] {0x0, 0x0, 0x1};
            Assert.Equal(new byte[] {0x0, 0x0, 0x1}, mp[new byte[] {0x0, 0x0, 0x1}]);

            mp[new byte[] {0x0, 0x0, 0x1}] = new byte[] {0x0, 0x0, 0x2};
            Assert.Equal(new byte[] {0x0, 0x0, 0x2}, mp[new byte[] {0x0, 0x0, 0x1}]);
        }
        
        [Fact]
        public void ColideKeys()
        {
            var mp = new MPT
            {
                ["oi"] = "batata",
                ["oi"] = "batatatinha"
            };
            Assert.True(mp.ContainsKey("oi"));
            Assert.Equal("batatatinha", mp["oi"]);

            mp["orelha"] = "batatatinha";
            Assert.Equal("batatatinha", mp["orelha"]);

            mp["orfão"] = "criança";
            Assert.Equal("criança", mp["orfão"]);

            mp["orfanato"] = "crianças";
            Assert.Equal("crianças", mp["orfanato"]);

            Assert.True(mp.Remove("orfanato"));
            Assert.Equal("criança", mp["orfão"]);
            Assert.False(mp.ContainsKey("orfanato"));

            mp["orfã"] = "menina";
            Assert.Equal("menina", mp["orfã"]);
        }
    }
}
*/