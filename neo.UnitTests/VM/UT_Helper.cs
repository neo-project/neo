using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using Neo.VM;
using System;
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
            Array.Copy(Encoding.UTF8.GetBytes("AAAAA"),0, tempArray,3, 5);//operation.data
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
            sb.EmitAppCall(UInt160.Zero, "AAAAA",new ContractParameter[] {new ContractParameter(ContractParameterType.Integer)});
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
    }
}