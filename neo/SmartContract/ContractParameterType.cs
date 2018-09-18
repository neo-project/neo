namespace Neo.SmartContract
{
    public enum ContractParameterType : byte
    {
        Signature = 0x00,
        Boolean = 0x01,
        Integer = 0x02,
        Hash160 = 0x03,
        Hash256 = 0x04,
        ByteArray = 0x05,
        PublicKey = 0x06,
        String = 0x07,

        Array = 0x10,
        Map = 0x12,

        InteropInterface = 0xf0,

        Void = 0xff
    }
}
