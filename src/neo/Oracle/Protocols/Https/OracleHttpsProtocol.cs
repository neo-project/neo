using Neo.Ledger;
using Neo.SmartContract.Native;
using Neo.SmartContract.Native.Oracle;
using System;
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
        public TimeSpan TimeOut { get; private set; }

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

            TimeOut = TimeSpan.FromSeconds(seconds);
        }

        // <summary>
        /// Process HTTP oracle request
        /// </summary>
        /// <param name="request">Request</param>
        /// <returns>Oracle result</returns>
        public OracleResponse Process(OracleHttpsRequest request)
        {
            LoadConfig();

            Task<HttpResponseMessage> result;
            using var client = new HttpClient();

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
                return OracleResponse.CreateError(request.Hash, OracleResultError.Timeout);
            }

            if (result.Result.IsSuccessStatusCode)
            {
                var ret = result.Result.Content.ReadAsStringAsync();

                if (!ret.Wait(TimeOut))
                {
                    return OracleResponse.CreateError(request.Hash, OracleResultError.Timeout);
                }

                if (!ret.IsFaulted)
                {
                    if (!FilterResponse(ret.Result, request.Filter, out var filteredStr))
                    {
                        return OracleResponse.CreateError(request.Hash, OracleResultError.FilterError);
                    }

                    return OracleResponse.CreateResult(request.Hash, filteredStr);
                }
            }

            return OracleResponse.CreateError(request.Hash, OracleResultError.ServerError);
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
