using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
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
        public void TestGetAccountState()
        {
            MockResponse(@"{
    ""jsonrpc"": ""2.0"",
    ""id"": 1,
    ""result"": {
                ""version"": 0,
        ""script_hash"": ""0x1179716da2e9523d153a35fb3ad10c561b1e5b1a"",
        ""frozen"": false,
        ""votes"": [],
        ""balances"": [
            {
                ""asset"": ""0xc56f33fc6ecfcd0c225c4ab356fee59390af8560be0e930faebe74a6daff7c9b"",
                ""value"": ""94""
            }
        ]
    }
    }");
            var response = rpc.GetAccountState("AJBENSwajTzQtwyJFkiJSv7MAaaMc7DsRz");
            Assert.AreEqual("0x1179716da2e9523d153a35fb3ad10c561b1e5b1a", response.ScriptHash);
        }


    }



}
