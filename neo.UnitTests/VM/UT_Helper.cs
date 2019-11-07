using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.SmartContract;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Neo.UnitTests.IO
{
    [TestClass]
    public class UT_Helper
    {
        [TestMethod]
        public void TestEmit()
        {
            ScriptBuilder sb = new ScriptBuilder();
            sb.Emit(new OpCode[] { OpCode.PUSH0 });
            Assert.AreEqual(Encoding.Default.GetString(new byte[] { 0x00 }), Encoding.Default.GetString(sb.ToArray()));
        }

        [TestMethod]
        public void TestEmitAppCall1()
        {
            //format:(byte)0x00+(byte)OpCode.NEWARRAY+(string)operation+(Uint160)scriptHash+(uint)InteropService.System_Contract_Call
            ScriptBuilder sb = new ScriptBuilder();
            sb.EmitAppCall(UInt160.Zero, "AAAAA");
            byte[] tempArray = new byte[34];
            tempArray[0] = 0x00;//0
            tempArray[1] = 0xC5;//OpCode.NEWARRAY 
            tempArray[2] = 5;//operation.Length
            Array.Copy(Encoding.UTF8.GetBytes("AAAAA"), 0, tempArray, 3, 5);//operation.data
            tempArray[8] = 0x14;//scriptHash.Length
            Array.Copy(UInt160.Zero.ToArray(), 0, tempArray, 9, 20);//operation.data
            uint api = InteropService.System_Contract_Call;
            tempArray[29] = 0x68;//OpCode.SYSCALL
            Array.Copy(BitConverter.GetBytes(api), 0, tempArray, 30, 4);//api.data
            byte[] resultArray = sb.ToArray();
            Assert.AreEqual(Encoding.Default.GetString(tempArray), Encoding.Default.GetString(resultArray));
        }

        [TestMethod]
        public void TestEmitAppCall2()
        {
            //format:(ContractParameter[])ContractParameter+(byte)OpCode.PACK+(string)operation+(Uint160)scriptHash+(uint)InteropService.System_Contract_Call
            ScriptBuilder sb = new ScriptBuilder();
            sb.EmitAppCall(UInt160.Zero, "AAAAA", new ContractParameter[] { new ContractParameter(ContractParameterType.Integer) });
            byte[] tempArray = new byte[35];
            tempArray[0] = 0x00;//0
            tempArray[1] = 0x51;//ContractParameter.Length 
            tempArray[2] = 0xC1;//OpCode.PACK
            tempArray[3] = 0x05;//operation.Length
            Array.Copy(Encoding.UTF8.GetBytes("AAAAA"), 0, tempArray, 4, 5);//operation.data
            tempArray[9] = 0x14;//scriptHash.Length
            Array.Copy(UInt160.Zero.ToArray(), 0, tempArray, 10, 20);//operation.data
            uint api = InteropService.System_Contract_Call;
            tempArray[30] = 0x68;//OpCode.SYSCALL
            Array.Copy(BitConverter.GetBytes(api), 0, tempArray, 31, 4);//api.data
            byte[] resultArray = sb.ToArray();
            Assert.AreEqual(Encoding.Default.GetString(tempArray), Encoding.Default.GetString(resultArray));
        }

        [TestMethod]
        public void TestEmitAppCall3()
        {
            //format:(object[])args+(byte)OpCode.PACK+(string)operation+(Uint160)scriptHash+(uint)InteropService.System_Contract_Call
            ScriptBuilder sb = new ScriptBuilder();
            sb.EmitAppCall(UInt160.Zero, "AAAAA", true);
            byte[] tempArray = new byte[35];
            tempArray[0] = 0x51;//arg
            tempArray[1] = 0x51;//args.Length 
            tempArray[2] = 0xC1;//OpCode.PACK
            tempArray[3] = 0x05;//operation.Length
            Array.Copy(Encoding.UTF8.GetBytes("AAAAA"), 0, tempArray, 4, 5);//operation.data
            tempArray[9] = 0x14;//scriptHash.Length
            Array.Copy(UInt160.Zero.ToArray(), 0, tempArray, 10, 20);//operation.data
            uint api = InteropService.System_Contract_Call;
            tempArray[30] = 0x68;//OpCode.SYSCALL
            Array.Copy(BitConverter.GetBytes(api), 0, tempArray, 31, 4);//api.data
            byte[] resultArray = sb.ToArray();
            Assert.AreEqual(Encoding.Default.GetString(tempArray), Encoding.Default.GetString(resultArray));
        }

        [TestMethod]
        public void TestToParameter()
        {
            StackItem byteItem = "00e057eb481b".HexToBytes();
            Assert.AreEqual(30000000000000L, (long)new BigInteger(byteItem.ToParameter().Value as byte[]));

            StackItem boolItem = false;
            Assert.AreEqual(false, (bool)boolItem.ToParameter().Value);

            StackItem intItem = new BigInteger(1000);
            Assert.AreEqual(1000, (BigInteger)intItem.ToParameter().Value);

            StackItem interopItem = new VM.Types.InteropInterface<string>("test");
            Assert.AreEqual(null, interopItem.ToParameter().Value);

            StackItem arrayItem = new VM.Types.Array(new[] { byteItem, boolItem, intItem, interopItem });
            Assert.AreEqual(1000, (BigInteger)(arrayItem.ToParameter().Value as List<ContractParameter>)[2].Value);

            StackItem mapItem = new VM.Types.Map(new Dictionary<StackItem, StackItem>(new[] { new KeyValuePair<StackItem, StackItem>(byteItem, intItem) }));
            Assert.AreEqual(1000, (BigInteger)(mapItem.ToParameter().Value as List<KeyValuePair<ContractParameter, ContractParameter>>)[0].Value.Value);
        }

        [TestMethod]
        public void TestToStackItem()
        {
            ContractParameter byteParameter = new ContractParameter { Type = ContractParameterType.ByteArray, Value = "00e057eb481b".HexToBytes() };
            Assert.AreEqual(30000000000000L, (long)byteParameter.ToStackItem().GetBigInteger());

            ContractParameter boolParameter = new ContractParameter { Type = ContractParameterType.Boolean, Value = false };
            Assert.AreEqual(false, boolParameter.ToStackItem().GetBoolean());

            ContractParameter intParameter = new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(1000) };
            Assert.AreEqual(1000, intParameter.ToStackItem().GetBigInteger());

            ContractParameter h160Parameter = new ContractParameter { Type = ContractParameterType.Hash160, Value = UInt160.Zero };
            Assert.AreEqual(0, h160Parameter.ToStackItem().GetBigInteger());

            ContractParameter h256Parameter = new ContractParameter { Type = ContractParameterType.Hash256, Value = UInt256.Zero };
            Assert.AreEqual(0, h256Parameter.ToStackItem().GetBigInteger());

            ContractParameter pkParameter = new ContractParameter { Type = ContractParameterType.PublicKey, Value = ECPoint.Parse("02f9ec1fd0a98796cf75b586772a4ddd41a0af07a1dbdf86a7238f74fb72503575", ECCurve.Secp256r1) };
            Assert.AreEqual("02f9ec1fd0a98796cf75b586772a4ddd41a0af07a1dbdf86a7238f74fb72503575", pkParameter.ToStackItem().GetByteArray().ToHexString());

            ContractParameter strParameter = new ContractParameter { Type = ContractParameterType.String, Value = "test😂👍" };
            Assert.AreEqual("test😂👍", Encoding.UTF8.GetString(strParameter.ToStackItem().GetByteArray()));

            ContractParameter interopParameter = new ContractParameter { Type = ContractParameterType.InteropInterface };
            Assert.AreEqual(null, interopParameter.ToStackItem());

            ContractParameter arrayParameter = new ContractParameter { Type = ContractParameterType.Array, Value = new[] { byteParameter, boolParameter, intParameter, h160Parameter, h256Parameter, pkParameter, strParameter, interopParameter }.ToList() };
            Assert.AreEqual(1000, ((VM.Types.Array)arrayParameter.ToStackItem())[2].GetBigInteger());

            ContractParameter mapParameter = new ContractParameter { Type = ContractParameterType.Map, Value = new[] { new KeyValuePair<ContractParameter, ContractParameter>(byteParameter, pkParameter) } };
            Assert.AreEqual(30000000000000L, (long)((VM.Types.Map)mapParameter.ToStackItem()).Keys.First().GetBigInteger());
        }

        [TestMethod]
        public void TestEmitPush1()
        {
            ScriptBuilder sb = new ScriptBuilder();
            sb.EmitPush(UInt160.Zero);
            byte[] tempArray = new byte[21];
            tempArray[0] = 0x14;
            Assert.AreEqual(Encoding.Default.GetString(tempArray), Encoding.Default.GetString(sb.ToArray()));
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

            ScriptBuilder sb = new ScriptBuilder();
            Action action = () => sb.EmitPush(new ContractParameter(ContractParameterType.Map));
            action.Should().Throw<ArgumentException>();
        }

        private void TestEmitPush2Array()
        {
            ScriptBuilder sb = new ScriptBuilder();
            ContractParameter parameter = new ContractParameter(ContractParameterType.Array);
            IList<ContractParameter> values = new List<ContractParameter>();
            values.Add(new ContractParameter(ContractParameterType.Integer));
            values.Add(new ContractParameter(ContractParameterType.Integer));
            parameter.Value = values;
            sb.EmitPush(parameter);
            byte[] tempArray = new byte[4];
            tempArray[0] = 0x00;
            tempArray[1] = 0x00;
            tempArray[2] = 0x52;
            tempArray[3] = 0xC1;
            Assert.AreEqual(Encoding.Default.GetString(tempArray), Encoding.Default.GetString(sb.ToArray()));
        }

        private void TestEmitPush2String()
        {
            ScriptBuilder sb = new ScriptBuilder();
            sb.EmitPush(new ContractParameter(ContractParameterType.String));
            byte[] tempArray = new byte[1];
            Assert.AreEqual(Encoding.Default.GetString(tempArray), Encoding.Default.GetString(sb.ToArray()));
        }

        private void TestEmitPush2PublicKey()
        {
            ScriptBuilder sb = new ScriptBuilder();
            sb.EmitPush(new ContractParameter(ContractParameterType.PublicKey));
            byte[] tempArray = new byte[34];
            tempArray[0] = 0x21;
            Array.Copy(ECCurve.Secp256r1.G.EncodePoint(true), 0, tempArray, 1, 33);
            Assert.AreEqual(Encoding.Default.GetString(tempArray), Encoding.Default.GetString(sb.ToArray()));
        }

        private void TestEmitPush2Hash256()
        {
            ScriptBuilder sb = new ScriptBuilder();
            sb.EmitPush(new ContractParameter(ContractParameterType.Hash256));
            byte[] tempArray = new byte[33];
            tempArray[0] = 0x20;
            Assert.AreEqual(Encoding.Default.GetString(tempArray), Encoding.Default.GetString(sb.ToArray()));
        }

        private void TestEmitPush2Hash160()
        {
            ScriptBuilder sb = new ScriptBuilder();
            sb.EmitPush(new ContractParameter(ContractParameterType.Hash160));
            byte[] tempArray = new byte[21];
            tempArray[0] = 0x14;
            Assert.AreEqual(Encoding.Default.GetString(tempArray), Encoding.Default.GetString(sb.ToArray()));
        }

        private void TestEmitPush2BigInteger()
        {
            ScriptBuilder sb = new ScriptBuilder();
            ContractParameter parameter = new ContractParameter(ContractParameterType.Integer)
            {
                Value = BigInteger.Zero
            };
            sb.EmitPush(parameter);
            byte[] tempArray = new byte[1];
            tempArray[0] = 0x00;
            Assert.AreEqual(Encoding.Default.GetString(tempArray), Encoding.Default.GetString(sb.ToArray()));
        }

        private void TestEmitPush2Integer()
        {
            ScriptBuilder sb = new ScriptBuilder();
            ContractParameter parameter = new ContractParameter(ContractParameterType.Integer);
            sb.EmitPush(parameter);
            byte[] tempArray = new byte[1];
            tempArray[0] = 0x00;
            Assert.AreEqual(Encoding.Default.GetString(tempArray), Encoding.Default.GetString(sb.ToArray()));
        }

        private void TestEmitPush2Boolean()
        {
            ScriptBuilder sb = new ScriptBuilder();
            sb.EmitPush(new ContractParameter(ContractParameterType.Boolean));
            byte[] tempArray = new byte[1];
            tempArray[0] = 0x00;
            Assert.AreEqual(Encoding.Default.GetString(tempArray), Encoding.Default.GetString(sb.ToArray()));
        }

        private void TestEmitPush2ByteArray()
        {
            ScriptBuilder sb = new ScriptBuilder();
            sb.EmitPush(new ContractParameter(ContractParameterType.ByteArray));
            byte[] tempArray = new byte[1];
            tempArray[0] = 0x00;
            Assert.AreEqual(Encoding.Default.GetString(tempArray), Encoding.Default.GetString(sb.ToArray()));
        }

        private void TestEmitPush2Signature()
        {
            ScriptBuilder sb = new ScriptBuilder();
            sb.EmitPush(new ContractParameter(ContractParameterType.Signature));
            byte[] tempArray = new byte[65];
            tempArray[0] = 0x40;
            Assert.AreEqual(Encoding.Default.GetString(tempArray), Encoding.Default.GetString(sb.ToArray()));
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
            TestEmitPush3Int();
            TestEmitPush3Uint();
            TestEmitPush3Long();
            TestEmitPush3Ulong();
            TestEmitPush3Enum();

            ScriptBuilder sb = new ScriptBuilder();
            Action action = () => sb.EmitPush(new object());
            action.Should().Throw<ArgumentException>();
        }


        private void TestEmitPush3Enum()
        {
            ScriptBuilder sb = new ScriptBuilder();
            sb.EmitPush(TestEnum.case1);
            byte[] tempArray = new byte[1];
            tempArray[0] = 0x00;
            Assert.AreEqual(Encoding.Default.GetString(tempArray), Encoding.Default.GetString(sb.ToArray()));
        }

        private void TestEmitPush3Ulong()
        {
            ScriptBuilder sb = new ScriptBuilder();
            ulong temp = 0;
            VM.Helper.EmitPush(sb, temp);
            byte[] tempArray = new byte[1];
            tempArray[0] = 0x00;
            Assert.AreEqual(Encoding.Default.GetString(tempArray), Encoding.Default.GetString(sb.ToArray()));
        }

        private void TestEmitPush3Long()
        {
            ScriptBuilder sb = new ScriptBuilder();
            long temp = 0;
            VM.Helper.EmitPush(sb, temp);
            byte[] tempArray = new byte[1];
            tempArray[0] = 0x00;
            Assert.AreEqual(Encoding.Default.GetString(tempArray), Encoding.Default.GetString(sb.ToArray()));
        }

        private void TestEmitPush3Uint()
        {
            ScriptBuilder sb = new ScriptBuilder();
            uint temp = 0;
            VM.Helper.EmitPush(sb, temp);
            byte[] tempArray = new byte[1];
            tempArray[0] = 0x00;
            Assert.AreEqual(Encoding.Default.GetString(tempArray), Encoding.Default.GetString(sb.ToArray()));
        }

        private void TestEmitPush3Int()
        {
            ScriptBuilder sb = new ScriptBuilder();
            int temp = 0;
            VM.Helper.EmitPush(sb, temp);
            byte[] tempArray = new byte[1];
            tempArray[0] = 0x00;
            Assert.AreEqual(Encoding.Default.GetString(tempArray), Encoding.Default.GetString(sb.ToArray()));
        }

        private void TestEmitPush3Ushort()
        {
            ScriptBuilder sb = new ScriptBuilder();
            ushort temp = 0;
            VM.Helper.EmitPush(sb, temp);
            byte[] tempArray = new byte[1];
            tempArray[0] = 0x00;
            Assert.AreEqual(Encoding.Default.GetString(tempArray), Encoding.Default.GetString(sb.ToArray()));
        }

        private void TestEmitPush3Short()
        {
            ScriptBuilder sb = new ScriptBuilder();
            short temp = 0;
            VM.Helper.EmitPush(sb, temp);
            byte[] tempArray = new byte[1];
            tempArray[0] = 0x00;
            Assert.AreEqual(Encoding.Default.GetString(tempArray), Encoding.Default.GetString(sb.ToArray()));
        }

        private void TestEmitPush3Byte()
        {
            ScriptBuilder sb = new ScriptBuilder();
            byte temp = 0;
            VM.Helper.EmitPush(sb, temp);
            byte[] tempArray = new byte[1];
            tempArray[0] = 0x00;
            Assert.AreEqual(Encoding.Default.GetString(tempArray), Encoding.Default.GetString(sb.ToArray()));
        }

        private void TestEmitPush3Sbyte()
        {
            ScriptBuilder sb = new ScriptBuilder();
            sbyte temp = 0;
            VM.Helper.EmitPush(sb, temp);
            byte[] tempArray = new byte[1];
            tempArray[0] = 0x00;
            Assert.AreEqual(Encoding.Default.GetString(tempArray), Encoding.Default.GetString(sb.ToArray()));
        }

        private void TestEmitPush3ISerializable()
        {
            ScriptBuilder sb = new ScriptBuilder();
            sb.EmitPush(UInt160.Zero);
            byte[] tempArray = new byte[21];
            tempArray[0] = 0x14;
            Assert.AreEqual(Encoding.Default.GetString(tempArray), Encoding.Default.GetString(sb.ToArray()));
        }

        private void TestEmitPush3BigInteger()
        {
            ScriptBuilder sb = new ScriptBuilder();
            sb.EmitPush(BigInteger.Zero);
            byte[] tempArray = new byte[1];
            tempArray[0] = 0x00;
            Assert.AreEqual(Encoding.Default.GetString(tempArray), Encoding.Default.GetString(sb.ToArray()));
        }

        private void TestEmitPush3String()
        {
            ScriptBuilder sb = new ScriptBuilder();
            sb.EmitPush("");
            byte[] tempArray = new byte[1];
            tempArray[0] = 0x00;
            Assert.AreEqual(Encoding.Default.GetString(tempArray), Encoding.Default.GetString(sb.ToArray()));
        }

        private void TestEmitPush3ByteArray()
        {
            ScriptBuilder sb = new ScriptBuilder();
            sb.EmitPush(new byte[] { 0x01 });
            byte[] tempArray = new byte[2];
            tempArray[0] = 0x01;
            tempArray[1] = 0x01;
            Assert.AreEqual(Encoding.Default.GetString(tempArray), Encoding.Default.GetString(sb.ToArray()));
        }

        private void TestEmitPush3Bool()
        {
            ScriptBuilder sb = new ScriptBuilder();
            sb.EmitPush(true);
            byte[] tempArray = new byte[1];
            tempArray[0] = 0x51;
            Assert.AreEqual(Encoding.Default.GetString(tempArray), Encoding.Default.GetString(sb.ToArray()));
        }

        [TestMethod]
        public void TestEmitSysCall()
        {
            ScriptBuilder sb = new ScriptBuilder();
            sb.EmitSysCall(0, true);
            byte[] tempArray = new byte[6];
            tempArray[0] = 0x51;
            tempArray[1] = 0x68;
            tempArray[2] = 0x00;
            tempArray[3] = 0x00;
            tempArray[4] = 0x00;
            tempArray[5] = 0x00;
            Assert.AreEqual(Encoding.Default.GetString(tempArray), Encoding.Default.GetString(sb.ToArray()));
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

            Action action = () => VM.Helper.ToParameter(null);
            action.Should().Throw<ArgumentException>();
        }

        private void TestToParameter2InteropInterface()
        {
            StackItem item = new VM.Types.InteropInterface<VM.Types.Boolean>(new VM.Types.Boolean(true));
            ContractParameter parameter = VM.Helper.ToParameter(item);
            Assert.AreEqual(ContractParameterType.InteropInterface, parameter.Type);
        }

        private void TestToParameter2Integer()
        {
            StackItem item = new VM.Types.Integer(0);
            ContractParameter parameter = VM.Helper.ToParameter(item);
            Assert.AreEqual(ContractParameterType.Integer, parameter.Type);
            Assert.AreEqual(BigInteger.Zero, parameter.Value);
        }

        private void TestToParameter2ByteArray()
        {
            StackItem item = new VM.Types.ByteArray(new byte[] { 0x00 });
            ContractParameter parameter = VM.Helper.ToParameter(item);
            Assert.AreEqual(ContractParameterType.ByteArray, parameter.Type);
            Assert.AreEqual(Encoding.Default.GetString(new byte[] { 0x00 }), Encoding.Default.GetString((byte[])parameter.Value));
        }

        private void TestToParameter2VMBoolean()
        {
            StackItem item = new VM.Types.Boolean(true);
            ContractParameter parameter = VM.Helper.ToParameter(item);
            Assert.AreEqual(ContractParameterType.Boolean, parameter.Type);
            Assert.AreEqual(true, parameter.Value);
        }

        private void TestToParameter2Map()
        {
            StackItem item = new VM.Types.Map();
            ContractParameter parameter = VM.Helper.ToParameter(item);
            Assert.AreEqual(ContractParameterType.Map, parameter.Type);
            Assert.AreEqual(0, ((List<KeyValuePair<ContractParameter, ContractParameter>>)parameter.Value).Count);
        }

        private void TestToParaMeter2VMArray()
        {
            VM.Types.Array item = new VM.Types.Array();
            ContractParameter parameter = VM.Helper.ToParameter(item);
            Assert.AreEqual(ContractParameterType.Array, parameter.Type);
            Assert.AreEqual(0, ((List<ContractParameter>)parameter.Value).Count);
        }
    }
}
