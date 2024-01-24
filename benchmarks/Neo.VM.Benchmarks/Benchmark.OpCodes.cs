// Copyright (C) 2015-2024 The Neo Project.
//
// Benchmark.OpCodes.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Test;
using Neo.Test.Extensions;
using Neo.Test.Types;
using System.Text;

namespace Neo.VM
{
    public class BenchmarkOpCodes : VMJsonTestBase
    {
        private const string Root = "../../../../../../../../../tests/Neo.VM.Tests/";
        private List<VMUT> _others = new();
        private List<VMUT> _opCodesArrays = new();
        private List<VMUT> _opCodesStack = new();
        private List<VMUT> _opCodesSlot = new();
        private List<VMUT> _opCodesSplice = new();
        private List<VMUT> _opCodesControl = new();
        private List<VMUT> _opCodesPush = new();
        private List<VMUT> _opCodesArithmetic = new();
        private List<VMUT> _opCodesBitwiseLogic = new();
        private List<VMUT> _opCodesTypes = new();

        [GlobalSetup]
        public void Setup()
        {
            _others = TestJson("Tests/Others");
            _opCodesArrays = TestJson("Tests/OpCodes/Arrays");
            _opCodesStack = TestJson("Tests/OpCodes/Stack");
            _opCodesSlot = TestJson("Tests/OpCodes/Slot");
            _opCodesSplice = TestJson("Tests/OpCodes/Splice");
            _opCodesControl = TestJson("Tests/OpCodes/Control");
            _opCodesPush = TestJson("Tests/OpCodes/Push");
            _opCodesArithmetic = TestJson("Tests/OpCodes/Arithmetic");
            _opCodesBitwiseLogic = TestJson("Tests/OpCodes/BitwiseLogic");
            _opCodesTypes = TestJson("Tests/OpCodes/Types");
        }

        [Benchmark]
        public void TestOthers() => ExecuteTest(_others);

        [Benchmark]
        public void TestOpCodesArrays() => ExecuteTest(_opCodesArrays);

        [Benchmark]
        public void TestOpCodesStack() => ExecuteTest(_opCodesStack);

        [Benchmark]
        public void TestOpCodesSlot() => ExecuteTest(_opCodesSlot);

        [Benchmark]
        public void TestOpCodesSplice() => ExecuteTest(_opCodesSplice);

        [Benchmark]
        public void TestOpCodesControl() => ExecuteTest(_opCodesControl);

        [Benchmark]
        public void TestOpCodesPush() => ExecuteTest(_opCodesPush);

        [Benchmark]
        public void TestOpCodesArithmetic() => ExecuteTest(_opCodesArithmetic);

        [Benchmark]
        public void TestOpCodesBitwiseLogic() => ExecuteTest(_opCodesBitwiseLogic);

        [Benchmark]
        public void TestOpCodesTypes() => ExecuteTest(_opCodesTypes);

        private List<VMUT> TestJson(string path)
        {
            List<VMUT> list = new();
            path = Root + path;
            foreach (var file in Directory.GetFiles(path, "*.json", SearchOption.AllDirectories))
            {
                var realFile = Path.GetFullPath(file);
                var json = File.ReadAllText(realFile, Encoding.UTF8);
                var ut = json.DeserializeJson<VMUT>();

                Assert.IsFalse(string.IsNullOrEmpty(ut.Name), "Name is required");

                if (json != ut.ToJson().Replace("\r\n", "\n"))
                {
                    // Format json
                    // Console.WriteLine($"The file '{realFile}' was optimized");
                    //File.WriteAllText(realFile, ut.ToJson().Replace("\r\n", "\n"), Encoding.UTF8);
                }
                list.Add(ut);
            }

            return list;
        }

        private void ExecuteTest(List<VMUT> list)
        {
            foreach (var ut in list)
            {
                try
                {
                    ExecuteTest(ut);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }
}
