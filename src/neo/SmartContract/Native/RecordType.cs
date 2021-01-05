namespace Neo.SmartContract.Native
{
    public enum RecordType : byte
    {
        #region [RFC 1035](https://tools.ietf.org/html/rfc1035)
        A = 1,
        CNAME = 5,
        TXT = 16,
        #endregion

        #region [RFC 3596](https://tools.ietf.org/html/rfc3596)
        AAAA = 28,
        #endregion
    }
}
