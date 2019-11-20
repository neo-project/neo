using System;

namespace Neo.Oracle
{
    public class OraclePolicy
    {
        /// <summary>
        /// Timeout for serve one request
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(3);
    }
}
