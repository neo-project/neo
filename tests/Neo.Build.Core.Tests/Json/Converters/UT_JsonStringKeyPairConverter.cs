// Copyright (C) 2015-2025 The Neo Project.
//
// UT_JsonStringKeyPairConverter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Cryptography;
using Neo.Build.Core.Json.Converters;
using Neo.Build.Core.Tests.Helpers;
using Neo.Extensions;
using Neo.Wallets;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neo.Build.Core.Tests.Json.Converters
{
    [TestClass]
    public class UT_JsonStringKeyPairConverter
    {
        private class TestJson
        {
            [JsonConverter(typeof(JsonStringKeyPairHexFormatConverter))]
            public KeyPair? Test { get; set; }
        };

        [TestMethod]
        public void TestReadJson()
        {
            var keyPair = KeyPairHelper.CreateNew();
            var expectedBytes = keyPair.PrivateKey!;
            var expectedJsonString = $"{{\"test\":\"{expectedBytes.ToHexString()}\"}}";

            var actualObject = JsonSerializer.Deserialize<TestJson>(expectedJsonString, TestDefaults.JsonDefaultSerializerOptions);
            var actualKeyPair = actualObject!.Test!;

            CollectionAssert.AreEqual(expectedBytes, actualKeyPair.PrivateKey);
        }

        [TestMethod]
        public void TestWriteJson()
        {
            var keyPair = KeyPairHelper.CreateNew();
            var expectedBytes = keyPair.PrivateKey;
            var expectedJsonString = $"{{\"test\":\"{expectedBytes.ToHexString()}\"}}";
            var expectedJsonObj = new TestJson() { Test = keyPair, };

            var actualJsonString = JsonSerializer.Serialize(expectedJsonObj, TestDefaults.JsonDefaultSerializerOptions);

            Assert.AreEqual(expectedJsonString, actualJsonString);
        }
    }
}
