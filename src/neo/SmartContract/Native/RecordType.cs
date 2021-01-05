namespace Neo.SmartContract.Native
{
    public enum RecordType : byte
    {
        #region [RFC 1035](https://tools.ietf.org/html/rfc1035)
        A = 1,
        CNAME = 5,
        MX = 15,
        TXT = 16,
        #endregion

        #region [RFC 3596](https://tools.ietf.org/html/rfc3596)
        AAAA = 28,
        #endregion

        #region [RFC 2782](https://tools.ietf.org/html/rfc2782)
        SRV = 33,
        #endregion

        #region [RFC 6672](https://tools.ietf.org/html/rfc6672)
        DNAME = 39
        #endregion
    }
}
