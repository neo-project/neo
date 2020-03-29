using Neo.Ledger;
using Neo.SmartContract.Native;
using Neo.SmartContract.Native.Oracle;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Oracle.Protocols.Https
{
    internal class OracleHTTPProtocol
    {
        private long _lastHeight = -1;

        /// <summary>
        /// Timeout
        /// </summary>
        public TimeSpan TimeOut { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public OracleHTTPProtocol()
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

            short seconds;
            using (var snapshot = Blockchain.Singleton.GetSnapshot())
            {
                seconds = (short)NativeContract.Oracle.GetConfig(snapshot, HttpConfig.Timeout).ToBigInteger();
            }

            TimeOut = TimeSpan.FromSeconds(seconds);
        }

        // <summary>
        /// Process HTTP oracle request
        /// </summary>
        /// <param name="request">Request</param>
        /// <returns>Oracle result</returns>
        public OracleResult Process(OracleHttpsRequest request)
        {
            LoadConfig();

            using (var client = new HttpClient())
            {
                Task<HttpResponseMessage> result;

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
                            return OracleResult.CreateError(UInt256.Zero, request.Hash, OracleResultError.PolicyError);
                        }
                }

                if (!result.Wait(TimeOut))
                {
                    return OracleResult.CreateError(UInt256.Zero, request.Hash, OracleResultError.Timeout);
                }

                if (result.Result.IsSuccessStatusCode)
                {
                    var ret = result.Result.Content.ReadAsStringAsync();

                    if (!ret.Wait(TimeOut))
                    {
                        return OracleResult.CreateError(UInt256.Zero, request.Hash, OracleResultError.Timeout);
                    }

                    if (!ret.IsFaulted)
                    {
                        if (!FilterResponse(ret.Result, request.Filter, out var filteredStr))
                        {
                            return OracleResult.CreateError(UInt256.Zero, request.Hash, OracleResultError.FilterError);
                        }

                        return OracleResult.CreateResult(UInt256.Zero, request.Hash, filteredStr);
                    }
                }

                return OracleResult.CreateError(UInt256.Zero, request.Hash, OracleResultError.ServerError);
            }
        }

        private bool FilterResponse(string input, string filter, out string filtered)
        {
            // TODO: Filter
            //filtered = "";
            //return false;

            filtered = input;
            return true;
        }
    }
}
