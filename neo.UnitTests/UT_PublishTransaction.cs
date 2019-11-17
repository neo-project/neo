using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using System;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_PublishTransaction
    {
        [TestMethod]
        public void TestBigScript()
        {
#pragma warning disable CS0612 // Type or member is obsolete
            var uut = new PublishTransaction()
#pragma warning restore CS0612 // Type or member is obsolete
            {
                Attributes = new TransactionAttribute[0],
                Author = "Author",
                CodeVersion = "CodeVersion",
                Description = "Description",
                Email = "Email",
                Name = "Name",
                Inputs = new CoinReference[0],
                Outputs = new TransactionOutput[0],
                ReturnType = SmartContract.ContractParameterType.Array,
                ParameterList = new SmartContract.ContractParameterType[0],
                Witnesses = new Witness[0],
                Script = new byte[0],
            };

            // No error

            var raw = uut.ToArray();
            var copy = Transaction.DeserializeFrom(raw);

            // Error

            uut.Script = new byte[ushort.MaxValue * 2];
            raw = uut.ToArray();

            Assert.ThrowsException<FormatException>(() => Transaction.DeserializeFrom(raw));
        }
    }
}
