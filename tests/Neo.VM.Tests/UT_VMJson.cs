// Copyright (C) 2015-2024 The Neo Project.
//
// UT_VMJson.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Test.Extensions;
using Neo.Test.Types;
using System;
using System.IO;
using System.Text;

namespace Neo.Test
{
    [TestClass]
    public class UT_VMJson : VMJsonTestBase
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
