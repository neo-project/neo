using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Json;

namespace Neo.UnitTests.IO.Json
{
    [TestClass]
    public class UT_JBoolean
    {
        private JBoolean jFalse;
        private JBoolean jTrue;

        [TestInitialize]
        public void SetUp()
        {
            jFalse = new JBoolean();
            jTrue = new JBoolean(true);
        }

        [TestMethod]
        public void TestAsNumber()
        {
            jFalse.AsNumber().Should().Be(0);
            jTrue.AsNumber().Should().Be(1);
        }
    }
}
