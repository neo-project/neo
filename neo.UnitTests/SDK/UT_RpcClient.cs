using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using Neo.IO.Json;
using Neo.Network.RPC;
using Neo.SDK;
using Neo.SDK.RPC;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.UnitTests.SDK
{
    [TestClass]
    public class UT_RpcClient
    {
        RpcClient rpc;
        Mock<HttpMessageHandler> handlerMock;

        [TestInitialize]
        public void TestSetup()
        {
            handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

            // use real http client with mocked handler here
            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("http://seed1.neo.org:10331"),
            };

            var helper = new HttpService(httpClient);
            rpc = new RpcClient(helper);
        }

        private void MockResponse(string content)
        {
            handlerMock
               .Protected()
               // Setup the PROTECTED method to mock
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               )
               // prepare the expected response of the mocked http call
               .ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = HttpStatusCode.OK,
                   Content = new StringContent(content),
               })
               .Verifiable();
        }

        [TestMethod]
        public void TestErrorResponse()
        {
            JObject response = RpcServer.CreateErrorResponse(null, -32700, "Parse error", "something");
            MockResponse(response.ToString());
            try
            {
                var result = rpc.GetBlockHex("773dd2dae4a9c9275290f89b56e67d7363ea4826dfd4fc13cc01cf73a44b0d0e");
            }
            catch (NeoSdkException ex)
            {
                Assert.AreEqual(-32700, ex.HResult);
                Assert.AreEqual("Parse error", ex.Message);
                Assert.AreEqual("something", ex.Data["data"]);
            }
        }

        [TestMethod]
        public void TestGetBestBlockHash()
        {
            JObject response = RpcServer.CreateResponse(1);
            response["result"] = "000000002deadfa82cbc4682f5800";
            MockResponse(response.ToString());

            var result = rpc.GetBestBlockHash();
            Assert.AreEqual("000000002deadfa82cbc4682f5800", result);
        }

        [TestMethod]
        public void TestGetBlockHex()
        {
            JObject response = RpcServer.CreateResponse(1);
            response["result"] = "000000002deadfa82cbc4682f5800";
            MockResponse(response.ToString());

            var result = rpc.GetBlockHex("773dd2dae4a9c9275290f89b56e67d7363ea4826dfd4fc13cc01cf73a44b0d0e");
            Assert.AreEqual("000000002deadfa82cbc4682f5800", result);
        }


    }



}
