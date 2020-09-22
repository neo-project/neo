namespace Neo.Models
{
    public enum OracleResponseCode : byte
    {
        Success = 0x00,

        NotFound = 0x10,
        Timeout = 0x12,
        Forbidden = 0x14,

        Error = 0xff
    }
}
