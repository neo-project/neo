using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P.Payloads;
using Neo.Oracle;
using Neo.Oracle.Protocols.HTTP;
using Neo.SmartContract;
using Neo.VM;
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
        IWebHost server;

        [TestInitialize]
        public void Init()
        {
            server = new WebHostBuilder().UseKestrel(options => options.Listen(IPAddress.Any, 9898, listenOptions =>
            {

            }))
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

            switch (context.Request.Path.Value)
            {
                case "/helloWorld":
                    {
                        context.Response.ContentType = "text/plain";
                        response = "Hello world!";
                        break;
                    }
                case "/timeout":
                    {
                        Thread.Sleep(3100);
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
        public void TestTransaction_GET_Content()
        {
            var request = new OracleHTTPRequest()
            {
                Method = OracleHTTPRequest.HTTPMethod.GET,
                URL = "http://127.0.0.1:9898/helloWorld",
                Filter = "",
                Body = null,
                VersionMajor = 1,
                VersionMinor = 1
            };

            var ret = ExecuteHTTP1GetTx(request);

            Assert.AreEqual(1, ret.Count);
            Assert.IsTrue(ret.TryGet(request, out var result));
            Assert.AreEqual(OracleResultError.None, result.Error);
            CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("Hello world!"), result.Result);
        }

        [TestMethod]
        public void TestTransaction_GET_Timeout()
        {
            var request = new OracleHTTPRequest()
            {
                Method = OracleHTTPRequest.HTTPMethod.GET,
                URL = "http://127.0.0.1:9898/timeout",
                Filter = "",
                Body = null,
                VersionMajor = 1,
                VersionMinor = 1
            };

            var ret = ExecuteHTTP1GetTx(request);

            Assert.AreEqual(1, ret.Count);
            Assert.IsTrue(ret.TryGet(request, out var result));
            Assert.AreEqual(OracleResultError.Timeout, result.Error);
            CollectionAssert.AreEqual(new byte[0], result.Result);
        }

        private OracleResultsCache ExecuteHTTP1GetTx(OracleHTTPRequest request)
        {
            Transaction tx;

            using (var script = new ScriptBuilder())
            {
                script.EmitSysCall(InteropService.Neo_Oracle_HTTP11_Get, request.URL, request.Filter);

                tx = new Transaction()
                {
                    Script = script.ToArray(),
                    Attributes = new TransactionAttribute[0],
                    Cosigners = new Cosigner[0],
                    Sender = UInt160.Zero,
                    Witnesses = new Witness[0]
                };
            }

            // Without oracle MUST fail

            Assert.AreEqual(VMState.FAULT, ExecuteTxWithoutOracle(tx));

            // With Oracle

            var service = new OracleService();
            return service.Process(null, tx, true);
        }

        private VMState ExecuteTxWithoutOracle(Transaction tx)
        {
            using (var engine = new ApplicationEngine(TriggerType.Application, tx, null, tx.SystemFee, true, null))
            {
                engine.LoadScript(tx.Script);
                return engine.Execute();
            }
        }
    }
}
