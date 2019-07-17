using Neo.IO.Json;
using Neo.SDK.RPC.Model;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Neo.SDK.RPC
{
    public class HttpService : IDisposable
    {
        private readonly HttpClient httpClient;

        public HttpService(string url)
        {
            httpClient = new HttpClient() { BaseAddress = new Uri(url) };
        }

        public HttpService(HttpClient client)
        {
            httpClient = client;
        }

        public void Dispose()
        {
            httpClient?.Dispose();
        }

        public async Task<RpcResponse> SendAsync(RpcRequest request)
        {
            var requestJson = request.ToJson().ToString();
            var result = await httpClient.PostAsync(httpClient.BaseAddress, new StringContent(requestJson, Encoding.UTF8));
            var content = await result.Content.ReadAsStringAsync();
            var response = RpcResponse.FromJson(JObject.Parse(content));
            response.RawResponse = content;

            if (response.Error != null)
            {
                throw new NeoSdkException(response.Error.Code, response.Error.Message, response.Error.Data);
            }

            return response;
        }

        public RpcResponse Send(RpcRequest request)
        {
            try
            {
                return SendAsync(request).Result;
            }
            catch (AggregateException ex)
            {
                throw ex.GetBaseException();
            }
        }

    }


}
