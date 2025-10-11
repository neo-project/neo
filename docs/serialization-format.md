# Neo Serialization Format

This document describes the binary serialization format used by the Neo blockchain platform. The format is designed for efficient serialization and deserialization of blockchain data structures.

## Overview

Neo uses a custom binary serialization format that supports:
- Primitive data types (integers, booleans, bytes)
- Variable-length integers (VarInt)
- Strings (fixed and variable length)
- Arrays and collections
- Custom serializable objects
- Nullable objects

## Core Interfaces

### ISerializable

All serializable objects in Neo implement the `ISerializable` interface:

```csharp
public interface ISerializable
{
    int Size { get; }

    void Serialize(BinaryWriter writer);

    void Deserialize(ref MemoryReader reader);
}
```

- `Size`: Returns the serialized size in bytes
- `Serialize`: Writes the object to a BinaryWriter
- `Deserialize`: Reads the object from a MemoryReader

## Primitive Data Types

### Integers

Neo supports both little-endian and big-endian integer formats:

| Type | Size | Endianness | Description |
|------|------|------------|-------------|
| `sbyte` | 1 byte | N/A | Signed 8-bit integer |
| `byte` | 1 byte | N/A | Unsigned 8-bit integer |
| `short` | 2 bytes | Little-endian | Signed 16-bit integer |
| `ushort` | 2 bytes | Little-endian | Unsigned 16-bit integer |
| `int` | 4 bytes | Little-endian | Signed 32-bit integer |
| `uint` | 4 bytes | Little-endian | Unsigned 32-bit integer |
| `long` | 8 bytes | Little-endian | Signed 64-bit integer |
| `ulong` | 8 bytes | Little-endian | Unsigned 64-bit integer |

Big-endian variants are available for `short`, `ushort`, `int`, `uint`, `long`, and `ulong`.

### Boolean

Booleans are serialized as single bytes:
- `false` → `0x00`
- `true` → `0x01`
- Any other value throws `FormatException`

### Variable-Length Integers (VarInt)

Neo uses a compact variable-length integer format:

| Value Range | Format | Size |
|-------------|--------|------|
| 0-252 | Direct value | 1 byte |
| 253-65535 | `0xFD` + 2-byte little-endian | 3 bytes |
| 65536-4294967295 | `0xFE` + 4-byte little-endian | 5 bytes |
| 4294967296+ | `0xFF` + 8-byte little-endian | 9 bytes |

**Serialization:**
```csharp
if (value < 0xFD)
{
    writer.Write((byte)value);
}
else if (value <= 0xFFFF)
{
    writer.Write((byte)0xFD);
    writer.Write((ushort)value);
}
else if (value <= 0xFFFFFFFF)
{
    writer.Write((byte)0xFE);
    writer.Write((uint)value);
}
else
{
    writer.Write((byte)0xFF);
    writer.Write(value);
}
```

**Deserialization:**
```csharp
var b = ReadByte();
var value = b switch
{
    0xfd => ReadUInt16(),
    0xfe => ReadUInt32(),
    0xff => ReadUInt64(),
    _ => b
};
```

## Strings

### Fixed-Length Strings

Fixed-length strings are padded with null bytes:

**Format:** `[UTF-8 bytes][zero padding]`

**Serialization:**
```csharp
var bytes = value.ToStrictUtf8Bytes();
if (bytes.Length > length)
    throw new ArgumentException();
writer.Write(bytes);
if (bytes.Length < length)
    writer.Write(new byte[length - bytes.Length]);
```

**Deserialization:**
```csharp
var end = currentOffset + length;
var offset = currentOffset;
while (offset < end && _span[offset] != 0) offset++;
var data = _span[currentOffset..offset];
for (; offset < end; offset++)
    if (_span[offset] != 0)
        throw new FormatException();
currentOffset = end;
return data.ToStrictUtf8String();
```

### Variable-Length Strings

Variable-length strings use VarInt for length prefix:

**Format:** `[VarInt length][UTF-8 bytes]`

**Serialization:**
```csharp
writer.WriteVarInt(value.Length);
writer.Write(value.ToStrictUtf8Bytes());
```

**Deserialization:**
```csharp
var length = (int)ReadVarInt((ulong)max);
EnsurePosition(length);
var data = _span.Slice(currentOffset, length);
currentOffset += length;
return data.ToStrictUtf8String();
```

## Byte Arrays

### Fixed-Length Byte Arrays

**Format:** `[raw bytes]`

### Variable-Length Byte Arrays

**Format:** `[VarInt length][raw bytes]`

**Serialization:**
```csharp
writer.WriteVarInt(value.Length);
writer.Write(value);
```

**Deserialization:**
```csharp
return ReadMemory((int)ReadVarInt((ulong)max));
```

## Collections

### Serializable Arrays

**Format:** `[VarInt count][item1][item2]...[itemN]`

**Serialization:**
```csharp
writer.WriteVarInt(value.Count);
foreach (T item in value)
{
    item.Serialize(writer);
}
```

**Deserialization:**
```csharp
var array = new T[reader.ReadVarInt((ulong)max)];
for (var i = 0; i < array.Length; i++)
{
    array[i] = new T();
    array[i].Deserialize(ref reader);
}
return array;
```

### Nullable Arrays

**Format:** `[VarInt count][bool1][item1?][bool2][item2?]...[boolN][itemN?]`

**Serialization:**
```csharp
writer.WriteVarInt(value.Length);
foreach (var item in value)
{
    var isNull = item is null;
    writer.Write(!isNull);
    if (isNull) continue;
    item!.Serialize(writer);
}
```

**Deserialization:**
```csharp
var array = new T[reader.ReadVarInt((ulong)max)];
for (var i = 0; i < array.Length; i++)
    array[i] = reader.ReadBoolean() ? reader.ReadSerializable<T>() : null;
return array;
```

## UTF-8 Encoding

Neo uses strict UTF-8 encoding with the following characteristics:

- **Strict Mode**: Invalid UTF-8 sequences throw exceptions
- **No Fallback**: No replacement characters for invalid sequences
- **Exception Handling**: Detailed error messages for debugging

**String to Bytes:**
```csharp
public static byte[] ToStrictUtf8Bytes(this string value)
{
    return StrictUTF8.GetBytes(value);
}
```

**Bytes to String:**
```csharp
public static string ToStrictUtf8String(this ReadOnlySpan<byte> value)
{
    return StrictUTF8.GetString(value);
}
```

## Error Handling

The serialization format includes comprehensive error handling:

- **FormatException**: Invalid data format or corrupted data
- **ArgumentNullException**: Null values where not allowed
- **ArgumentException**: Invalid arguments (e.g., string too long)
- **ArgumentOutOfRangeException**: Values outside allowed ranges
- **DecoderFallbackException**: Invalid UTF-8 sequences
- **EncoderFallbackException**: Characters that cannot be encoded

## Examples

### Simple Object Serialization

```csharp
public class SimpleData : ISerializable
{
    public string Name { get; set; }
    public int Value { get; set; }
    
    public int Size => Name.GetStrictUtf8ByteCount() + sizeof(int);
    
    public void Serialize(BinaryWriter writer)
    {
        writer.WriteVarString(Name);
        writer.Write(Value);
    }
    
    public void Deserialize(ref MemoryReader reader)
    {
        Name = reader.ReadVarString();
        Value = reader.ReadInt32();
    }
}
```

### Array Serialization

```csharp
public class DataArray : ISerializable
{
    public SimpleData[] Items { get; set; }
    
    public int Size => Items.Sum(item => item.Size) + GetVarSize(Items.Length);
    
    public void Serialize(BinaryWriter writer)
    {
        writer.Write(Items);
    }
    
    public void Deserialize(ref MemoryReader reader)
    {
        Items = reader.ReadSerializableArray<SimpleData>();
    }
}
```
