using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P.Payloads;
using Neo.Oracle;
using Neo.Oracle.Protocols.HTTP;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.VM;
using System;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.UnitTests.Oracle
{
    [TestClass]
    public class UT_OracleService
    {
        Store store;
        IWebHost server;

        [TestInitialize]
        public void Init()
        {
            TestBlockchain.InitializeMockNeoSystem();
            store = TestBlockchain.GetStore();

            server = new WebHostBuilder().UseKestrel(options => options.Listen(IPAddress.Any, 9898))
            .Configure(app =>
            {
                app.UseResponseCompression();
                app.Run(ProcessAsync);
            })
            .ConfigureServices(services =>
            {
                services.AddResponseCompression(options =>
                {
                    // options.EnableForHttps = false;
                    options.Providers.Add<GzipCompressionProvider>();
                    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/json-rpc" });
                });

                services.Configure<GzipCompressionProviderOptions>(options =>
                {
                    options.Level = CompressionLevel.Fastest;
                });
            })
            .Build();

            server.Start();
        }

        [TestCleanup]
        public void Clean()
        {
            server.StopAsync().Wait();
            server.Dispose();
        }

        private async Task ProcessAsync(HttpContext context)
        {
            var response = "";
            context.Response.ContentType = "text/plain";

            switch (context.Request.Path.Value)
            {
                case "/xPath":
                    {
                        context.Response.ContentType = "text/xml";
                        response =
                            @"<?xml version=""1.0"" encoding=""UTF-8""?>
<bookstore>
<book category=""cooking"">
  <title lang=""en"">Everyday Italian</title>
  <author>Giada De Laurentiis</author>
  <year>2005</year>
  <price>30.00</price>
</book>

<book category=""children"">
  <title lang=""en"">Harry Potter</title>
  <author>J K. Rowling</author>
  <year>2005</year>
  <price>29.99</price>
</book>
</bookstore>";
                        break;
                    }
                case "/json":
                    {
                        context.Response.ContentType = "application/json";
                        response =
@"{
  'Stores': [
    'Lambton Quay',
    'Willis Street'
  ],
  'Manufacturers': [
    {
      'Name': 'Acme Co',
      'Products': [
        {
          'Name': 'Anvil',
          'Price': 50
        }
      ]
    },
    {
      'Name': 'Contoso',
      'Products': [
        {
          'Name': 'Elbow Grease',
          'Price': 99.95
        },
        {
          'Name': 'Headlight Fluid',
          'Price': 4
        }
      ]
    }
  ]
}
";
                        break;
                    }
                case "/text":
                    {
                        response = @"Mahesh Chand, Raj Kumar, Mike Gold, Allen O'Neill, Marshal Troll";
                        break;
                    }
                case "/helloWorld":
                    {
                        response = "Hello world!";
                        break;
                    }
                case "/timeout":
                    {
                        Thread.Sleep(2100);
                        break;
                    }
                case "/delete":
                    {
                        if (context.Request.Method != "DELETE")
                        {
                            context.Response.StatusCode = 404;
                            break;
                        }

                        response = "true";
                        break;
                    }
                case "/put":
                    {
                        if (context.Request.Method != "PUT")
                        {
                            context.Response.StatusCode = 404;
                            break;
                        }

                        var read = new byte[4096];
                        Array.Resize(ref read, context.Request.Body.Read(read, 0, read.Length));
                        response = Encoding.UTF8.GetString(read);
                        break;
                    }
                case "/post":
                    {
                        if (context.Request.Method != "POST")
                        {
                            context.Response.StatusCode = 404;
                            break;
                        }

                        var read = new byte[4096];
                        Array.Resize(ref read, context.Request.Body.Read(read, 0, read.Length));
                        response = Encoding.UTF8.GetString(read);
                        break;
                    }
                case "/error":
                    {
                        context.Response.StatusCode = 503;
                        break;
                    }
                default:
                    {
                        context.Response.StatusCode = 404;
                        break;
                    }
            }

            await context.Response.WriteAsync(response, Encoding.UTF8);
        }

        [TestMethod]
        public void Test_HTTP_Filter_XPath()
        {
            var request = new OracleHTTPRequest()
            {
                Version = OracleHTTPRequest.HTTPVersion.v1_1,
                Method = OracleHTTPRequest.HTTPMethod.GET,
                URL = "http://127.0.0.1:9898/xPath",
                Filter = "/bookstore/book/title"
            };

            var ret = ExecuteHTTP1Tx(request);

            Assert.AreEqual(1, ret.Count);
            Assert.IsTrue(ret.TryGet(request, out var result));
            Assert.AreEqual(OracleResultError.None, result.Error);
            Assert.AreEqual("Everyday Italian\nHarry Potter", Encoding.UTF8.GetString(result.Result));
        }

        [TestMethod]
        public void Test_HTTP_Filter_Text()
        {
            var request = new OracleHTTPRequest()
            {
                Version = OracleHTTPRequest.HTTPVersion.v1_1,
                Method = OracleHTTPRequest.HTTPMethod.GET,
                URL = "http://127.0.0.1:9898/text",
                Filter = @"\b[M]\w+"
            };

            var ret = ExecuteHTTP1Tx(request);

            Assert.AreEqual(1, ret.Count);
            Assert.IsTrue(ret.TryGet(request, out var result));
            Assert.AreEqual(OracleResultError.None, result.Error);
            Assert.AreEqual("Mahesh\nMike\nMarshal", Encoding.UTF8.GetString(result.Result));
        }

        [TestMethod]
        public void Test_HTTP_Filter_Json()
        {
            var request = new OracleHTTPRequest()
            {
                Version = OracleHTTPRequest.HTTPVersion.v1_1,
                Method = OracleHTTPRequest.HTTPMethod.GET,
                URL = "http://127.0.0.1:9898/json",
                Filter = "$.Manufacturers[?(@.Name == 'Acme Co')].Products[?(@.Price >= 50)].Name"
            };

            var ret = ExecuteHTTP1Tx(request);

            Assert.AreEqual(1, ret.Count);
            Assert.IsTrue(ret.TryGet(request, out var result));
            Assert.AreEqual(OracleResultError.None, result.Error);
            Assert.AreEqual(@"""Anvil""", Encoding.UTF8.GetString(result.Result));
        }

        [TestMethod]
        public void Test_HTTP_POST_Content()
        {
            var request = new OracleHTTPRequest()
            {
                Version = OracleHTTPRequest.HTTPVersion.v1_1,
                Method = OracleHTTPRequest.HTTPMethod.POST,
                URL = "http://127.0.0.1:9898/post",
                Filter = "",
                Body = Encoding.UTF8.GetBytes("Hello from POST oracle!")
            };

            var ret = ExecuteHTTP1Tx(request);

            Assert.AreEqual(1, ret.Count);
            Assert.IsTrue(ret.TryGet(request, out var result));
            Assert.AreEqual(OracleResultError.None, result.Error);
            CollectionAssert.AreEqual(request.Body, result.Result);
        }

        [TestMethod]
        public void Test_HTTP_PUT_Content()
        {
            var request = new OracleHTTPRequest()
            {
                Version = OracleHTTPRequest.HTTPVersion.v1_1,
                Method = OracleHTTPRequest.HTTPMethod.PUT,
                URL = "http://127.0.0.1:9898/put",
                Filter = "",
                Body = Encoding.UTF8.GetBytes("Hello from PUT oracle!")
            };

            var ret = ExecuteHTTP1Tx(request);

            Assert.AreEqual(1, ret.Count);
            Assert.IsTrue(ret.TryGet(request, out var result));
            Assert.AreEqual(OracleResultError.None, result.Error);
            CollectionAssert.AreEqual(request.Body, result.Result);
        }

        [TestMethod]
        public void Test_HTTP_DELETE_Content()
        {
            var request = new OracleHTTPRequest()
            {
                Version = OracleHTTPRequest.HTTPVersion.v1_1,
                Method = OracleHTTPRequest.HTTPMethod.DELETE,
                URL = "http://127.0.0.1:9898/delete",
                Filter = "",
                Body = null
            };

            var ret = ExecuteHTTP1Tx(request);

            Assert.AreEqual(1, ret.Count);
            Assert.IsTrue(ret.TryGet(request, out var result));
            Assert.AreEqual(OracleResultError.None, result.Error);
            CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("true"), result.Result);
        }

        [TestMethod]
        public void Test_HTTP_GET_Content()
        {
            var request = new OracleHTTPRequest()
            {
                Version = OracleHTTPRequest.HTTPVersion.v1_1,
                Method = OracleHTTPRequest.HTTPMethod.GET,
                URL = "http://127.0.0.1:9898/helloWorld",
                Filter = "",
                Body = null
            };

            var ret = ExecuteHTTP1Tx(request);

            Assert.AreEqual(1, ret.Count);
            Assert.IsTrue(ret.TryGet(request, out var result));
            Assert.AreEqual(OracleResultError.None, result.Error);
            CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("Hello world!"), result.Result);
        }

        [TestMethod]
        public void Test_HTTP_GET_Error()
        {
            var request = new OracleHTTPRequest()
            {
                Version = OracleHTTPRequest.HTTPVersion.v1_1,
                Method = OracleHTTPRequest.HTTPMethod.GET,
                URL = "http://127.0.0.1:9898/error",
                Filter = "",
                Body = null
            };

            var ret = ExecuteHTTP1Tx(request);

            Assert.AreEqual(1, ret.Count);
            Assert.IsTrue(ret.TryGet(request, out var result));
            Assert.AreEqual(OracleResultError.ServerError, result.Error);
            CollectionAssert.AreEqual(new byte[0], result.Result);
        }

        [TestMethod]
        public void Test_HTTP_GET_Timeout()
        {
            var request = new OracleHTTPRequest()
            {
                Version = OracleHTTPRequest.HTTPVersion.v1_1,
                Method = OracleHTTPRequest.HTTPMethod.GET,
                URL = "http://127.0.0.1:9898/timeout",
                Filter = "",
                Body = null
            };

            var ret = ExecuteHTTP1Tx(request);

            Assert.AreEqual(1, ret.Count);
            Assert.IsTrue(ret.TryGet(request, out var result));
            Assert.AreEqual(OracleResultError.Timeout, result.Error);
            CollectionAssert.AreEqual(new byte[0], result.Result);
        }

        private OracleExecutionCache ExecuteHTTP1Tx(OracleHTTPRequest request)
        {
            Transaction tx;

            using (var script = new ScriptBuilder())
            {
                switch (request.Method)
                {
                    case OracleHTTPRequest.HTTPMethod.GET:
                        {
                            script.EmitSysCall(InteropService.Neo_Oracle_HTTP11_Get, request.URL, request.Filter);
                            break;
                        }
                    case OracleHTTPRequest.HTTPMethod.POST:
                        {
                            script.EmitSysCall(InteropService.Neo_Oracle_HTTP11_Post, request.URL, request.Filter, request.Body);
                            break;
                        }
                    case OracleHTTPRequest.HTTPMethod.DELETE:
                        {
                            script.EmitSysCall(InteropService.Neo_Oracle_HTTP11_Delete, request.URL, request.Filter);
                            break;
                        }
                    case OracleHTTPRequest.HTTPMethod.PUT:
                        {
                            script.EmitSysCall(InteropService.Neo_Oracle_HTTP11_Put, request.URL, request.Filter, request.Body);
                            break;
                        }
                }

                tx = new Transaction()
                {
                    // Ensure that multiple execution are treated as one (with same hash)
                    Script = script.ToArray().Concat(script.ToArray()).ToArray(),
                    Attributes = new TransactionAttribute[0],
                    Cosigners = new Cosigner[0],
                    Sender = UInt160.Zero,
                    Witnesses = new Witness[0]
                };
            }

            // Without oracle MUST fail

            Assert.AreEqual(VMState.FAULT, ExecuteTxWithoutOracle(tx));

            // With Oracle

            using (var snapshot = store.GetSnapshot())
            {
                var service = new OracleService() { TimeOut = TimeSpan.FromSeconds(2) };
                return service.Process(snapshot, null, tx, true);
            }
        }

        private VMState ExecuteTxWithoutOracle(Transaction tx)
        {
            using (var snapshot = store.GetSnapshot())
            using (var engine = new ApplicationEngine(TriggerType.Application, tx, snapshot, tx.SystemFee, true, null))
            {
                engine.LoadScript(tx.Script);
                return engine.Execute();
            }
        }
    }
}
