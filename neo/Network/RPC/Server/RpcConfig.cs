using Microsoft.Extensions.Configuration;
using System.IO;
using System.Net;

namespace Neo.Network.RPC.Server
{
    public class RpcConfig
    {
        public class SslCert
        {
            /// <summary>
            /// Path
            /// </summary>
            public string Path { get; internal set; }
            /// <summary>
            /// Password
            /// </summary>
            public string Password { get; internal set; }

            /// <summary>
            /// Is valid?
            /// </summary>
            public bool IsValid => !string.IsNullOrEmpty(Path) && !string.IsNullOrEmpty(Password) && File.Exists(Path);
        }

        /// <summary>
        /// Listen end point
        /// </summary>
        public IPEndPoint ListenEndPoint { get; internal set; }

        /// <summary>
        /// SSL config
        /// </summary>
        public SslCert Ssl { get; internal set; }

        public string[] TrustedAuthorities { get; internal set; }

        public IPAddress[] IpBlacklist { get; internal set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configuration">Configuration</param>
        public RpcConfig(IConfiguration configuration = null)
        {
            configuration?.GetSection("rpc")?.Bind(this);
        }
    }
}
