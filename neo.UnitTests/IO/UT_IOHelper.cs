using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_IOHelper
    {

        [TestMethod]
        public void TestReadVarInt()
        {
            for (int i = 0; i < 5; i++)
            {

                if (i == 0)
                {
                    MemoryStream stream = new MemoryStream();
                    BinaryWriter writer = new BinaryWriter(stream);

                    Neo.IO.Helper.WriteVarInt(writer, 0xFFFF);
                    stream.Seek(0, SeekOrigin.Begin);
                    BinaryReader reader = new BinaryReader(stream);

                    ulong result = Neo.IO.Helper.ReadVarInt(reader, 0xFFFF);
                    Assert.AreEqual((ulong)0xFFFF, result);
                }else if (i == 1)
                {
                    MemoryStream stream = new MemoryStream();
                    BinaryWriter writer = new BinaryWriter(stream);

                    Neo.IO.Helper.WriteVarInt(writer, 0xFFFFFFFF);
                    stream.Seek(0, SeekOrigin.Begin);
                    byte[] byteArray = new byte[stream.Length];
                    stream.Read(byteArray, 0, (int)stream.Length);
                    Assert.AreEqual(0xFE, byteArray[0]);
                    Assert.AreEqual(0xFFFFFFFF, BitConverter.ToUInt32(byteArray, 1));
                }
            }
        }

        [TestMethod]
        public void TestToArray()
        {
            byte[] byteArray = Neo.IO.Helper.ToArray(UInt160.Zero);
            Assert.AreEqual(Encoding.Default.GetString(new byte[] { 0x00,0x00,0x00,0x00,0x00,
                                         0x00,0x00,0x00,0x00,0x00,
                                         0x00,0x00,0x00,0x00,0x00,
                                         0x00,0x00,0x00,0x00,0x00}), Encoding.Default.GetString(byteArray));
        }

        [TestMethod]
        public void TestToByteArrayGeneric()
        {
            byte[] byteArray = Neo.IO.Helper.ToByteArray<UInt160>(new UInt160[] { UInt160.Zero });
            Assert.AreEqual(Encoding.Default.GetString(new byte[] { 0x01,0x00,0x00,0x00,0x00,0x00,
                                              0x00,0x00,0x00,0x00,0x00,
                                              0x00,0x00,0x00,0x00,0x00,
                                              0x00,0x00,0x00,0x00,0x00}), Encoding.Default.GetString(byteArray));
        }

        [TestMethod]
        public void TestWrite()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            Neo.IO.Helper.Write(writer, UInt160.Zero);
            stream.Seek(0, SeekOrigin.Begin);
            byte[] byteArray = new byte[stream.Length];
            stream.Read(byteArray, 0, (int)stream.Length);
            Assert.AreEqual(Encoding.Default.GetString(new byte[] {      0x00,0x00,0x00,0x00,0x00,
                                                                         0x00,0x00,0x00,0x00,0x00,
                                                                         0x00,0x00,0x00,0x00,0x00,
                                                                         0x00,0x00,0x00,0x00,0x00}), Encoding.Default.GetString(byteArray));
        }

        [TestMethod]
        public void TestWriteGeneric()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            Neo.IO.Helper.Write<UInt160>(writer, new UInt160[] { UInt160.Zero });
            stream.Seek(0, SeekOrigin.Begin);
            byte[] byteArray = new byte[stream.Length];
            stream.Read(byteArray, 0, (int)stream.Length);
            Assert.AreEqual(Encoding.Default.GetString(new byte[] { 0x01,0x00,0x00,0x00,0x00,0x00,
                                                                         0x00,0x00,0x00,0x00,0x00,
                                                                         0x00,0x00,0x00,0x00,0x00,
                                                                         0x00,0x00,0x00,0x00,0x00}), Encoding.Default.GetString(byteArray));
        }


        [TestMethod]
        public void TestWriteBytesWithGrouping()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            Neo.IO.Helper.WriteBytesWithGrouping(writer, new byte[] { 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA,
                                                                      0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA,
                                                                      0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA,
                                                                      0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA,
                                                                      0xAA, 0xAA, 0xAA, 0xAA});
            stream.Seek(0, SeekOrigin.Begin);
            byte[] byteArray = new byte[stream.Length];
            stream.Read(byteArray, 0, (int)stream.Length);
            Assert.AreEqual(Encoding.Default.GetString(new byte[] { 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA,
                                                                      0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA,0x00,
                                                                      0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA,
                                                                      0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA,0x00,
                                                                      0xAA, 0xAA, 0xAA, 0xAA, 0x00, 0x00, 0x00, 0x00,
                                                                      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,0x0C}), Encoding.Default.GetString(byteArray));
        }

        [TestMethod]
        public void TestWriteFixedString()
        {
            for (int i = 0; i < 5; i++)
            {

                if (i == 0)
                {
                    try
                    {
                        MemoryStream stream = new MemoryStream();
                        BinaryWriter writer = new BinaryWriter(stream);

                        Neo.IO.Helper.WriteFixedString(writer, null, 0);
                    }
                    catch (Exception e)
                    {
                        Assert.IsTrue(e is ArgumentNullException);
                    }
                }
                else if (i == 1)
                {
                    try
                    {
                        MemoryStream stream = new MemoryStream();
                        BinaryWriter writer = new BinaryWriter(stream);

                        Neo.IO.Helper.WriteFixedString(writer, "AA", Encoding.UTF8.GetBytes("AA").Length - 1);
                    }
                    catch (Exception e)
                    {
                        Assert.IsTrue(e is ArgumentException);
                    }
                }
                else if (i == 2)
                {

                    try
                    {
                        MemoryStream stream = new MemoryStream();
                        BinaryWriter writer = new BinaryWriter(stream);

                        Neo.IO.Helper.WriteFixedString(writer, "拉拉", Encoding.UTF8.GetBytes("拉拉").Length - 1);
                    }
                    catch (Exception e)
                    {
                        Assert.IsTrue(e is ArgumentException);
                    }
                }
                else if (i == 3)
                {
                    MemoryStream stream = new MemoryStream();
                    BinaryWriter writer = new BinaryWriter(stream);

                    Neo.IO.Helper.WriteFixedString(writer, "AA", Encoding.UTF8.GetBytes("AA").Length + 1);
                    stream.Seek(0, SeekOrigin.Begin);
                    byte[] byteArray = new byte[stream.Length];
                    stream.Read(byteArray, 0, (int)stream.Length);

                    byte[] newArray = new byte[Encoding.UTF8.GetBytes("AA").Length + 1];
                    Encoding.UTF8.GetBytes("AA").CopyTo(newArray, 0);
                    Assert.AreEqual(Encoding.Default.GetString(newArray), Encoding.Default.GetString(byteArray));


                }
            }
        }

        [TestMethod]
        public void TestWriteVarBytes()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            Neo.IO.Helper.WriteVarBytes(writer, new byte[] { 0xAA });
            stream.Seek(0, SeekOrigin.Begin);
            byte[] byteArray = new byte[stream.Length];
            stream.Read(byteArray, 0, (int)stream.Length);
            Assert.AreEqual(Encoding.Default.GetString(new byte[] { 0x01, 0xAA }), Encoding.Default.GetString(byteArray));
        }

        [TestMethod]
        public void TestWriteVarInt()
        {
            for (int i = 0; i < 5; i++)
            {

                if (i == 0)
                {
                    try
                    {
                        MemoryStream stream = new MemoryStream();
                        BinaryWriter writer = new BinaryWriter(stream);

                        Neo.IO.Helper.WriteVarInt(writer, -1);
                    }
                    catch (Exception e)
                    {
                        Assert.IsTrue(e is ArgumentOutOfRangeException);
                    }
                }
                else if (i == 1)
                {
                    MemoryStream stream = new MemoryStream();
                    BinaryWriter writer = new BinaryWriter(stream);

                    Neo.IO.Helper.WriteVarInt(writer, 0xFC);
                    stream.Seek(0, SeekOrigin.Begin);
                    byte[] byteArray = new byte[stream.Length];
                    stream.Read(byteArray, 0, (int)stream.Length);
                    Assert.AreEqual(0xFC, byteArray[0]);
                }
                else if (i == 2)
                {
                    MemoryStream stream = new MemoryStream();
                    BinaryWriter writer = new BinaryWriter(stream);

                    Neo.IO.Helper.WriteVarInt(writer, 0xFFFF);
                    stream.Seek(0, SeekOrigin.Begin);
                    byte[] byteArray = new byte[stream.Length];
                    stream.Read(byteArray, 0, (int)stream.Length);
                    Assert.AreEqual(0xFD, byteArray[0]);
                    Assert.AreEqual(Encoding.Default.GetString(new byte[] { 0xFF, 0xFF }), Encoding.Default.GetString(byteArray.Skip(1).Take(byteArray.Length - 1).ToArray()));
                }
                else if (i == 3)
                {
                    MemoryStream stream = new MemoryStream();
                    BinaryWriter writer = new BinaryWriter(stream);

                    Neo.IO.Helper.WriteVarInt(writer, 0xFFFFFFFF);
                    stream.Seek(0, SeekOrigin.Begin);
                    byte[] byteArray = new byte[stream.Length];
                    stream.Read(byteArray, 0, (int)stream.Length);
                    Assert.AreEqual(0xFE, byteArray[0]);
                    Assert.AreEqual(0xFFFFFFFF, BitConverter.ToUInt32(byteArray, 1));
                }
                else
                {
                    MemoryStream stream = new MemoryStream();
                    BinaryWriter writer = new BinaryWriter(stream);

                    Neo.IO.Helper.WriteVarInt(writer, 0xAEFFFFFFFF);
                    stream.Seek(0, SeekOrigin.Begin);
                    byte[] byteArray = new byte[stream.Length];
                    stream.Read(byteArray, 0, (int)stream.Length);
                    Assert.AreEqual(0xFF, byteArray[0]);
                    //long value has 8 bytes
                    Assert.AreEqual(Encoding.Default.GetString(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00 }), Encoding.Default.GetString(byteArray.Skip(1).Take(byteArray.Length - 1).ToArray()));

                }
            }
        }

        [TestMethod]
        public void TestWriteVarString()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            Neo.IO.Helper.WriteVarString(writer, "a");
            stream.Seek(0, SeekOrigin.Begin);
            byte[] byteArray = new byte[stream.Length];
            stream.Read(byteArray, 0, (int)stream.Length);
            Assert.AreEqual(0x01, byteArray[0]);
            Assert.AreEqual(0x61, byteArray[1]);

        }

    }
}
