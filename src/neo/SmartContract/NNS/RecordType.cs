namespace Neo.SmartContract.NNS
{
    public enum RecordType : byte
    {
        A = 0x00,
        CNAME = 0x01,
        TXT = 0x02,
        NS = 0x03,
        ERROR = 0x04
    }
}
