using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.IO.Data.LevelDB;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Neo.UnitTests.IO.Data.LevelDb
{
    public class Test { }

    [TestClass]
    public class UT_Slice
    {
        private Slice sliceTest;

        [TestMethod]
        public void TestConstructor()
        {
            IntPtr parr = Marshal.AllocHGlobal(1);
            Marshal.WriteByte(parr, 0x01);
            UIntPtr plength = new UIntPtr(1);
            sliceTest = new Slice(parr, plength);
            Assert.IsNotNull(sliceTest);
            Assert.IsInstanceOfType(sliceTest, typeof(Slice));
            Slice slice = (byte)0x01;
            Assert.AreEqual(slice, sliceTest);
            Marshal.FreeHGlobal(parr);
        }

        [TestMethod]
        public void TestCompareTo()
        {
            Slice slice = new byte[] { 0x01, 0x02 };
            sliceTest = new byte[] { 0x01, 0x02 };
            int result = sliceTest.CompareTo(slice);
            Assert.AreEqual(0, result);
            sliceTest = new byte[] { 0x01 };
            result = sliceTest.CompareTo(slice);
            Assert.AreEqual(-1, result);
            sliceTest = new byte[] { 0x01, 0x02, 0x03 };
            result = sliceTest.CompareTo(slice);
            Assert.AreEqual(1, result);
            sliceTest = new byte[] { 0x01, 0x03 };
            result = sliceTest.CompareTo(slice);
            Assert.AreEqual(1, result);
            sliceTest = new byte[] { 0x01, 0x01 };
            result = sliceTest.CompareTo(slice);
            Assert.AreEqual(-1, result);
        }

        [TestMethod]
        public void TestEqualsSlice()
        {
            byte[] arr1 = { 0x01, 0x02 };
            byte[] arr2 = { 0x01, 0x02 };
            Slice slice = arr1;
            sliceTest = arr1;
            Assert.IsTrue(sliceTest.Equals(slice));
            sliceTest = arr2;
            Assert.IsTrue(sliceTest.Equals(slice));
            sliceTest = new byte[] { 0x01, 0x03 };
            Assert.IsFalse(sliceTest.Equals(slice));
        }

        [TestMethod]
        public void TestEqualsObj()
        {
            sliceTest = new byte[] { 0x01 };
            object slice = null;
            bool result = sliceTest.Equals(slice);
            Assert.AreEqual(false, result);
            slice = new Test();
            result = sliceTest.Equals(slice);
            Assert.AreEqual(false, result);
            slice = sliceTest;
            result = sliceTest.Equals(slice);
            Assert.AreEqual(true, result);
            Slice s = new byte[] { 0x01 };
            result = sliceTest.Equals(s);
            Assert.AreEqual(true, result);
            s = new byte[] { 0x01, 0x02 };
            result = sliceTest.Equals(s);
            Assert.AreEqual(false, result);
        }

        [TestMethod]
        public void TestGetHashCode()
        {
            byte[] arr = new byte[] { 0x01, 0x02 };
            sliceTest = arr;
            int hash1 = (int)arr.Murmur32(0);
            int hash2 = sliceTest.GetHashCode();
            Assert.AreEqual(hash2, hash1);
        }

        [TestMethod]
        public void TestFromArray()
        {
            byte[] arr = new byte[]{
                0x01,0x01,0x01,0x01,
            };
            IntPtr parr = Marshal.AllocHGlobal(arr.Length);
            for (int i = 0; i < arr.Length; i++)
            {
                Marshal.WriteByte(parr + i, 0x01);
            }
            UIntPtr plength = new UIntPtr((uint)arr.Length);
            Slice slice = new Slice(parr, plength);
            sliceTest = arr;
            Assert.AreEqual(slice, sliceTest);
            Marshal.FreeHGlobal(parr);
        }

        [TestMethod]
        public void TestToArray()
        {
            sliceTest = new Slice();
            byte[] arr = sliceTest.ToArray();
            Assert.AreEqual(0, arr.Length);
            arr = new byte[] { 0x01, 0x02 };
            sliceTest = arr;
            byte[] parr = sliceTest.ToArray();
            Assert.AreSame(parr, arr);
        }

        [TestMethod]
        public void TestToBoolean()
        {
            sliceTest = new byte[] { 0x01, 0x02 };
            Assert.ThrowsException<InvalidCastException>(() => sliceTest.ToBoolean());
            sliceTest = (byte)0x01;
            bool result = sliceTest.ToBoolean();
            Assert.AreEqual(true, result);
        }

        [TestMethod]
        public void TestToByte()
        {
            sliceTest = new byte[] { 0x01, 0x02 };
            Assert.ThrowsException<InvalidCastException>(() => sliceTest.ToByte());
            sliceTest = (byte)0x01;
            byte result = sliceTest.ToByte();
            Assert.AreEqual((byte)0x01, result);
        }

        [TestMethod]
        public void TestToDouble()
        {
            sliceTest = new byte[] { 0x01 };
            Assert.ThrowsException<InvalidCastException>(() => sliceTest.ToDouble());
            byte[] arr = new byte[sizeof(double)];
            sliceTest = arr;
            double result = sliceTest.ToDouble();
            Assert.AreEqual(0D, result);
            sliceTest = 0.5D;
            Assert.AreEqual(0.5D, sliceTest.ToDouble());
        }

        [TestMethod]
        public void TestToInt16()
        {
            sliceTest = new byte[] { 0x01 };
            Assert.ThrowsException<InvalidCastException>(() => sliceTest.ToInt16());
            sliceTest = (Int16)(-15);
            Assert.AreEqual((Int16)(-15), sliceTest.ToInt16());
        }

        [TestMethod]
        public void TestToInt32()
        {
            sliceTest = new byte[] { 0x01 };
            Assert.ThrowsException<InvalidCastException>(() => sliceTest.ToInt32());
            sliceTest = (Int32)(-15);
            Assert.AreEqual((Int32)(-15), sliceTest.ToInt32());
        }

        [TestMethod]
        public void TestToInt64()
        {
            sliceTest = new byte[] { 0x01 };
            Assert.ThrowsException<InvalidCastException>(() => sliceTest.ToInt64());
            sliceTest = Int64.MaxValue;
            Assert.AreEqual(Int64.MaxValue, sliceTest.ToInt64());
        }

        [TestMethod]
        public void TestToSingle()
        {
            sliceTest = new byte[] { 0x01 };
            Assert.ThrowsException<InvalidCastException>(() => sliceTest.ToSingle());
            sliceTest = (float)(-15.5);
            Assert.AreEqual((float)(-15.5), sliceTest.ToSingle());
        }

        [TestMethod]
        public void TestToString()
        {
            sliceTest = "abc你好";
            Assert.AreEqual("abc你好", sliceTest.ToString());
        }

        [TestMethod]
        public void TestToUint16()
        {
            sliceTest = new byte[] { 0x01 };
            Assert.ThrowsException<InvalidCastException>(() => sliceTest.ToUInt16());
            sliceTest = (UInt16)(25);
            Assert.AreEqual((UInt16)25, sliceTest.ToUInt16());
        }

        [TestMethod]
        public void TestToUint32()
        {
            sliceTest = new byte[] { 0x01 };
            Assert.ThrowsException<InvalidCastException>(() => sliceTest.ToUInt32());
            sliceTest = (UInt32)(2525252525);
            Assert.AreEqual((UInt32)2525252525, sliceTest.ToUInt32());
        }

        [TestMethod]
        public void TestToUint64()
        {
            sliceTest = new byte[] { 0x01 };
            Assert.ThrowsException<InvalidCastException>(() => sliceTest.ToUInt64());
            sliceTest = (UInt64)(0x2525252525252525);
            Assert.AreEqual((UInt64)(0x2525252525252525), sliceTest.ToUInt64());
        }

        [TestMethod]
        public void TestFromBool()
        {
            byte[] arr = { 0x01 };
            Slice slice = arr;
            sliceTest = true;
            Assert.AreEqual(slice, sliceTest);
        }

        [TestMethod]
        public void TestFromByte()
        {
            sliceTest = (byte)0x01;
            byte[] arr = { 0x01 };
            Slice slice = arr;
            Assert.AreEqual(slice, sliceTest);
        }

        [TestMethod]
        public void TestFromDouble()
        {
            Slice slice = BitConverter.GetBytes(1.23D);
            sliceTest = 1.23D;
            Assert.AreEqual(slice, sliceTest);
        }

        [TestMethod]
        public void TestFromShort()
        {
            Slice slice = BitConverter.GetBytes((short)1234);
            sliceTest = (short)1234;
            Assert.AreEqual(slice, sliceTest);
        }

        [TestMethod]
        public void TestFromInt()
        {
            Slice slice = BitConverter.GetBytes(-1234);
            sliceTest = -1234;
            Assert.AreEqual(slice, sliceTest);
        }

        [TestMethod]
        public void TestFromLong()
        {
            Slice slice = BitConverter.GetBytes(-1234L);
            sliceTest = -1234L;
            Assert.AreEqual(slice, sliceTest);
        }

        [TestMethod]
        public void TestFromFloat()
        {
            Slice slice = BitConverter.GetBytes(1.234F);
            sliceTest = 1.234F;
            Assert.AreEqual(slice, sliceTest);
        }

        [TestMethod]
        public void TestFromString()
        {
            string str = "abcdefghijklmnopqrstuvwxwz!@#$%^&*&()_+?><你好";
            Slice slice = Encoding.UTF8.GetBytes(str);
            sliceTest = str;
            Assert.AreEqual(slice, sliceTest);
        }

        [TestMethod]
        public void TestFromUnshort()
        {
            Slice slice = BitConverter.GetBytes((ushort)12345);
            sliceTest = (ushort)12345;
            Assert.AreEqual(slice, sliceTest);
        }

        [TestMethod]
        public void TestFromUint()
        {
            Slice slice = BitConverter.GetBytes((uint)12345);
            sliceTest = (uint)12345;
            Assert.AreEqual(slice, sliceTest);
        }

        [TestMethod]
        public void TestFromUlong()
        {
            Slice slice = BitConverter.GetBytes(12345678UL);
            sliceTest = 12345678UL;
            Assert.AreEqual(slice, sliceTest);
        }

        [TestMethod]
        public void TestLessThan()
        {
            sliceTest = new byte[] { 0x01 };
            Slice slice = new byte[] { 0x02 };
            bool result = sliceTest < slice;
            Assert.AreEqual(true, result);
            slice = new byte[] { 0x01 };
            result = sliceTest < slice;
            Assert.AreEqual(false, result);
            slice = new byte[] { 0x00 };
            result = sliceTest < slice;
            Assert.AreEqual(false, result);
        }

        [TestMethod]
        public void TestLessThanAndEqual()
        {
            sliceTest = new byte[] { 0x01 };
            Slice slice = new byte[] { 0x02 };
            bool result = sliceTest <= slice;
            Assert.AreEqual(true, result);
            slice = new byte[] { 0x01 };
            result = sliceTest <= slice;
            Assert.AreEqual(true, result);
            slice = new byte[] { 0x00 };
            result = sliceTest <= slice;
            Assert.AreEqual(false, result);
        }

        [TestMethod]
        public void TestGreatThan()
        {
            sliceTest = new byte[] { 0x01 };
            Slice slice = new byte[] { 0x00 };
            bool result = sliceTest > slice;
            Assert.AreEqual(true, result);
            slice = new byte[] { 0x01 };
            result = sliceTest > slice;
            Assert.AreEqual(false, result);
            slice = new byte[] { 0x02 };
            result = sliceTest > slice;
            Assert.AreEqual(false, result);
        }

        [TestMethod]
        public void TestGreatThanAndEqual()
        {
            sliceTest = new byte[] { 0x01 };
            Slice slice = new byte[] { 0x00 };
            bool result = sliceTest >= slice;
            Assert.AreEqual(true, result);
            slice = new byte[] { 0x01 };
            result = sliceTest >= slice;
            Assert.AreEqual(true, result);
            slice = new byte[] { 0x02 };
            result = sliceTest >= slice;
            Assert.AreEqual(false, result);
        }

        [TestMethod]
        public void TestEqual()
        {
            sliceTest = new byte[] { 0x01 };
            Slice slice = new byte[] { 0x00 };
            bool result = sliceTest == slice;
            Assert.AreEqual(false, result);
            slice = new byte[] { 0x01 };
            result = sliceTest == slice;
            Assert.AreEqual(true, result);
            slice = new byte[] { 0x02 };
            result = sliceTest == slice;
            Assert.AreEqual(false, result);
        }

        [TestMethod]
        public void TestUnequal()
        {
            sliceTest = new byte[] { 0x01 };
            Slice slice = new byte[] { 0x00 };
            bool result = sliceTest != slice;
            Assert.AreEqual(true, result);
            slice = new byte[] { 0x01 };
            result = sliceTest != slice;
            Assert.AreEqual(false, result);
        }
    }
}
