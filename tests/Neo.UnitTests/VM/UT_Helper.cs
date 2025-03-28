// Copyright (C) 2015-2025 The Neo Project.
//
// UT_Helper.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using Array = System.Array;

namespace Neo.UnitTests.VMT
{
    [TestClass]
    public class UT_Helper
    {
        private DataCache _snapshotCache;

        [TestInitialize]
        public void TestSetup()
        {
            _snapshotCache = TestBlockchain.GetTestSnapshotCache();
        }

        [TestMethod]
        public void TestEmit()
        {
            ScriptBuilder sb = new ScriptBuilder();
            sb.Emit([OpCode.PUSH0]);
            CollectionAssert.AreEqual(new[] { (byte)OpCode.PUSH0 }, sb.ToArray());
        }

        [TestMethod]
        public void TestToJson()
        {
            var item = new VM.Types.Array
            {
                5,
                "hello world",
                new byte[] { 1, 2, 3 },
                true
            };

            Assert.AreEqual("{\"type\":\"Integer\",\"value\":\"5\"}", item[0].ToJson().ToString());
            Assert.AreEqual("{\"type\":\"ByteString\",\"value\":\"aGVsbG8gd29ybGQ=\"}", item[1].ToJson().ToString());
            Assert.AreEqual("{\"type\":\"ByteString\",\"value\":\"AQID\"}", item[2].ToJson().ToString());
            Assert.AreEqual("{\"type\":\"Boolean\",\"value\":true}", item[3].ToJson().ToString());
            Assert.AreEqual("{\"type\":\"Array\",\"value\":[{\"type\":\"Integer\",\"value\":\"5\"},{\"type\":\"ByteString\",\"value\":\"aGVsbG8gd29ybGQ=\"},{\"type\":\"ByteString\",\"value\":\"AQID\"},{\"type\":\"Boolean\",\"value\":true}]}", item.ToJson().ToString());

            var item2 = new Map();
            item2[1] = new Pointer(new Script(ReadOnlyMemory<byte>.Empty), 0);

            Assert.AreEqual("{\"type\":\"Map\",\"value\":[{\"key\":{\"type\":\"Integer\",\"value\":\"1\"},\"value\":{\"type\":\"Pointer\",\"value\":0}}]}", item2.ToJson().ToString());
        }

        [TestMethod]
        public void TestEmitAppCall1()
        {
            ScriptBuilder sb = new ScriptBuilder();
            sb.EmitDynamicCall(UInt160.Zero, "AAAAA");
            byte[] tempArray = new byte[36];
            tempArray[0] = (byte)OpCode.NEWARRAY0;
            tempArray[1] = (byte)OpCode.PUSH15;//(byte)CallFlags.All;
            tempArray[2] = (byte)OpCode.PUSHDATA1;
            tempArray[3] = 5;//operation.Length
            Array.Copy(Encoding.UTF8.GetBytes("AAAAA"), 0, tempArray, 4, 5);//operation.data
            tempArray[9] = (byte)OpCode.PUSHDATA1;
            tempArray[10] = 0x14;//scriptHash.Length
            Array.Copy(UInt160.Zero.ToArray(), 0, tempArray, 11, 20);//operation.data
            uint api = ApplicationEngine.System_Contract_Call;
            tempArray[31] = (byte)OpCode.SYSCALL;
            Array.Copy(BitConverter.GetBytes(api), 0, tempArray, 32, 4);//api.data
            Assert.AreEqual(tempArray.ToHexString(), sb.ToArray().ToHexString());
        }

        [TestMethod]
        public void TestEmitArray()
        {
            var snapshot = _snapshotCache.CloneCache();
            var expected = new BigInteger[] { 1, 2, 3 };
            var sb = new ScriptBuilder();
            sb.CreateArray(expected);

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot);
            engine.LoadScript(sb.ToArray());
            Assert.AreEqual(VMState.HALT, engine.Execute());

            CollectionAssert.AreEqual(expected, engine.ResultStack.Pop<VM.Types.Array>().Select(u => u.GetInteger()).ToArray());

            expected = [];
            sb = new ScriptBuilder();
            sb.CreateArray(expected);

            using var engine2 = ApplicationEngine.Create(TriggerType.Application, null, snapshot);
            engine2.LoadScript(sb.ToArray());
            Assert.AreEqual(VMState.HALT, engine2.Execute());

            Assert.AreEqual(0, engine2.ResultStack.Pop<VM.Types.Array>().Count);
        }

        [TestMethod]
        public void TestEmitStruct()
        {
            var snapshot = _snapshotCache.CloneCache();
            var expected = new BigInteger[] { 1, 2, 3 };
            var sb = new ScriptBuilder();
            sb.CreateStruct(expected);

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot);
            engine.LoadScript(sb.ToArray());
            Assert.AreEqual(VMState.HALT, engine.Execute());

            CollectionAssert.AreEqual(expected, engine.ResultStack.Pop<Struct>().Select(u => u.GetInteger()).ToArray());

            expected = [];
            sb = new ScriptBuilder();
            sb.CreateStruct(expected);

            using var engine2 = ApplicationEngine.Create(TriggerType.Application, null, snapshot);
            engine2.LoadScript(sb.ToArray());
            Assert.AreEqual(VMState.HALT, engine2.Execute());

            Assert.AreEqual(0, engine2.ResultStack.Pop<Struct>().Count);
        }

        [TestMethod]
        public void TestEmitMap()
        {
            var snapshot = _snapshotCache.CloneCache();
            var expected = new Dictionary<BigInteger, BigInteger>() { { 1, 2 }, { 3, 4 } };
            var sb = new ScriptBuilder();
            sb.CreateMap(expected);

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot);
            engine.LoadScript(sb.ToArray());
            Assert.AreEqual(VMState.HALT, engine.Execute());

            var map = engine.ResultStack.Pop<Map>();
            var dic = map.ToDictionary(u => u.Key, u => u.Value);

            CollectionAssert.AreEqual(expected.Keys, dic.Keys.Select(u => u.GetInteger()).ToArray());
            CollectionAssert.AreEqual(expected.Values, dic.Values.Select(u => u.GetInteger()).ToArray());
        }

        [TestMethod]
        public void TestEmitAppCall2()
        {
            ScriptBuilder sb = new ScriptBuilder();
            sb.EmitDynamicCall(UInt160.Zero, "AAAAA", [new ContractParameter(ContractParameterType.Integer)]);
            byte[] tempArray = new byte[38];
            tempArray[0] = (byte)OpCode.PUSH0;
            tempArray[1] = (byte)OpCode.PUSH1;
            tempArray[2] = (byte)OpCode.PACK;
            tempArray[3] = (byte)OpCode.PUSH15;//(byte)CallFlags.All;
            tempArray[4] = (byte)OpCode.PUSHDATA1;
            tempArray[5] = 0x05;//operation.Length
            Array.Copy(Encoding.UTF8.GetBytes("AAAAA"), 0, tempArray, 6, 5);//operation.data
            tempArray[11] = (byte)OpCode.PUSHDATA1;
            tempArray[12] = 0x14;//scriptHash.Length
            Array.Copy(UInt160.Zero.ToArray(), 0, tempArray, 13, 20);//operation.data
            uint api = ApplicationEngine.System_Contract_Call;
            tempArray[33] = (byte)OpCode.SYSCALL;
            Array.Copy(BitConverter.GetBytes(api), 0, tempArray, 34, 4);//api.data
            Assert.AreEqual(tempArray.ToHexString(), sb.ToArray().ToHexString());
        }

        [TestMethod]
        public void TestEmitAppCall3()
        {
            ScriptBuilder sb = new ScriptBuilder();
            sb.EmitDynamicCall(UInt160.Zero, "AAAAA", true);
            byte[] tempArray = new byte[38];
            tempArray[0] = (byte)OpCode.PUSHT;
            tempArray[1] = (byte)OpCode.PUSH1;//arg.Length
            tempArray[2] = (byte)OpCode.PACK;
            tempArray[3] = (byte)OpCode.PUSH15;//(byte)CallFlags.All;
            tempArray[4] = (byte)OpCode.PUSHDATA1;
            tempArray[5] = 0x05;//operation.Length
            Array.Copy(Encoding.UTF8.GetBytes("AAAAA"), 0, tempArray, 6, 5);//operation.data
            tempArray[11] = (byte)OpCode.PUSHDATA1;
            tempArray[12] = 0x14;//scriptHash.Length
            Array.Copy(UInt160.Zero.ToArray(), 0, tempArray, 13, 20);//operation.data
            uint api = ApplicationEngine.System_Contract_Call;
            tempArray[33] = (byte)OpCode.SYSCALL;
            Array.Copy(BitConverter.GetBytes(api), 0, tempArray, 34, 4);//api.data
            Assert.AreEqual(tempArray.ToHexString(), sb.ToArray().ToHexString());
        }

        [TestMethod]
        public void TestMakeScript()
        {
            byte[] testScript = NativeContract.GAS.Hash.MakeScript("balanceOf", UInt160.Zero);

            Assert.AreEqual("0c14000000000000000000000000000000000000000011c01f0c0962616c616e63654f660c14cf76e28bd0062c4a478ee35561011319f3cfa4d241627d5b52",
                            testScript.ToHexString());
        }

        [TestMethod]
        public void TestToParameter()
        {
            StackItem byteItem = "00e057eb481b".HexToBytes();
            Assert.AreEqual(30000000000000L, (long)new BigInteger(byteItem.ToParameter().Value as byte[]));

            StackItem boolItem = false;
            Assert.IsFalse((bool)boolItem.ToParameter().Value);

            StackItem intItem = new BigInteger(1000);
            Assert.AreEqual(1000, (BigInteger)intItem.ToParameter().Value);

            StackItem interopItem = new InteropInterface("test");
            Assert.AreEqual(ContractParameterType.InteropInterface, interopItem.ToParameter().Type);

            StackItem arrayItem = new VM.Types.Array(new[] { byteItem, boolItem, intItem, interopItem });
            Assert.AreEqual(1000, (BigInteger)(arrayItem.ToParameter().Value as List<ContractParameter>)[2].Value);

            StackItem mapItem = new Map { [(PrimitiveType)byteItem] = intItem };
            Assert.AreEqual(1000, (BigInteger)(mapItem.ToParameter().Value as List<KeyValuePair<ContractParameter, ContractParameter>>)[0].Value.Value);
        }

        [TestMethod]
        public void TestToStackItem()
        {
            ContractParameter parameter = null;
            Assert.ThrowsExactly<ArgumentNullException>(() => _ = parameter.ToStackItem());

            ContractParameter byteParameter = new ContractParameter { Type = ContractParameterType.ByteArray, Value = "00e057eb481b".HexToBytes() };
            Assert.AreEqual(30000000000000L, (long)byteParameter.ToStackItem().GetInteger());

            ContractParameter boolParameter = new ContractParameter { Type = ContractParameterType.Boolean, Value = false };
            Assert.IsFalse(boolParameter.ToStackItem().GetBoolean());

            ContractParameter intParameter = new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(1000) };
            Assert.AreEqual(1000, intParameter.ToStackItem().GetInteger());

            ContractParameter h160Parameter = new ContractParameter { Type = ContractParameterType.Hash160, Value = UInt160.Zero };
            Assert.AreEqual(0, h160Parameter.ToStackItem().GetInteger());

            ContractParameter h256Parameter = new ContractParameter { Type = ContractParameterType.Hash256, Value = UInt256.Zero };
            Assert.AreEqual(0, h256Parameter.ToStackItem().GetInteger());

            ContractParameter pkParameter = new ContractParameter
            {
                Type = ContractParameterType.PublicKey,
                Value = ECPoint.Parse("02f9ec1fd0a98796cf75b586772a4ddd41a0af07a1dbdf86a7238f74fb72503575", ECCurve.Secp256r1)
            };
            Assert.IsInstanceOfType(pkParameter.ToStackItem(), typeof(ByteString));
            Assert.AreEqual("02f9ec1fd0a98796cf75b586772a4ddd41a0af07a1dbdf86a7238f74fb72503575", pkParameter.ToStackItem().GetSpan().ToHexString());

            ContractParameter strParameter = new ContractParameter { Type = ContractParameterType.String, Value = "test😂👍" };
            Assert.AreEqual("test😂👍", strParameter.ToStackItem().GetString());

            ContractParameter interopParameter = new ContractParameter { Type = ContractParameterType.InteropInterface, Value = new object() };
            Assert.ThrowsExactly<ArgumentException>(() => _ = interopParameter.ToStackItem());

            ContractParameter interopParameter2 = new ContractParameter { Type = ContractParameterType.InteropInterface };
            Assert.AreEqual(StackItem.Null, interopParameter2.ToStackItem());

            ContractParameter arrayParameter = new ContractParameter
            {
                Type = ContractParameterType.Array,
                Value = new[] { byteParameter, boolParameter, intParameter, h160Parameter, h256Parameter, pkParameter, strParameter }.ToList()
            };
            Assert.AreEqual(1000, ((VM.Types.Array)arrayParameter.ToStackItem())[2].GetInteger());

            ContractParameter mapParameter = new ContractParameter
            {
                Type = ContractParameterType.Map,
                Value = new[] { new KeyValuePair<ContractParameter, ContractParameter>(byteParameter, pkParameter) }
            };
            Assert.AreEqual(30000000000000L, (long)((Map)mapParameter.ToStackItem()).Keys.First().GetInteger());
        }

        [TestMethod]
        public void TestEmitPush1()
        {
            ScriptBuilder sb = new ScriptBuilder();
            sb.EmitPush(UInt160.Zero);
            byte[] tempArray = new byte[22];
            tempArray[0] = (byte)OpCode.PUSHDATA1;
            tempArray[1] = 0x14;
            CollectionAssert.AreEqual(tempArray, sb.ToArray());
        }

        [TestMethod]
        public void TestEmitPush2()
        {
            TestEmitPush2Signature();
            TestEmitPush2ByteArray();
            TestEmitPush2Boolean();
            TestEmitPush2Integer();
            TestEmitPush2BigInteger();
            TestEmitPush2Hash160();
            TestEmitPush2Hash256();
            TestEmitPush2PublicKey();
            TestEmitPush2String();
            TestEmitPush2Array();
            TestEmitPush2Map();
        }

        private void TestEmitPush2Map()
        {
            ScriptBuilder sb = new ScriptBuilder();
            sb.EmitPush(new ContractParameter(ContractParameterType.Map));
            CollectionAssert.AreEqual(new[] { (byte)OpCode.NEWMAP }, sb.ToArray());
        }

        private void TestEmitPush2Array()
        {
            ScriptBuilder sb = new ScriptBuilder();
            ContractParameter parameter = new ContractParameter(ContractParameterType.Array);
            IList<ContractParameter> values = new List<ContractParameter>
            {
                new ContractParameter(ContractParameterType.Integer),
                new ContractParameter(ContractParameterType.Integer)
            };
            parameter.Value = values;
            sb.EmitPush(parameter);
            byte[] tempArray =
            [
                (byte)OpCode.PUSH0,
                (byte)OpCode.PUSH0,
                (byte)OpCode.PUSH2,
                (byte)OpCode.PACK,
            ];
            CollectionAssert.AreEqual(tempArray, sb.ToArray());
        }

        private void TestEmitPush2String()
        {
            ScriptBuilder sb = new ScriptBuilder();
            sb.EmitPush(new ContractParameter(ContractParameterType.String));
            byte[] tempArray = [(byte)OpCode.PUSHDATA1, 0x00];
            CollectionAssert.AreEqual(tempArray, sb.ToArray());
        }

        private void TestEmitPush2PublicKey()
        {
            ScriptBuilder sb = new ScriptBuilder();
            sb.EmitPush(new ContractParameter(ContractParameterType.PublicKey));
            byte[] tempArray = new byte[35];
            tempArray[0] = (byte)OpCode.PUSHDATA1;
            tempArray[1] = 0x21;
            Array.Copy(ECCurve.Secp256r1.G.EncodePoint(true), 0, tempArray, 2, 33);
            CollectionAssert.AreEqual(tempArray, sb.ToArray());
        }

        private void TestEmitPush2Hash256()
        {
            ScriptBuilder sb = new ScriptBuilder();
            sb.EmitPush(new ContractParameter(ContractParameterType.Hash256));
            byte[] tempArray = new byte[34];
            tempArray[0] = (byte)OpCode.PUSHDATA1;
            tempArray[1] = 0x20;
            CollectionAssert.AreEqual(tempArray, sb.ToArray());
        }

        private void TestEmitPush2Hash160()
        {
            ScriptBuilder sb = new ScriptBuilder();
            sb.EmitPush(new ContractParameter(ContractParameterType.Hash160));
            byte[] tempArray = new byte[22];
            tempArray[0] = (byte)OpCode.PUSHDATA1;
            tempArray[1] = 0x14;
            CollectionAssert.AreEqual(tempArray, sb.ToArray());
        }

        private void TestEmitPush2BigInteger()
        {
            ScriptBuilder sb = new ScriptBuilder();
            ContractParameter parameter = new ContractParameter(ContractParameterType.Integer)
            {
                Value = BigInteger.Zero
            };
            sb.EmitPush(parameter);
            byte[] tempArray = [(byte)OpCode.PUSH0];
            CollectionAssert.AreEqual(tempArray, sb.ToArray());
        }

        private void TestEmitPush2Integer()
        {
            ScriptBuilder sb = new ScriptBuilder();
            ContractParameter parameter = new ContractParameter(ContractParameterType.Integer);
            sb.EmitPush(parameter);
            byte[] tempArray = [(byte)OpCode.PUSH0];
            CollectionAssert.AreEqual(tempArray, sb.ToArray());
        }

        private void TestEmitPush2Boolean()
        {
            ScriptBuilder sb = new ScriptBuilder();
            sb.EmitPush(new ContractParameter(ContractParameterType.Boolean));
            byte[] tempArray = [(byte)OpCode.PUSHF];
            CollectionAssert.AreEqual(tempArray, sb.ToArray());
        }

        private void TestEmitPush2ByteArray()
        {
            ScriptBuilder sb = new ScriptBuilder();
            sb.EmitPush(new ContractParameter(ContractParameterType.ByteArray));
            byte[] tempArray = [(byte)OpCode.PUSHDATA1, 0x00];
            CollectionAssert.AreEqual(tempArray, sb.ToArray());
        }

        private void TestEmitPush2Signature()
        {
            ScriptBuilder sb = new ScriptBuilder();
            sb.EmitPush(new ContractParameter(ContractParameterType.Signature));
            byte[] tempArray = new byte[66];
            tempArray[0] = (byte)OpCode.PUSHDATA1;
            tempArray[1] = 0x40;
            CollectionAssert.AreEqual(tempArray, sb.ToArray());
        }

        enum TestEnum : byte
        {
            case1 = 0
        }

        [TestMethod]
        public void TestEmitPush3()
        {
            TestEmitPush3Bool();
            TestEmitPush3ByteArray();
            TestEmitPush3String();
            TestEmitPush3BigInteger();
            TestEmitPush3ISerializable();
            TestEmitPush3Sbyte();
            TestEmitPush3Byte();
            TestEmitPush3Short();
            TestEmitPush3Ushort();
            TestEmitPush3Char();
            TestEmitPush3Int();
            TestEmitPush3Uint();
            TestEmitPush3Long();
            TestEmitPush3Ulong();
            TestEmitPush3Enum();

            ScriptBuilder sb = new ScriptBuilder();
            Action action = () => sb.EmitPush(new object());
            Assert.ThrowsExactly<ArgumentException>(() => action());
        }


        private void TestEmitPush3Enum()
        {
            ScriptBuilder sb = new ScriptBuilder();
            sb.EmitPush(TestEnum.case1);
            byte[] tempArray = [(byte)OpCode.PUSH0];
            CollectionAssert.AreEqual(tempArray, sb.ToArray());
        }

        private void TestEmitPush3Ulong()
        {
            ScriptBuilder sb = new ScriptBuilder();
            ulong temp = 0;
            sb.EmitPush(temp);
            byte[] tempArray = [(byte)OpCode.PUSH0];
            CollectionAssert.AreEqual(tempArray, sb.ToArray());
        }

        private void TestEmitPush3Long()
        {
            ScriptBuilder sb = new ScriptBuilder();
            long temp = 0;
            sb.EmitPush(temp);
            byte[] tempArray = [(byte)OpCode.PUSH0];
            CollectionAssert.AreEqual(tempArray, sb.ToArray());
        }

        private void TestEmitPush3Uint()
        {
            ScriptBuilder sb = new ScriptBuilder();
            uint temp = 0;
            sb.EmitPush(temp);
            byte[] tempArray = [(byte)OpCode.PUSH0];
            CollectionAssert.AreEqual(tempArray, sb.ToArray());
        }

        private void TestEmitPush3Int()
        {
            ScriptBuilder sb = new ScriptBuilder();
            int temp = 0;
            sb.EmitPush(temp);
            byte[] tempArray = [(byte)OpCode.PUSH0];
            CollectionAssert.AreEqual(tempArray, sb.ToArray());
        }

        private void TestEmitPush3Ushort()
        {
            ScriptBuilder sb = new ScriptBuilder();
            ushort temp = 0;
            sb.EmitPush(temp);
            byte[] tempArray = [(byte)OpCode.PUSH0];
            CollectionAssert.AreEqual(tempArray, sb.ToArray());
        }

        private void TestEmitPush3Char()
        {
            ScriptBuilder sb = new ScriptBuilder();
            char temp = char.MinValue;
            sb.EmitPush(temp);
            byte[] tempArray = [(byte)OpCode.PUSH0];
            CollectionAssert.AreEqual(tempArray, sb.ToArray());
        }

        private void TestEmitPush3Short()
        {
            ScriptBuilder sb = new ScriptBuilder();
            short temp = 0;
            sb.EmitPush(temp);
            byte[] tempArray = [(byte)OpCode.PUSH0];
            CollectionAssert.AreEqual(tempArray, sb.ToArray());
        }

        private void TestEmitPush3Byte()
        {
            ScriptBuilder sb = new ScriptBuilder();
            byte temp = 0;
            sb.EmitPush(temp);
            byte[] tempArray = [(byte)OpCode.PUSH0];
            CollectionAssert.AreEqual(tempArray, sb.ToArray());
        }

        private void TestEmitPush3Sbyte()
        {
            ScriptBuilder sb = new ScriptBuilder();
            sbyte temp = 0;
            sb.EmitPush(temp);
            byte[] tempArray = [(byte)OpCode.PUSH0];
            CollectionAssert.AreEqual(tempArray, sb.ToArray());
        }

        private void TestEmitPush3ISerializable()
        {
            ScriptBuilder sb = new ScriptBuilder();
            sb.EmitPush(UInt160.Zero);
            var tempArray = new byte[22];
            tempArray[0] = (byte)OpCode.PUSHDATA1;
            tempArray[1] = 0x14;
            CollectionAssert.AreEqual(tempArray, sb.ToArray());
        }

        private void TestEmitPush3BigInteger()
        {
            ScriptBuilder sb = new ScriptBuilder();
            sb.EmitPush(BigInteger.Zero);
            byte[] tempArray = [(byte)OpCode.PUSH0];
            CollectionAssert.AreEqual(tempArray, sb.ToArray());
        }

        private void TestEmitPush3String()
        {
            ScriptBuilder sb = new ScriptBuilder();
            sb.EmitPush("");
            byte[] tempArray = [(byte)OpCode.PUSHDATA1, 0x00];
            CollectionAssert.AreEqual(tempArray, sb.ToArray());
        }

        private void TestEmitPush3ByteArray()
        {
            ScriptBuilder sb = new ScriptBuilder();
            sb.EmitPush([0x01]);
            byte[] tempArray = [(byte)OpCode.PUSHDATA1, 0x01, 0x01];
            CollectionAssert.AreEqual(tempArray, sb.ToArray());
        }

        private void TestEmitPush3Bool()
        {
            ScriptBuilder sb = new ScriptBuilder();
            sb.EmitPush(true);
            byte[] tempArray = [(byte)OpCode.PUSHT];
            CollectionAssert.AreEqual(tempArray, sb.ToArray());
        }

        [TestMethod]
        public void TestEmitSysCall()
        {
            ScriptBuilder sb = new ScriptBuilder();
            sb.EmitSysCall(0, true);
            byte[] tempArray =
            [
                (byte)OpCode.PUSHT,
                (byte)OpCode.SYSCALL,
                0x00,
                0x00,
                0x00,
                0x00,
            ];
            CollectionAssert.AreEqual(tempArray, sb.ToArray());
        }

        [TestMethod]
        public void TestToParameter2()
        {
            TestToParaMeter2VMArray();
            TestToParameter2Map();
            TestToParameter2VMBoolean();
            TestToParameter2ByteArray();
            TestToParameter2Integer();
            TestToParameter2InteropInterface();
            TestToParameterNull();
        }

        private void TestToParameterNull()
        {
            StackItem item = null;
            Assert.ThrowsExactly<ArgumentNullException>(() => _ = item.ToParameter());
        }

        private void TestToParameter2InteropInterface()
        {
            StackItem item = new InteropInterface(new object());
            ContractParameter parameter = item.ToParameter();
            Assert.AreEqual(ContractParameterType.InteropInterface, parameter.Type);
        }

        private void TestToParameter2Integer()
        {
            StackItem item = new Integer(0);
            ContractParameter parameter = item.ToParameter();
            Assert.AreEqual(ContractParameterType.Integer, parameter.Type);
            Assert.AreEqual(BigInteger.Zero, parameter.Value);
        }

        private void TestToParameter2ByteArray()
        {
            StackItem item = new ByteString(new byte[] { 0x00 });
            ContractParameter parameter = item.ToParameter();
            Assert.AreEqual(ContractParameterType.ByteArray, parameter.Type);
            Assert.AreEqual(Encoding.Default.GetString([0x00]), Encoding.Default.GetString((byte[])parameter.Value));
        }

        private void TestToParameter2VMBoolean()
        {
            StackItem item = StackItem.True;
            ContractParameter parameter = item.ToParameter();
            Assert.AreEqual(ContractParameterType.Boolean, parameter.Type);
            Assert.IsTrue((bool?)parameter.Value);
        }

        private void TestToParameter2Map()
        {
            StackItem item = new Map();
            ContractParameter parameter = item.ToParameter();
            Assert.AreEqual(ContractParameterType.Map, parameter.Type);
            Assert.AreEqual(0, ((List<KeyValuePair<ContractParameter, ContractParameter>>)parameter.Value).Count);
        }

        private void TestToParaMeter2VMArray()
        {
            VM.Types.Array item = new VM.Types.Array();
            ContractParameter parameter = item.ToParameter();
            Assert.AreEqual(ContractParameterType.Array, parameter.Type);
            Assert.AreEqual(0, ((List<ContractParameter>)parameter.Value).Count);
        }

        [TestMethod]
        public void TestCharAsUInt16()
        {
            Assert.AreEqual(ushort.MaxValue, char.MaxValue);
            Assert.AreEqual(ushort.MinValue, char.MinValue);

            // test every char in a loop
            for (int i = ushort.MinValue; i < char.MinValue; i++)
            {
                var c = Convert.ToChar(i);
                Assert.AreEqual(i, c);
            }

            for (int i = ushort.MinValue; i < ushort.MaxValue; i++)
            {
                using var sbUInt16 = new ScriptBuilder();
                using var sbChar = new ScriptBuilder();
                sbUInt16.EmitPush((ushort)i);
                sbChar.EmitPush(Convert.ToChar(i));
                CollectionAssert.AreEqual(sbUInt16.ToArray(), sbChar.ToArray());
            }
        }

        [TestMethod]
        public void TestCyclicReference()
        {
            var map = new Map { [1] = 2 };
            var item = new VM.Types.Array { map, map };

            // just check there is no exception
            var expected = """
            {
                "type":"Array",
                "value":[
                {
                    "type":"Map",
                    "value":[{
                        "key":{"type":"Integer","value":"1"},
                        "value":{"type":"Integer","value":"2"}
                    }]
                },{
                    "type":"Map",
                    "value":[{
                        "key":{"type":"Integer","value":"1"},
                        "value":{"type":"Integer","value":"2"}
                    }]
                }]
            }
            """;

            var json = item.ToJson();
            Assert.AreEqual(Regex.Replace(expected, @"\s+", ""), json.ToString());
            // check cyclic reference
            map[2] = item;
            var action = () => item.ToJson();
            Assert.ThrowsExactly<InvalidOperationException>(() => _ = action());
        }
    }
}
