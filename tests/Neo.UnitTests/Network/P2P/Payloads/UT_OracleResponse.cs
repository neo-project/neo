// Copyright (C) 2015-2026 The Neo Project.
//
// UT_OracleResponse.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using System;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_OracleResponse
    {
        [TestMethod]
        public void Serialize_Deserialize_Success_WithResult()
        {
            var original = new OracleResponse
            {
                Id = 42,
                Code = OracleResponseCode.Success,
                Result = (byte[])[0x01, 0x02, 0x03]
            };

            Assert.AreEqual(TransactionAttributeType.OracleResponse, original.Type);
            Assert.IsFalse(original.AllowMultiple);
            Assert.AreEqual(1 + sizeof(ulong) + sizeof(OracleResponseCode) + original.Result.GetVarSize(), original.Size);

            var bytes = original.ToArray();
            var reader = new MemoryReader(bytes);
            var clone = (OracleResponse)TransactionAttribute.DeserializeFrom(ref reader);

            Assert.AreEqual(original.Id, clone.Id);
            Assert.AreEqual(original.Code, clone.Code);
            CollectionAssert.AreEqual(original.Result.ToArray(), clone.Result.ToArray());
        }

        [TestMethod]
        public void Serialize_Deserialize_NonSuccess_EmptyResult()
        {
            var original = new OracleResponse
            {
                Id = 7,
                Code = OracleResponseCode.Timeout,
                Result = ReadOnlyMemory<byte>.Empty
            };

            var bytes = original.ToArray();
            var reader = new MemoryReader(bytes);
            var clone = (OracleResponse)TransactionAttribute.DeserializeFrom(ref reader);

            Assert.AreEqual(OracleResponseCode.Timeout, clone.Code);
            Assert.AreEqual(0, clone.Result.Length);
        }

        [TestMethod]
        public void Deserialize_NonSuccess_WithResult_Throws()
        {
            using var ms = new System.IO.MemoryStream();
            using var writer = new System.IO.BinaryWriter(ms);
            writer.Write((byte)TransactionAttributeType.OracleResponse);
            writer.Write(1UL);
            writer.Write((byte)OracleResponseCode.Timeout);
            writer.WriteVarBytes([0x01]);
            writer.Flush();
            var data = ms.ToArray();

            void Act()
            {
                var reader = new MemoryReader(data);
                TransactionAttribute.DeserializeFrom(ref reader);
            }
            Assert.ThrowsExactly<FormatException>(Act);
        }

        [TestMethod]
        public void ToJson_IncludesFields()
        {
            var response = new OracleResponse
            {
                Id = 9,
                Code = OracleResponseCode.Success,
                Result = (byte[])[0x0A, 0x0B]
            };

            var json = response.ToJson();
            Assert.AreEqual(9UL, (ulong)json["id"].GetNumber());
            // Enum may serialize as number or string depending on JToken conversion
            Assert.IsNotNull(json["code"]);
            Assert.AreEqual(Convert.ToBase64String([0x0A, 0x0B]), json["result"].GetString());
        }

        [TestMethod]
        public void FixedScript_IsNonEmpty()
        {
            Assert.IsTrue(OracleResponse.FixedScript.Length > 0);
        }
    }
}
