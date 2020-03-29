using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Neo.Oracle.Protocols.Https
{
    internal class OracleHTTPProtocol
    {
        // <summary>
        /// Process HTTP oracle request
        /// </summary>
        /// <param name="request">Request</param>
        /// <param name="timeout">Timeouts</param>
        /// <returns>Oracle result</returns>
        public OracleResult Process(OracleHttpsRequest request, TimeSpan timeout)
        {
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

                if (!result.Wait(timeout))
                {
                    return OracleResult.CreateError(UInt256.Zero, request.Hash, OracleResultError.Timeout);
                }

                if (result.Result.IsSuccessStatusCode)
                {
                    var ret = result.Result.Content.ReadAsStringAsync();

                    if (!ret.Wait(timeout))
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
