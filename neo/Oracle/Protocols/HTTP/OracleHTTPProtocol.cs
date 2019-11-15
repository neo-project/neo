using System;
using System.Net.Http;

namespace Neo.Oracle.Protocols.HTTP
{
    public class OracleHTTPProtocol : IOracleProtocol
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
            if (!(request is OracleHTTPRequest httpRequest))
            {
                return OracleResult.CreateError(txHash, request.Hash, OracleResultError.ServerError);
            }

            HttpRequestMessage req;
            var version = new Version(httpRequest.VersionMajor, httpRequest.VersionMinor);

            using (var client = new HttpClient())
            {
                switch (httpRequest.Method)
                {
                    case OracleHTTPRequest.HTTPMethod.GET:
                        {
                            req = new HttpRequestMessage(HttpMethod.Get, httpRequest.URL)
                            {
                                Version = version
                            };
                            break;
                        }
                    case OracleHTTPRequest.HTTPMethod.DELETE:
                        {
                            req = new HttpRequestMessage(HttpMethod.Delete, httpRequest.URL)
                            {
                                Version = version
                            };
                            break;
                        }
                    case OracleHTTPRequest.HTTPMethod.POST:
                        {
                            req = new HttpRequestMessage(HttpMethod.Post, httpRequest.URL)
                            {
                                Version = version,
                                Content = new ByteArrayContent(httpRequest.Body)
                            };
                            break;
                        }
                    case OracleHTTPRequest.HTTPMethod.PUT:
                        {
                            req = new HttpRequestMessage(HttpMethod.Put, httpRequest.URL)
                            {
                                Version = version,
                                Content = new ByteArrayContent(httpRequest.Body)
                            };
                            break;
                        }
                    default:
                        {
                            return OracleResult.CreateError(txHash, request.Hash, OracleResultError.PolicyError);
                        }
                }

                var result = client.SendAsync(req);

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
                        if (!OracleFilters.FilterContent(result.Result.Content.Headers.ContentType,
                            ret.Result, httpRequest.Filter, out var filteredStr))
                        {
                            return OracleResult.CreateError(txHash, request.Hash, OracleResultError.FilterError);
                        }

                        return OracleResult.CreateResult(txHash, request.Hash, filteredStr);
                    }
                }

                return OracleResult.CreateError(txHash, request.Hash, OracleResultError.ServerError);
            }
        }
    }
}
