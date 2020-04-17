namespace Neo.Oracle
{
    public enum OracleWalletBehaviour
    {
        /// <summary>
        /// If an Oracle syscall was found, the tx will fault
        /// </summary>
        WithoutOracle,

        /// <summary>
        /// If an Oracle syscall was found, the tx will be relayed without any check (The gas cost could be more if the result it's different)
        /// </summary>
        OracleWithoutAssert,

        /// <summary>
        /// If an Oracle syscall was found, it will be added an asert at the begining of the script
        /// </summary>
        OracleWithAssert
    }
}
