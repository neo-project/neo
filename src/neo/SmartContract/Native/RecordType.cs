namespace Neo.SmartContract.Native
{
    /// <summary>
    /// Represents the type of a name record.
    /// </summary>
    public enum RecordType : byte
    {
        #region [RFC 1035](https://tools.ietf.org/html/rfc1035)

        /// <summary>
        /// Address record.
        /// </summary>
        A = 1,

        /// <summary>
        /// Canonical name record.
        /// </summary>
        CNAME = 5,

        /// <summary>
        /// Text record.
        /// </summary>
        TXT = 16,

        #endregion

        #region [RFC 3596](https://tools.ietf.org/html/rfc3596)

        /// <summary>
        /// IPv6 address record.
        /// </summary>
        AAAA = 28,

        #endregion
    }
}
