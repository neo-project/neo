using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_StackItemSerializer
    {
        private const int MaxItemSize = 1024 * 1024;

        [TestMethod]
        public void TestSerialize()
        {
            byte[] result1 = StackItemSerializer.Serialize(new byte[5], MaxItemSize);
            byte[] expectedArray1 = new byte[] {
                        0x00, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00
                    };
            Assert.AreEqual(Encoding.Default.GetString(expectedArray1), Encoding.Default.GetString(result1));

            byte[] result2 = StackItemSerializer.Serialize(true, MaxItemSize);
            byte[] expectedArray2 = new byte[] {
                        0x01, 0x01
                    };
            Assert.AreEqual(Encoding.Default.GetString(expectedArray2), Encoding.Default.GetString(result2));

            byte[] result3 = StackItemSerializer.Serialize(1, MaxItemSize);
            byte[] expectedArray3 = new byte[] {
                        0x02, 0x01, 0x01
                    };
            Assert.AreEqual(Encoding.Default.GetString(expectedArray3), Encoding.Default.GetString(result3));

            StackItem stackItem4 = new InteropInterface<object>(new object());
            Action action4 = () => StackItemSerializer.Serialize(stackItem4, MaxItemSize);
            action4.Should().Throw<NotSupportedException>();

            byte[] result5 = StackItemSerializer.Serialize(1, MaxItemSize);
            byte[] expectedArray5 = new byte[] {
                        0x02, 0x01, 0x01
                    };
            Assert.AreEqual(Encoding.Default.GetString(expectedArray5), Encoding.Default.GetString(result5));


            List<StackItem> list6 = new List<StackItem> { 1 };
            StackItem stackItem62 = new VM.Types.Array(list6);
            byte[] result6 = StackItemSerializer.Serialize(stackItem62, MaxItemSize);
            byte[] expectedArray6 = new byte[] {
                        0x80,0x01,0x02,0x01,0x01
                    };
            Assert.AreEqual(Encoding.Default.GetString(expectedArray6), Encoding.Default.GetString(result6));

            List<StackItem> list7 = new List<StackItem> { 1 };
            StackItem stackItem72 = new Struct(list7);
            byte[] result7 = StackItemSerializer.Serialize(stackItem72, MaxItemSize);
            byte[] expectedArray7 = new byte[] {
                        0x81,0x01,0x02,0x01,0x01
                    };
            Assert.AreEqual(Encoding.Default.GetString(expectedArray7), Encoding.Default.GetString(result7));

            Dictionary<PrimitiveType, StackItem> list8 = new Dictionary<PrimitiveType, StackItem> { [2] = 1 };
            StackItem stackItem82 = new Map(list8);
            byte[] result8 = StackItemSerializer.Serialize(stackItem82, MaxItemSize);
            byte[] expectedArray8 = new byte[] {
                        0x82,0x01,0x02,0x01,0x02,0x02,0x01,0x01
                    };
            Assert.AreEqual(Encoding.Default.GetString(expectedArray8), Encoding.Default.GetString(result8));

            Map stackItem91 = new Map();
            stackItem91[1] = stackItem91;
            Action action9 = () => StackItemSerializer.Serialize(stackItem91, MaxItemSize);
            action9.Should().Throw<NotSupportedException>();

            VM.Types.Array stackItem10 = new VM.Types.Array();
            stackItem10.Add(stackItem10);
            Action action10 = () => StackItemSerializer.Serialize(stackItem10, MaxItemSize);
            action10.Should().Throw<NotSupportedException>();
        }

        [TestMethod]
        public void TestDeserializeStackItem()
        {
            StackItem stackItem1 = new ByteArray(new byte[5]);
            byte[] byteArray1 = StackItemSerializer.Serialize(stackItem1, MaxItemSize);
            StackItem result1 = StackItemSerializer.Deserialize(byteArray1, (uint)byteArray1.Length);
            Assert.AreEqual(stackItem1, result1);

            StackItem stackItem2 = new VM.Types.Boolean(true);
            byte[] byteArray2 = StackItemSerializer.Serialize(stackItem2, MaxItemSize);
            StackItem result2 = StackItemSerializer.Deserialize(byteArray2, (uint)byteArray2.Length);
            Assert.AreEqual(stackItem2, result2);

            StackItem stackItem3 = new Integer(1);
            byte[] byteArray3 = StackItemSerializer.Serialize(stackItem3, MaxItemSize);
            StackItem result3 = StackItemSerializer.Deserialize(byteArray3, (uint)byteArray3.Length);
            Assert.AreEqual(stackItem3, result3);

            byte[] byteArray4 = StackItemSerializer.Serialize(1, MaxItemSize);
            byteArray4[0] = 0x40;
            Action action4 = () => StackItemSerializer.Deserialize(byteArray4, (uint)byteArray4.Length);
            action4.Should().Throw<FormatException>();

            List<StackItem> list5 = new List<StackItem> { 1 };
            StackItem stackItem52 = new VM.Types.Array(list5);
            byte[] byteArray5 = StackItemSerializer.Serialize(stackItem52, MaxItemSize);
            StackItem result5 = StackItemSerializer.Deserialize(byteArray5, (uint)byteArray5.Length);
            Assert.AreEqual(((VM.Types.Array)stackItem52).Count, ((VM.Types.Array)result5).Count);
            Assert.AreEqual(((VM.Types.Array)stackItem52).GetEnumerator().Current, ((VM.Types.Array)result5).GetEnumerator().Current);

            List<StackItem> list6 = new List<StackItem> { 1 };
            StackItem stackItem62 = new Struct(list6);
            byte[] byteArray6 = StackItemSerializer.Serialize(stackItem62, MaxItemSize);
            StackItem result6 = StackItemSerializer.Deserialize(byteArray6, (uint)byteArray6.Length);
            Assert.AreEqual(((Struct)stackItem62).Count, ((Struct)result6).Count);
            Assert.AreEqual(((Struct)stackItem62).GetEnumerator().Current, ((Struct)result6).GetEnumerator().Current);

            Dictionary<PrimitiveType, StackItem> list7 = new Dictionary<PrimitiveType, StackItem> { [2] = 1 };
            StackItem stackItem72 = new Map(list7);
            byte[] byteArray7 = StackItemSerializer.Serialize(stackItem72, MaxItemSize);
            StackItem result7 = StackItemSerializer.Deserialize(byteArray7, (uint)byteArray7.Length);
            Assert.AreEqual(((Map)stackItem72).Count, ((Map)result7).Count);
            Assert.AreEqual(((Map)stackItem72).Keys.GetEnumerator().Current, ((Map)result7).Keys.GetEnumerator().Current);
            Assert.AreEqual(((Map)stackItem72).Values.GetEnumerator().Current, ((Map)result7).Values.GetEnumerator().Current);
        }
    }
}
