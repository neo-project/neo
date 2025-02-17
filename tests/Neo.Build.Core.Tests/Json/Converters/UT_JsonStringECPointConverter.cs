// Copyright (C) 2015-2025 The Neo Project.
//
// UT_JsonStringECPointConverter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using System.Text.Json;

namespace Neo.Build.Core.Tests.Json.Converters
{
    [TestClass]
    public class UT_JsonStringECPointConverter
    {
        private JsonSerializerOptions _options;

        private class TestJson
        {
            public ECPoint Test { get; set; }
        };

        [TestInitialize]
        public void Setup()
        {
            _options = NeoBuildDefaults.JsonDefaultSerializerOptions;
            _options.WriteIndented = false;
        }

        [TestMethod]
        public void TestReadJson()
        {
            var expectedPointString = "036b17d1f2e12c4247f8bce6e563a440f277037d812deb33a0f4a13945d898c296";
            var expectedJsonString = $"{{\"test\":\"{expectedPointString}\"}}";

            var actualObject = JsonSerializer.Deserialize<TestJson>(expectedJsonString, _options);
            var actualPoint = actualObject.Test;

            Assert.AreEqual(expectedPointString, $"{actualPoint}");
        }

        [TestMethod]
        public void TestWriteJson()
        {
            var expectedPointString = "036b17d1f2e12c4247f8bce6e563a440f277037d812deb33a0f4a13945d898c296";
            var expectedJsonString = $"{{\"test\":\"{expectedPointString}\"}}";
            var expectedJsonObj = new TestJson() { Test = ECPoint.Parse(expectedPointString, ECCurve.Secp256r1), };

            var actualJsonString = JsonSerializer.Serialize(expectedJsonObj, _options);

            Assert.AreEqual(expectedJsonString, actualJsonString);
        }
    }
}
