using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Json;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using System.IO;
using Neo.VM;
using Neo.VM.Types;
using Neo.IO;
using System;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_IOHelper
    {
        [TestInitialize]
        public void TestSetup()
        {
        }

        [TestMethod]
        public void ReadVarBytes_With_Ulong_2147483591()
        {
            byte[] data = new byte[]{0xff,  // ulong on ReadVarInt()
                                     // maximum accepted value on ReadVarBytes (0x000000007fffffc7 => 2147483591)
                                     0xc7, 0xff, 0xff, 0x7f, 0x00, 0x00, 0x00, 0x00,
                                     0x0a, 0x0b}; // actual data: {0a, 0b}

            // Testing version without ReadVarBytes()
            data.Length.Should().Be(11);
            using (MemoryStream ms = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                int size = (int)reader.ReadVarInt();
                size.Should().Be(2147483591);
                byte[] b = reader.ReadBytes(size);
                b.Length.Should().Be(2);
            }

            // Testing version without ReadVarBytes() but using max size
            data.Length.Should().Be(11);
            using (MemoryStream ms = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                int size = (int)reader.ReadVarInt(ulong.MaxValue); // ulong max = ulong.MaxValue
                size.Should().Be(2147483591);
                byte[] b = reader.ReadBytes(size);
                b.Length.Should().Be(2);
            }

            // Testing version with ReadVarBytes() -> should fail
            data.Length.Should().Be(11);
            using (MemoryStream ms = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                try {
                    byte[] b = reader.ReadVarBytes(); // int max = 0x1000000
                }
                catch (FormatException) {
                    // Should not pass limit of 0x1000000
                }
            }
        }

        [TestMethod]
        public void ReadVarBytes_With_Ulong_16777216()
        {
            byte[] data = new byte[]{0xff,  // ulong on ReadVarInt()
                                     // value 0x1000000 -> 16777216
                                     0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00,
                                     0x0a, 0x0b}; // actual data: {0a, 0b}

            // Testing version without ReadVarBytes()
            data.Length.Should().Be(11);
            using (MemoryStream ms = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                int size = (int)reader.ReadVarInt();
                size.Should().Be(16777216);
                byte[] b = reader.ReadBytes(size);
                b.Length.Should().Be(2);
            }

            // Testing version with ReadVarBytes() -> limit 16777216
            data.Length.Should().Be(11);
            using (MemoryStream ms = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                byte[] b = reader.ReadVarBytes(); // int max = 0x1000000
                b.Length.Should().Be(2);
            }
        }

        [TestMethod]
        public void ReadVarBytes_With_Ulong_16777217()
        {
            byte[] data = new byte[]{0xff,  // ulong on ReadVarInt()
                                     // value 0x1000001 -> 16777217
                                     0x01, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00,
                                     0x0a, 0x0b}; // actual data: {0a, 0b}

            // Testing version without ReadVarBytes()
            data.Length.Should().Be(11);
            using (MemoryStream ms = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                int size = (int)reader.ReadVarInt();
                size.Should().Be(16777217);
                byte[] b = reader.ReadBytes(size);
                b.Length.Should().Be(2);
            }

            // Testing version with ReadVarBytes() -> limit 16777216
            data.Length.Should().Be(11);
            using (MemoryStream ms = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                try {
                    byte[] b = reader.ReadVarBytes(); // int max = 0x1000000
                }
                catch (FormatException) {
                    // Should not pass limit of 0x1000000
                }
            }
        }

        [TestMethod]
        public void ReadVarBytes_With_Uint32_16777216()
        {
            byte[] data = new byte[]{0xfe,  // uint32 on ReadVarInt()
                                     // value 0x1000000 -> 16777216
                                     0x00, 0x00, 0x00, 0x01,
                                     0x0a, 0x0b}; // actual data: {0a, 0b}

            // Testing version without ReadVarBytes()
            data.Length.Should().Be(7);
            using (MemoryStream ms = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                int size = (int)reader.ReadVarInt();
                size.Should().Be(16777216);
                byte[] b = reader.ReadBytes(size);
                b.Length.Should().Be(2);
            }

            // Testing version with ReadVarBytes() -> limit 16777216
            data.Length.Should().Be(7);
            using (MemoryStream ms = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                byte[] b = reader.ReadVarBytes(); // int max = 0x1000000
                b.Length.Should().Be(2);
            }
        }

        [TestMethod]
        public void ReadVarBytes_With_Uint16_65535()
        {
            byte[] data = new byte[]{0xfd,  // uint16 on ReadVarInt()
                                     // value 0xffff -> 65535
                                     0xff, 0xff,
                                     0x0a, 0x0b}; // actual data: {0a, 0b}

            // Testing version without ReadVarBytes()
            data.Length.Should().Be(5);
            using (MemoryStream ms = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                int size = (int)reader.ReadVarInt();
                size.Should().Be(65535);
                byte[] b = reader.ReadBytes(size);
                b.Length.Should().Be(2);
            }

            // Testing version with ReadVarBytes() -> limit 16777216
            data.Length.Should().Be(5);
            using (MemoryStream ms = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                byte[] b = reader.ReadVarBytes(); // int max = 0x1000000
                b.Length.Should().Be(2);
            }
        }

        [TestMethod]
        public void ReadVarBytes_With_Uint32_65536()
        {
            byte[] data = new byte[]{0xfe,  // uint16 on ReadVarInt()
                                     // value 0x10000 -> 65536
                                     0x00, 0x00, 0x01, 0x00,
                                     0x0a, 0x0b}; // actual data: {0a, 0b}

            // Testing version without ReadVarBytes()
            data.Length.Should().Be(7);
            using (MemoryStream ms = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                int size = (int)reader.ReadVarInt();
                size.Should().Be(65536);
                byte[] b = reader.ReadBytes(size);
                b.Length.Should().Be(2);
            }

            // Testing version with ReadVarBytes() -> limit 16777216
            data.Length.Should().Be(7);
            using (MemoryStream ms = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                byte[] b = reader.ReadVarBytes(); // int max = 0x1000000
                b.Length.Should().Be(2);
            }
        }

        [TestMethod]
        public void ReadVarBytes_With_Uint8_252()
        {
            byte[] data = new byte[]{0xfc,  // uint8 on ReadVarInt()
                                     0x0a, 0x0b}; // actual data: {0a, 0b}

            // Testing version without ReadVarBytes()
            data.Length.Should().Be(3);
            using (MemoryStream ms = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                int size = (int)reader.ReadVarInt();
                size.Should().Be(252);
                byte[] b = reader.ReadBytes(size);
                b.Length.Should().Be(2);
            }

            // Testing version with ReadVarBytes() -> limit 16777216
            data.Length.Should().Be(3);
            using (MemoryStream ms = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                byte[] b = reader.ReadVarBytes(); // int max = 0x1000000
                b.Length.Should().Be(2);
            }
        }

        [TestMethod]
        public void ReadVarBytes_With_Uint16_253()
        {
            byte[] data = new byte[]{0xfd,  // uint16 on ReadVarInt()
                                     0xfd, 0x00,
                                     0x0a, 0x0b}; // actual data: {0a, 0b}

            // Testing version without ReadVarBytes()
            data.Length.Should().Be(5);
            using (MemoryStream ms = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                int size = (int)reader.ReadVarInt();
                size.Should().Be(253);
                byte[] b = reader.ReadBytes(size);
                b.Length.Should().Be(2);
            }

            // Testing version with ReadVarBytes() -> limit 16777216
            data.Length.Should().Be(5);
            using (MemoryStream ms = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                byte[] b = reader.ReadVarBytes(); // int max = 0x1000000
                b.Length.Should().Be(2);
            }
        }

        [TestMethod]
        public void ReadVarBytes_With_Uint8_251()
        {
            byte[] data = new byte[]{0xfb,  // uint8 on ReadVarInt()
                                     0x0a, 0x0b}; // actual data: {0a, 0b}

            // Testing version without ReadVarBytes()
            data.Length.Should().Be(3);
            using (MemoryStream ms = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                int size = (int)reader.ReadVarInt();
                size.Should().Be(251);
                byte[] b = reader.ReadBytes(size);
                b.Length.Should().Be(2);
            }

            // Testing version with ReadVarBytes() -> limit 16777216
            data.Length.Should().Be(3);
            using (MemoryStream ms = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                byte[] b = reader.ReadVarBytes(); // int max = 0x1000000
                b.Length.Should().Be(2);
            }
        }

        [TestMethod]
        public void ReadVarBytes_With_Uint16_252()
        {
            byte[] data = new byte[]{0xfd,  // uint16 on ReadVarInt()
                                     0xfc, 0x00,
                                     0x0a, 0x0b}; // actual data: {0a, 0b}

            // Testing version without ReadVarBytes()
            data.Length.Should().Be(5);
            using (MemoryStream ms = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                int size = (int)reader.ReadVarInt();
                size.Should().Be(252);
                byte[] b = reader.ReadBytes(size);
                b.Length.Should().Be(2);
            }

            // Testing version with ReadVarBytes() -> limit 16777216
            data.Length.Should().Be(5);
            using (MemoryStream ms = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                byte[] b = reader.ReadVarBytes(); // int max = 0x1000000
                b.Length.Should().Be(2);
            }
        }

        [TestMethod]
        public void ReadVarBytes_With_Uint8_3()
        {
            byte[] data = new byte[]{0x03,  // uint8 on ReadVarInt()
                                     0x0a, 0x0b, 0x0c, 0x0d}; // actual data: {0a, 0b, 0c, 0d}

            // Testing version without ReadVarBytes()
            data.Length.Should().Be(5);
            using (MemoryStream ms = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                int size = (int)reader.ReadVarInt();
                size.Should().Be(3);
                byte[] b = reader.ReadBytes(size);
                b.Length.Should().Be(3);
            }

            // Testing version with ReadVarBytes() -> limit 16777216
            data.Length.Should().Be(5);
            using (MemoryStream ms = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                byte[] b = reader.ReadVarBytes(); // int max = 0x1000000
                b.Length.Should().Be(3);
            }
        }

        [TestMethod]
        public void ReadVarBytes_With_Uint8_1()
        {
            byte[] data = new byte[]{0x01,  // uint8 on ReadVarInt()
                                     0x0a, 0x0b, 0x0c, 0x0d}; // actual data: {0a, 0b, 0c, 0d}

            // Testing version without ReadVarBytes()
            data.Length.Should().Be(5);
            using (MemoryStream ms = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                int size = (int)reader.ReadVarInt();
                size.Should().Be(1);
                byte[] b = reader.ReadBytes(size);
                b.Length.Should().Be(1);
                b[0].Should().Be(0x0a);
            }

            // Testing version with ReadVarBytes() -> limit 16777216
            data.Length.Should().Be(5);
            using (MemoryStream ms = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                byte[] b = reader.ReadVarBytes(); // int max = 0x1000000
                b.Length.Should().Be(1);
                b[0].Should().Be(0x0a);
            }
        }

        [TestMethod]
        public void ReadVarBytes_With_Uint8_0()
        {
            byte[] data = new byte[]{0x00,  // uint8 on ReadVarInt()
                                     0x0a, 0x0b, 0x0c, 0x0d}; // actual data: {0a, 0b, 0c, 0d}

            // Testing version without ReadVarBytes()
            data.Length.Should().Be(5);
            using (MemoryStream ms = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                int size = (int)reader.ReadVarInt();
                size.Should().Be(0);
                byte[] b = reader.ReadBytes(size);
                b.Length.Should().Be(0);
            }

            // Testing version with ReadVarBytes() -> limit 16777216
            data.Length.Should().Be(5);
            using (MemoryStream ms = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                byte[] b = reader.ReadVarBytes(); // int max = 0x1000000
                b.Length.Should().Be(0);
            }
        }

        [TestMethod]
        public void ReadVarBytes_With_Uint32_0()
        {
            byte[] data = new byte[]{0xfe,  // uint32 on ReadVarInt()
                                     0x00, 0x00, 0x00, 0x00,
                                     0x0a, 0x0b, 0x0c, 0x0d}; // actual data: {0a, 0b, 0c, 0d}

            // Testing version without ReadVarBytes()
            data.Length.Should().Be(9);
            using (MemoryStream ms = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                int size = (int)reader.ReadVarInt();
                size.Should().Be(0);
                byte[] b = reader.ReadBytes(size);
                b.Length.Should().Be(0);
            }

            // Testing version with ReadVarBytes() -> limit 16777216
            data.Length.Should().Be(9);
            using (MemoryStream ms = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                byte[] b = reader.ReadVarBytes(); // int max = 0x1000000
                b.Length.Should().Be(0);
            }
        }

    }
}
