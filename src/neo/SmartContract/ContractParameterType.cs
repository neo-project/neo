namespace Neo.SmartContract
{
    public enum ContractParameterType : byte
    {
        Any = 0x00,

        Boolean = 0x10,
        Integer = 0x11,
        ByteArray = 0x12,
        String = 0x13,
        Hash160 = 0x14,
        Hash256 = 0x15,
        PublicKey = 0x16,
        Signature = 0x17,

        Array = 0x20,
        Map = 0x22,

        InteropInterface = 0x30,

        Void = 0xff
    }
}
