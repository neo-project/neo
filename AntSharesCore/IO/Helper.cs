using System;
using System.IO;
using System.Linq;
using System.Text;

namespace AntShares.IO
{
    internal static class Helper
    {
        public static byte[][] ReadBytesArray(this BinaryReader reader)
        {
            byte[][] array = new byte[reader.ReadVarInt()][];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = reader.ReadBytes((int)reader.ReadVarInt());
            }
            return array;
        }

        public static string ReadFixedString(this BinaryReader reader, int length)
        {
            byte[] data = reader.ReadBytes(length);
            return Encoding.UTF8.GetString(data.TakeWhile(p => p != 0).ToArray());
        }

        public static T ReadSerializable<T>(this BinaryReader reader) where T : ISerializable, new()
        {
            T obj = new T();
            obj.Deserialize(reader);
            return obj;
        }

        public static T[] ReadSerializableArray<T>(this BinaryReader reader) where T : ISerializable, new()
        {
            T[] array = new T[reader.ReadVarInt()];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = new T();
                array[i].Deserialize(reader);
            }
            return array;
        }

        public static UInt64 ReadVarInt(this BinaryReader reader)
        {
            byte value = reader.ReadByte();
            if (value == 0xFD)
                return reader.ReadUInt16();
            else if (value == 0xFE)
                return reader.ReadUInt32();
            else if (value == 0xFF)
                return reader.ReadUInt64();
            else
                return value;
        }

        public static string ReadVarString(this BinaryReader reader)
        {
            return Encoding.UTF8.GetString(reader.ReadBytes((int)reader.ReadVarInt()));
        }

        public static void Write(this BinaryWriter writer, ISerializable value)
        {
            value.Serialize(writer);
        }

        public static void Write(this BinaryWriter writer, ISerializable[] value)
        {
            writer.WriteVarInt(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                value[i].Serialize(writer);
            }
        }

        public static void Write(this BinaryWriter writer, byte[][] value)
        {
            writer.WriteVarInt(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                writer.WriteVarInt(value[i].Length);
                writer.Write(value[i]);
            }
        }

        public static void WriteFixedString(this BinaryWriter writer, string value, int length)
        {
            if (value == null)
                throw new ArgumentNullException();
            if (value.Length < length)
                value = value.PadRight(length, '\0');
            if (value.Length != length)
                throw new ArgumentException();
            writer.Write(Encoding.UTF8.GetBytes(value));
        }

        public static void WriteVarInt(this BinaryWriter writer, long value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException();
            if (value < 0xFD)
            {
                writer.Write((byte)value);
            }
            else if (value <= 0xFFFF)
            {
                writer.Write((byte)0xFD);
                writer.Write((UInt16)value);
            }
            else if (value <= 0xFFFFFFFF)
            {
                writer.Write((byte)0xFE);
                writer.Write((UInt32)value);
            }
            else
            {
                writer.Write((byte)0xFF);
                writer.Write(value);
            }
        }

        public static void WriteVarString(this BinaryWriter writer, string value)
        {
            byte[] data = Encoding.UTF8.GetBytes(value);
            writer.WriteVarInt(data.Length);
            writer.Write(data);
        }
    }
}
