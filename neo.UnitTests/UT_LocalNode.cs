using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network;
using System.IO;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_LocalNode
    {
        LocalNode uut;

        [TestInitialize]
        public void TestSetup()
        {
            uut = new LocalNode();
        }

        [TestMethod]
        public void LocalNode_SaveState()
        {
            MemoryStream ms = new MemoryStream();
            uut.SaveState(ms);
            ms.Close();
		}

		[TestMethod]
		public void LocalNode_SaveLoadState()
		{
			MemoryStream ms = new MemoryStream();
			uut.SaveState(ms);
			ms.Position = 0;
			LocalNode.LoadState(ms);
			ms.Close();
		}
    }
}