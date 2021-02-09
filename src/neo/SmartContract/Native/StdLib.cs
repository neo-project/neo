#pragma warning disable IDE0051

using Neo.Cryptography;
using Neo.IO.Json;
using Neo.VM.Types;
using System;
using System.Globalization;
using System.Numerics;

namespace Neo.SmartContract.Native
{
    public sealed class StdLib : NativeContract
    {
        internal StdLib() { }

        [ContractMethod(1 << 12, CallFlags.None)]
        private static byte[] Serialize(ApplicationEngine engine, StackItem item)
        {
            return BinarySerializer.Serialize(item, engine.Limits.MaxItemSize);
        }

        [ContractMethod(1 << 14, CallFlags.None)]
        private static StackItem Deserialize(ApplicationEngine engine, byte[] data)
        {
            return BinarySerializer.Deserialize(data, engine.Limits.MaxStackSize, engine.ReferenceCounter);
        }

        [ContractMethod(1 << 12, CallFlags.None)]
        private static byte[] JsonSerialize(ApplicationEngine engine, StackItem item)
        {
            return JsonSerializer.SerializeToByteArray(item, engine.Limits.MaxItemSize);
        }

        [ContractMethod(1 << 14, CallFlags.None)]
        private static StackItem JsonDeserialize(ApplicationEngine engine, byte[] json)
        {
            return JsonSerializer.Deserialize(JObject.Parse(json, 10), engine.ReferenceCounter);
        }

        [ContractMethod(1 << 12, CallFlags.None)]
        public static string Itoa(BigInteger value, int @base)
        {
            return @base switch
            {
                10 => value.ToString(),
                16 => value.ToString("x"),
                _ => throw new ArgumentOutOfRangeException(nameof(@base))
            };
        }

        [ContractMethod(1 << 12, CallFlags.None)]
        public static BigInteger Atoi(string value, int @base)
        {
            return @base switch
            {
                10 => BigInteger.Parse(value),
                16 => BigInteger.Parse(value, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture),
                _ => throw new ArgumentOutOfRangeException(nameof(@base))
            };
        }

        [ContractMethod(1 << 12, CallFlags.None)]
        public static string Base64Encode(byte[] data)
        {
            return Convert.ToBase64String(data);
        }

        [ContractMethod(1 << 12, CallFlags.None)]
        public static byte[] Base64Decode(string s)
        {
            return Convert.FromBase64String(s);
        }

        [ContractMethod(1 << 12, CallFlags.None)]
        public static string Base58Encode(byte[] data)
        {
            return Base58.Encode(data);
        }

        [ContractMethod(1 << 12, CallFlags.None)]
        public static byte[] Base58Decode(string s)
        {
            return Base58.Decode(s);
        }
    }
}
