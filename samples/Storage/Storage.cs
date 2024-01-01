using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using System.ComponentModel;
using Neo;
using Neo.Cryptography.ECC;
using Neo.SmartContract.Framework.Services;

namespace Storage
{
    [DisplayName("SampleStorage")]
    [ManifestExtra("Author", "code-dev")]
    [ManifestExtra("Description", "A sample contract to demonstrate how to use storage")]
    [ManifestExtra("Email", "core@neo.org")]
    [ManifestExtra("Version", "0.0.1")]
    [ContractSourceCode("https://github.com/neo-project/samples/Storage")]
    [ContractPermission("*", "*")]
    public class SampleStorage : SmartContract
    {
        #region Byte

        public static bool TestPutByte(byte[] key, byte[] value)
        {
            var storage = new StorageMap(Neo.SmartContract.Framework.Services.Storage.CurrentContext, 0x11);
            storage.Put((ByteString)key, (ByteString)value);
            return true;
        }

        public static void TestDeleteByte(byte[] key)
        {
            var storage = new StorageMap(Neo.SmartContract.Framework.Services.Storage.CurrentContext, 0x11);
            storage.Delete((ByteString)key);
        }

        public static byte[] TestGetByte(byte[] key)
        {
            var context = Neo.SmartContract.Framework.Services.Storage.CurrentReadOnlyContext;
            var storage = new StorageMap(context, 0x11);
            var value = storage.Get((ByteString)key);
            return (byte[])value;
        }

        public static byte[] TestOver16Bytes()
        {
            var value = new byte[] { 0x3b, 0x00, 0x32, 0x03, 0x23, 0x23, 0x23, 0x23, 0x02, 0x23, 0x23, 0x02, 0x23, 0x23, 0x02, 0x23, 0x23, 0x02, 0x23, 0x23, 0x02, 0x23, 0x23, 0x02 };
            StorageMap storageMap = new StorageMap(Neo.SmartContract.Framework.Services.Storage.CurrentContext, "test_map");
            storageMap.Put((ByteString)new byte[] { 0x01 }, (ByteString)value);
            return (byte[])storageMap.Get((ByteString)new byte[] { 0x01 });
        }

        #endregion

        #region String

        public static bool TestPutString(byte[] key, byte[] value)
        {
            var prefix = "aa";
            var storage = new StorageMap(Neo.SmartContract.Framework.Services.Storage.CurrentContext, prefix);
            storage.Put((ByteString)key, (ByteString)value);
            return true;
        }

        public static void TestDeleteString(byte[] key)
        {
            var prefix = "aa";
            var storage = new StorageMap(Neo.SmartContract.Framework.Services.Storage.CurrentContext, prefix);
            storage.Delete((ByteString)key);
        }

        public static byte[] TestGetString(byte[] key)
        {
            var prefix = "aa";
            var context = Neo.SmartContract.Framework.Services.Storage.CurrentReadOnlyContext;
            var storage = new StorageMap(context, prefix);
            var value = storage.Get((ByteString)key);
            return (byte[])value;
        }

        #endregion

        #region ByteArray

        public static bool TestPutByteArray(byte[] key, byte[] value)
        {
            var prefix = new byte[] { 0x00, 0xFF };
            var storage = new StorageMap(Neo.SmartContract.Framework.Services.Storage.CurrentContext, prefix);
            storage.Put((ByteString)key, (ByteString)value);
            return true;
        }

        public static void TestDeleteByteArray(byte[] key)
        {
            var prefix = new byte[] { 0x00, 0xFF };
            var storage = new StorageMap(Neo.SmartContract.Framework.Services.Storage.CurrentContext, prefix);
            storage.Delete((ByteString)key);
        }

        public static byte[] TestGetByteArray(byte[] key)
        {
            var prefix = new byte[] { 0x00, 0xFF };
            var context = Neo.SmartContract.Framework.Services.Storage.CurrentContext.AsReadOnly;
            var storage = new StorageMap(context, prefix);
            var value = storage.Get((ByteString)key);
            return (byte[])value;
        }

        public static bool TestNewGetMethods()
        {
            var prefix = new byte[] { 0x00, 0xFF };
            var context = Neo.SmartContract.Framework.Services.Storage.CurrentContext;
            var storage = new StorageMap(context, prefix);

            var boolValue = true;
            var intValue = 123;
            var stringValue = "hello world";
            var uint160Value = (UInt160)new byte[] {
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09,
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09
            };
            var uint256Value = (UInt256)new byte[] {
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09,
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09,
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09,
                0x00, 0x01
            };
            var ecPointValue = (ECPoint)new byte[] {
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09,
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09,
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09,
                0x00, 0x01, 0x02
            };

            storage.Put("bool", boolValue);
            storage.Put("int", intValue);
            storage.Put("string", stringValue);
            storage.Put("uint160", uint160Value);
            storage.Put("uint256", uint256Value);
            storage.Put("ecpoint", ecPointValue);

            var boolValue2 = storage.GetBoolean("bool");
            var intValue2 = storage.GetInteger("int");
            var stringValue2 = storage.GetString("string");
            var uint160Value2 = storage.GetUInt160("uint160");
            var uint256Value2 = storage.GetUInt256("uint256");
            var ecPointValue2 = storage.GetECPoint("ecpoint");

            return boolValue == boolValue2
                && intValue == intValue2
                && stringValue == stringValue2
                && uint160Value == uint160Value2
                && uint256Value == uint256Value2
                && ecPointValue == ecPointValue2;
        }

        public static byte[] TestNewGetByteArray()
        {
            var prefix = new byte[] { 0x00, 0xFF };
            var context = Neo.SmartContract.Framework.Services.Storage.CurrentContext;
            var storage = new StorageMap(context, prefix);
            var byteArray = new byte[] { 0x00, 0x01 };
            storage.Put("byteArray", byteArray);
            var byteArray2 = storage.GetByteArray("byteArray");
            return byteArray2;
        }

        #endregion

        public static bool TestPutReadOnly(byte[] key, byte[] value)
        {
            var prefix = new byte[] { 0x00, 0xFF };
            var context = Neo.SmartContract.Framework.Services.Storage.CurrentContext.AsReadOnly;
            var storage = new StorageMap(context, prefix);
            storage.Put((ByteString)key, (ByteString)value);
            return true;
        }

        #region Serialize

        class Value
        {
            public int Val;
        }

        public static int SerializeTest(byte[] key, int value)
        {
            var prefix = new byte[] { 0x01, 0xAA };
            var context = Neo.SmartContract.Framework.Services.Storage.CurrentContext;
            var storage = new StorageMap(context, prefix);
            var val = new Value() { Val = value };
            storage.PutObject(key, val);
            val = (Value)storage.GetObject(key);
            return val.Val;
        }

        #endregion

        #region Find

        public static byte[] TestFind()
        {
            var context = Neo.SmartContract.Framework.Services.Storage.CurrentContext;
            Neo.SmartContract.Framework.Services.Storage.Put(context, (ByteString)"key1", (ByteString)new byte[] { 0x01 });
            Neo.SmartContract.Framework.Services.Storage.Put(context, (ByteString)"key2", (ByteString)new byte[] { 0x02 });
            Iterator<byte[]> iterator = (Iterator<byte[]>)Neo.SmartContract.Framework.Services.Storage.Find(context, "key", FindOptions.ValuesOnly);
            iterator.Next();
            return iterator.Value;
        }

        #endregion

        #region IndexProperty

        public static bool TestIndexPut(byte[] key, byte[] value)
        {
            var prefix = "ii";
            var storage = new StorageMap(Neo.SmartContract.Framework.Services.Storage.CurrentContext, prefix);
            storage[(ByteString)key] = (ByteString)value;
            return true;
        }

        public static byte[] TestIndexGet(byte[] key)
        {
            var prefix = "ii";
            var context = Neo.SmartContract.Framework.Services.Storage.CurrentReadOnlyContext;
            var storage = new StorageMap(context, prefix);
            var value = storage[(ByteString)key];
            return (byte[])value;
        }

        #endregion
    }
}
