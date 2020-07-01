namespace Neo.SmartContract.Native.Tokens
{
    public enum RequestStatusType : byte
    {
        Request = 0x00,
        Ready = 0x01,
        Successed = 0x02,
        Failed = 0x03
    }
}
