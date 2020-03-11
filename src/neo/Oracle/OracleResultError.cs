namespace Neo.Oracle
{
    public enum OracleResultError : byte
    {
        /// <summary>
        /// There was no errors
        /// </summary>
        None = 0x00,

        /// <summary>
        /// Timeout
        /// </summary>
        Timeout = 0x01,

        /// <summary>
        /// There was an error with the server
        /// </summary>
        ServerError = 0x02,

        /// <summary>
        /// There was an error with the policy
        /// </summary>
        PolicyError = 0x03,

        /// <summary>
        /// There was an error with the filter
        /// </summary>
        FilterError = 0x04,

        /// <summary>
        /// Unrecognized format
        /// </summary>
        ResponseError = 0x05
    }
}
