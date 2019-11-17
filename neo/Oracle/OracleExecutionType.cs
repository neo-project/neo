namespace Neo.Oracle
{
    public enum OracleExecutionType : byte
    {
        /// <summary>
        /// This option will fail if an oracle SYSCALL is called
        /// </summary>
        WithoutOracles = 0x00,

        /// <summary>
        /// This option will attach the expected result hash of the oracles, ensuring that the result is the same as the user.
        /// </summary>
        SecureOracle = 0x01,

        /// <summary>
        /// The user doesn't download any information during the execution of the contract because they don't want to consume the original sources.
        /// Any hash will be accepted, but only one different oracle request will be allowed, also the user must add enought gast because it will be impossible to compute the required gas amount.
        /// </summary>
        UnsecureOracle = 0x02
    }
}
