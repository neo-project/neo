using Neo.Ledger;
using Neo.SmartContract.Native;
using Neo.SmartContract.Native.Oracle;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Oracle.Protocols.Https
{
    internal class OracleHttpsProtocol
    {
        private long _lastHeight = -1;

        /// <summary>
        /// Timeout
        /// </summary>
        public TimeSpan TimeOut { get; internal set; }

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

            ushort seconds;
            using (var snapshot = Blockchain.Singleton.GetSnapshot())
            {
                seconds = (ushort)NativeContract.Oracle.GetConfig(snapshot, HttpConfig.Timeout).ToBigInteger();
            }

            TimeOut = TimeSpan.FromMilliseconds(seconds);
        }

        // <summary>
        /// Process HTTP oracle request
        /// </summary>
        /// <param name="request">Request</param>
        /// <returns>Oracle result</returns>
        public OracleResponse Process(OracleHttpsRequest request)
        {
            LoadConfig();

            if (!AllowPrivateHost && IsPrivateHost(Dns.GetHostEntry(request.URL.Host)))
            {
                // Don't allow private host in order to prevent SSRF

                return OracleResponse.CreateError(request.Hash, OracleResultError.PolicyError);
            }

            Task<HttpResponseMessage> result;
            using var handler = new HttpClientHandler
            {
                // TODO: Accept all certificates
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
            using var client = new HttpClient(handler);

            switch (request.Method)
            {
                case HttpMethod.GET:
                    {
                        result = client.GetAsync(request.URL);
                        break;
                    }
                //case HttpMethod.POST:
                //    {
                //        result = client.PostAsync(httpRequest.URL, new ByteArrayContent(httpRequest.Body));
                //        break;
                //    }
                //case HttpMethod.PUT:
                //    {
                //        result = client.PutAsync(httpRequest.URL, new ByteArrayContent(httpRequest.Body));
                //        break;
                //    }
                //case HttpMethod.DELETE:
                //    {
                //        result = client.DeleteAsync(httpRequest.URL);
                //        break;
                //    }
                default:
                    {
                        return OracleResponse.CreateError(request.Hash, OracleResultError.PolicyError);
                    }
            }

            if (!result.Wait(TimeOut))
            {
                // Timeout

                return OracleResponse.CreateError(request.Hash, OracleResultError.Timeout);
            }

            if (!result.Result.IsSuccessStatusCode)
            {
                // Error with response

                return OracleResponse.CreateError(request.Hash, OracleResultError.ResponseError);
            }

            string ret;
            var taskRet = result.Result.Content.ReadAsStringAsync();

            if (!taskRet.Wait(TimeOut))
            {
                // Timeout

                return OracleResponse.CreateError(request.Hash, OracleResultError.Timeout);
            }
            else
            {
                // Good response

                ret = taskRet.Result;
            }

            // Filter

            if (!OracleService.FilterResponse(ret, request.Filter, out string filteredStr))
            {
                return OracleResponse.CreateError(request.Hash, OracleResultError.FilterError);
            }

            return OracleResponse.CreateResult(request.Hash, filteredStr);
        }

        private bool IsPrivateHost(IPHostEntry entry)
        {
            foreach (var ip in entry.AddressList)
            {
                if (IPAddress.IsLoopback(ip)) return true;
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
        public bool IsInternal(IPAddress ipAddress)
        {
            if (ipAddress.ToString() == "::1") return true;

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
