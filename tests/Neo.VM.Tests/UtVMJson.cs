using System;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Test.Extensions;
using Neo.Test.Types;

namespace Neo.Test
{
    [TestClass]
    public class UtVMJson : VMJsonTestBase
    {
        [TestMethod]
        public void TestOthers() => TestJson("./Tests/Others");

        [TestMethod]
        public void TestOpCodesArrays() => TestJson("./Tests/OpCodes/Arrays");

        [TestMethod]
        public void TestOpCodesStack() => TestJson("./Tests/OpCodes/Stack");

        [TestMethod]
        public void TestOpCodesSlot() => TestJson("./Tests/OpCodes/Slot");

        [TestMethod]
        public void TestOpCodesSplice() => TestJson("./Tests/OpCodes/Splice");

        [TestMethod]
        public void TestOpCodesControl() => TestJson("./Tests/OpCodes/Control");

        [TestMethod]
        public void TestOpCodesPush() => TestJson("./Tests/OpCodes/Push");

        [TestMethod]
        public void TestOpCodesArithmetic() => TestJson("./Tests/OpCodes/Arithmetic");

        [TestMethod]
        public void TestOpCodesBitwiseLogic() => TestJson("./Tests/OpCodes/BitwiseLogic");

        [TestMethod]
        public void TestOpCodesTypes() => TestJson("./Tests/OpCodes/Types");

        private void TestJson(string path)
        {
            foreach (var file in Directory.GetFiles(path, "*.json", SearchOption.AllDirectories))
            {
                Console.WriteLine($"Processing file '{file}'");

                var realFile = Path.GetFullPath(file);
                var json = File.ReadAllText(realFile, Encoding.UTF8);
                var ut = json.DeserializeJson<VMUT>();

                Assert.IsFalse(string.IsNullOrEmpty(ut.Name), "Name is required");

                if (json != ut.ToJson().Replace("\r\n", "\n"))
                {
                    // Format json

                    Console.WriteLine($"The file '{realFile}' was optimized");
                    //File.WriteAllText(realFile, ut.ToJson().Replace("\r\n", "\n"), Encoding.UTF8);
                }

                try
                {
                    ExecuteTest(ut);
                }
                catch (Exception ex)
                {
                    throw new AggregateException("Error in file: " + realFile, ex);
                }
            }
        }
    }
}
