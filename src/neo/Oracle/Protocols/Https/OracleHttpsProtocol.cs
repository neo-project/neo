using Neo.Ledger;
using Neo.SmartContract.Native;
using Neo.SmartContract.Native.Oracle;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Oracle.Protocols.Https
{
    internal class OracleHttpsProtocol
    {
        private long _lastHeight = -1;

        /// <summary>
        /// Config
        /// </summary>
        public HttpConfig Config { get; internal set; }

        /// <summary>
        /// Allow private host
        /// </summary>
        public bool AllowPrivateHost { get; internal set; } = false;

        /// <summary>
        /// Constructor
        /// </summary>
        public OracleHttpsProtocol()
        {
            LoadConfig();
        }

        /// <summary>
        /// Load config
        /// </summary>
        private void LoadConfig()
        {
            // Check if it's the same

            var height = Blockchain.Singleton.Height;
            if (Interlocked.Exchange(ref _lastHeight, height) == height)
            {
                return;
            }

            // Load the configuration

            using (var snapshot = Blockchain.Singleton.GetSnapshot())
            {
                Config = NativeContract.Oracle.GetHttpConfig(snapshot);
            }
        }

        // <summary>
        /// Process HTTP oracle request
        /// </summary>
        /// <param name="request">Request</param>
        /// <returns>Oracle result</returns>
        public OracleResponse Process(OracleHttpsRequest request)
        {
            Log($"Downloading HTTPS request: url={request.URL.ToString()} method={request.Method}", LogLevel.Debug);
            LoadConfig();

            if (!AllowPrivateHost && IsInternal(Dns.GetHostEntry(request.URL.Host)))
            {
                // Don't allow private host in order to prevent SSRF

                LogError(request.URL, "PolicyError");
                return OracleResponse.CreateError(request.Hash);
            }

            Task<HttpResponseMessage> result;
            using var handler = new HttpClientHandler
            {
                // TODO: Accept all certificates
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
            using var client = new HttpClient(handler);

            client.DefaultRequestHeaders.Add("Accept", string.Join(",", HttpConfig.AllowedFormats));

            switch (request.Method)
            {
                case HttpMethod.GET:
                    {
                        result = client.GetAsync(request.URL);
                        break;
                    }
                default:
                    {
                        LogError(request.URL, "PolicyError");
                        return OracleResponse.CreateError(request.Hash);
                    }
            }

            if (!result.Wait(Config.TimeOut))
            {
                // Timeout

                LogError(request.URL, "Timeout");
                return OracleResponse.CreateError(request.Hash);
            }

            if (!result.Result.IsSuccessStatusCode)
            {
                // Error with response

                LogError(request.URL, "ResponseError");
                return OracleResponse.CreateError(request.Hash);
            }

            if (!HttpConfig.AllowedFormats.Contains(result.Result.Content.Headers.ContentType.MediaType))
            {
                // Error with the ContentType

                LogError(request.URL, "ContentType it's not allowed");
                return OracleResponse.CreateError(request.Hash);
            }

            string ret;
            var taskRet = result.Result.Content.ReadAsStringAsync();

            if (!taskRet.Wait(Config.TimeOut))
            {
                // Timeout

                LogError(request.URL, "Timeout");
                return OracleResponse.CreateError(request.Hash);
            }
            else
            {
                // Good response

                ret = taskRet.Result;
            }

            // Filter

            using var snapshot = Blockchain.Singleton.GetSnapshot();

            if (!OracleFilter.Filter(snapshot, request.Filter, Encoding.UTF8.GetBytes(ret), out var output, out var gasCost))
            {
                LogError(request.URL, "FilterError");
                return OracleResponse.CreateError(request.Hash, gasCost);
            }

            return OracleResponse.CreateResult(request.Hash, output, gasCost);
        }

        /// <summary>
        /// Log error
        /// </summary>
        /// <param name="url">Url</param>
        /// <param name="error">Error</param>
        private static void LogError(Uri url, string error)
        {
            Log($"{error} at {url.ToString()}", LogLevel.Error);
        }

        /// <summary>
        /// Log
        /// </summary>
        /// <param name="line">Line</param>
        /// <param name="level">Level</param>
        private static void Log(string line, LogLevel level)
        {
            Utility.Log(nameof(OracleHttpsProtocol), level, line);
        }

        internal static bool IsInternal(IPHostEntry entry)
        {
            foreach (var ip in entry.AddressList)
            {
                if (IsInternal(ip)) return true;
            }

            return false;
        }

        /// <summary>
        ///       ::1          -   IPv6  loopback
        ///       10.0.0.0     -   10.255.255.255  (10/8 prefix)
        ///       127.0.0.0    -   127.255.255.255  (127/8 prefix)
        ///       172.16.0.0   -   172.31.255.255  (172.16/12 prefix)
        ///       192.168.0.0  -   192.168.255.255 (192.168/16 prefix)
        /// </summary>
        /// <param name="ipAddress">Address</param>
        /// <returns>True if it was an internal address</returns>
        internal static bool IsInternal(IPAddress ipAddress)
        {
            if (IPAddress.IsLoopback(ipAddress)) return true;
            if (IPAddress.Broadcast.Equals(ipAddress)) return true;
            if (IPAddress.Any.Equals(ipAddress)) return true;
            if (IPAddress.IPv6Any.Equals(ipAddress)) return true;
            if (IPAddress.IPv6Loopback.Equals(ipAddress)) return true;

            var ip = ipAddress.GetAddressBytes();
            switch (ip[0])
            {
                case 10:
                case 127: return true;
                case 172: return ip[1] >= 16 && ip[1] < 32;
                case 192: return ip[1] == 168;
                default: return false;
            }
        }
    }
}
