// Copyright (C) 2015-2025 The Neo Project.
//
// UT_JsonStringUInt160Converter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Text.Json;

namespace Neo.Build.Core.Tests.Json.Converters
{
    [TestClass]
    public class UT_JsonStringUInt160Converter
    {
        private readonly JsonSerializerOptions _options;

        private class TestJson
        {
            public UInt160 Test { get; set; }
        };

        public UT_JsonStringUInt160Converter()
        {
            _options = NeoBuildDefaults.JsonDefaultSerializerOptions;
            _options.WriteIndented = false;
        }

        [TestMethod]
        public void TestReadJson()
        {
            UInt160 expectedScriptHash = "0xff00000000000000000000000000000000000001";
            var expectedJsonString = $"{{\"test\":\"{expectedScriptHash}\"}}";

            var actualObject = JsonSerializer.Deserialize<TestJson>(expectedJsonString, _options);
            var actualScriptHash = actualObject.Test;

            Assert.AreEqual(expectedScriptHash, actualScriptHash);
        }

        [TestMethod]
        public void TestWriteJson()
        {
            UInt160 expectedScriptHash = "0xff00000000000000000000000000000000000001";
            var expectedJsonString = $"{{\"test\":\"{expectedScriptHash}\"}}";
            var expectedJsonObj = new TestJson() { Test = expectedScriptHash, };

            var actualJsonString = JsonSerializer.Serialize(expectedJsonObj, _options);

            Assert.AreEqual(expectedJsonString, actualJsonString);
        }
    }
}
