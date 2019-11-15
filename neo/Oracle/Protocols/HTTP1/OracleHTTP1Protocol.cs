using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Neo.Oracle.Protocols.HTTP1
{
    public class OracleHTTP1Protocol : IOracleProtocol
    {
        /// <summary>
        /// Process HTTP1 oracle request
        /// </summary>
        /// <param name="txHash">Transaction Hash</param>
        /// <param name="request">Request</param>
        /// <param name="timeout">Timeout</param>
        /// <returns>Oracle result</returns>
        public OracleResult Process(UInt256 txHash, OracleRequest request, TimeSpan timeout)
        {
            if (!(request is OracleHTTP1Request httpRequest))
            {
                return OracleResult.CreateError(txHash, request.Hash, OracleResultError.ServerError);
            }

            using (var client = new HttpClient())
            {
                Task<HttpResponseMessage> result;

                switch (httpRequest.Method)
                {
                    case OracleHTTP1Method.GET:
                        {
                            result = client.GetAsync(httpRequest.URL);
                            break;
                        }
                    case OracleHTTP1Method.DELETE:
                        {
                            result = client.DeleteAsync(httpRequest.URL);
                            break;
                        }
                    case OracleHTTP1Method.POST:
                        {
                            result = client.PostAsync(httpRequest.URL, new ByteArrayContent(httpRequest.Body));
                            break;
                        }
                    case OracleHTTP1Method.PUT:
                        {
                            result = client.PutAsync(httpRequest.URL, new ByteArrayContent(httpRequest.Body));
                            break;
                        }
                    default:
                        {
                            return OracleResult.CreateError(txHash, request.Hash, OracleResultError.PolicyError);
                        }
                }

                if (!result.Wait(timeout))
                {
                    return OracleResult.CreateError(txHash, request.Hash, OracleResultError.Timeout);
                }

                if (result.Result.IsSuccessStatusCode)
                {
                    var ret = result.Result.Content.ReadAsStringAsync();

                    if (!ret.Wait(timeout))
                    {
                        return OracleResult.CreateError(txHash, request.Hash, OracleResultError.Timeout);
                    }

                    if (!ret.IsFaulted)
                    {
                        if (!FilterResponse(ret.Result, httpRequest.Filter, out var filteredStr))
                        {
                            return OracleResult.CreateError(txHash, request.Hash, OracleResultError.FilterError);
                        }

                        return OracleResult.CreateResult(txHash, request.Hash, filteredStr);
                    }
                }

                return OracleResult.CreateError(txHash, request.Hash, OracleResultError.ServerError);
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
