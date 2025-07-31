// Copyright (C) 2015-2025 The Neo Project.
//
// UT_MainServiceUnicodeSign.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Json;
using System;
using System.Text;

namespace Neo.CLI.Tests
{
    [TestClass]
    public class UT_MainServiceUnicodeSign
    {
        [TestMethod]
        public void TestPreprocessDataString()
        {
            // Test Unicode string
            var unicodeData = "你好世界 Hello World";
            var result = MainService.PreprocessDataString(unicodeData);
            var expectedBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(unicodeData));
            Assert.AreEqual(expectedBase64, result);

            // Test hex with 0x prefix
            var hexWithPrefix = "0x48656c6c6f";
            result = MainService.PreprocessDataString(hexWithPrefix);
            var expectedFromHex = Convert.ToBase64String(new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f });
            Assert.AreEqual(expectedFromHex, result);

            // Test hex without prefix
            var hexWithoutPrefix = "48656c6c6f";
            result = MainService.PreprocessDataString(hexWithoutPrefix);
            Assert.AreEqual(expectedFromHex, result);

            // Test existing Base64 (should remain unchanged)
            var existingBase64 = "SGVsbG8gV29ybGQ=";
            result = MainService.PreprocessDataString(existingBase64);
            Assert.AreEqual(existingBase64, result);

            // Test empty string
            result = MainService.PreprocessDataString("");
            Assert.AreEqual("", result);

            // Test null
            result = MainService.PreprocessDataString(null!);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestPreprocessSigningJson()
        {
            // Create a JSON object with Unicode values
            var json = JToken.Parse(@"{
                ""type"": ""Neo.Network.P2P.Payloads.Transaction"",
                ""data"": ""AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAFmUJDLobcPtqo9vZKIdjXsd8fVGwEAARI="",
                ""items"": {
                    ""0xbecaad15c0ea585211faf99738a4354014f177f2"": {
                        ""script"": ""某些中文脚本内容"",
                        ""parameters"": [
                            {""type"": ""Signature"", ""value"": ""Unicode签名数据""},
                            {""type"": ""String"", ""value"": ""This should not change""}
                        ],
                        ""signatures"": {
                            ""03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c"": ""另一个Unicode签名""
                        }
                    }
                }
            }") as JObject;

            var result = MainService.PreprocessSigningJson(json!);

            // Check that Unicode script was converted to Base64
            var scriptValue = result["items"]!["0xbecaad15c0ea585211faf99738a4354014f177f2"]!["script"]!.AsString();
            var expectedScript = Convert.ToBase64String(Encoding.UTF8.GetBytes("某些中文脚本内容"));
            Assert.AreEqual(expectedScript, scriptValue);

            // Check that Unicode signature parameter was converted
            var sigParamValue = result["items"]!["0xbecaad15c0ea585211faf99738a4354014f177f2"]!["parameters"]![0]!["value"]!.AsString();
            var expectedSigParam = Convert.ToBase64String(Encoding.UTF8.GetBytes("Unicode签名数据"));
            Assert.AreEqual(expectedSigParam, sigParamValue);

            // Check that String parameter was not changed
            var stringParamValue = result["items"]!["0xbecaad15c0ea585211faf99738a4354014f177f2"]!["parameters"]![1]!["value"]!.AsString();
            Assert.AreEqual("This should not change", stringParamValue);

            // Check that Unicode signature was converted
            var signatureValue = result["items"]!["0xbecaad15c0ea585211faf99738a4354014f177f2"]!["signatures"]!["03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c"]!.AsString();
            var expectedSignature = Convert.ToBase64String(Encoding.UTF8.GetBytes("另一个Unicode签名"));
            Assert.AreEqual(expectedSignature, signatureValue);
        }
    }
}
