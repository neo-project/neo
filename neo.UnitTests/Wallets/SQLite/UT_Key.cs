using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.IO.Wrappers;
using Neo.Wallets.SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_Key
    {
        [TestMethod]
        public void TestGenerator()
        {
            Key key = new Key();
            Assert.IsNotNull(key);
        }

        [TestMethod]
        public void TestGetName()
        {
            Key key = new Key();
            key.Name = "AAA";
            Assert.AreEqual("AAA", key.Name);
        }

        [TestMethod]
        public void TestSetName()
        {
            Key key = new Key();
            key.Name = "AAA";
            Assert.AreEqual("AAA", key.Name);
        }

        [TestMethod]
        public void TestGetValue()
        {
            Key key = new Key();
            key.Value= new byte[] { 0x01 };
            Assert.AreEqual(Encoding.Default.GetString(new byte[] { 0x01 }), Encoding.Default.GetString(key.Value));
        }

        [TestMethod]
        public void TestSetValue()
        {
            Key key = new Key();
            key.Value = new byte[] { 0x01 };
            Assert.AreEqual(Encoding.Default.GetString(new byte[] { 0x01 }), Encoding.Default.GetString(key.Value));
        }
    }
}
