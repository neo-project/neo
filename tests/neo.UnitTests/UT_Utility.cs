using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_Utility
    {
        [TestMethod]
        public void LoadConfig()
        {
            Assert.IsFalse(File.Exists("test.json"));
            Utility.LoadConfig("test").GetSection("test").Value.Should().BeNull();

            File.WriteAllText("test.json", @"{""test"":1}");
            Assert.IsTrue(File.Exists("test.json"));
            Utility.LoadConfig("test").GetSection("test").Value.Should().Be("1");
            File.Delete("test.json");
        }
    }
}
