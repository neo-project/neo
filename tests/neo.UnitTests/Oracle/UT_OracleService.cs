using Akka.TestKit;
using Akka.TestKit.Xunit2;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P.Payloads;
using Neo.Oracle;
using Neo.Oracle.Protocols.Https;
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
    public class UT_OracleService : TestKit
    {
        IWebHost server;

        [TestInitialize]
        public void Init()
        {
            TestBlockchain.InitializeMockNeoSystem();

            server = new WebHostBuilder().UseKestrel(options =>
            {
                options.Listen(IPAddress.Any, 9898, listenOptions =>
                 {
                     listenOptions.UseHttps("UT-cert.pfx", "123");

                 });
            })
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

        private async Task ProcessAsync(HttpContext context)
        {
            var response = "";
            context.Response.ContentType = "text/plain";

            switch (context.Request.Path.Value)
            {
                case "/xml":
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
                case "/ping":
                    {
                        response = "pong";
                        break;
                    }
                case "/timeout":
                    {
                        Thread.Sleep(6000);
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

        [TestCleanup]
        public void Clean()
        {
            server.StopAsync().Wait();
            server.Dispose();
        }

        [TestMethod]
        public void StartStop()
        {
            TestProbe subscriber = CreateTestProbe();

            var service = new OracleService(subscriber, null);
            Assert.IsFalse(service.IsStarted);
            service.Start();
            Assert.IsTrue(service.IsStarted);
            service.Stop();
        }

        [TestMethod]
        public void ProcessTx()
        {
            OracleHttpsProtocol.AllowPrivateHost = true;

            TestProbe subscriber = CreateTestProbe();

            var wallet = TestUtils.GenerateTestWallet();
            TestActorRef<OracleService> service = ActorOfAsTestActorRef<OracleService>(
                Akka.Actor.Props.Create(() => new OracleService(subscriber, wallet)));

            service.UnderlyingActor.Start();

            // Send tx

            service.Tell(CreateTx("https://127.0.0.1:9898/ping", ""));

            // Receive response

            var response = subscriber.ExpectMsg<OracleService.OracleServiceResponse>();
            Assert.AreEqual(0, response.OracleSignature.Length);
            Assert.AreEqual(1, response.ExecutionResult.Count);

            var entry = response.ExecutionResult.First();
            Assert.AreEqual("pong", Encoding.UTF8.GetString(entry.Value.Result));

            service.UnderlyingActor.Stop();

            OracleHttpsProtocol.AllowPrivateHost = false;
        }

        private Transaction CreateTx(string url, string filter)
        {
            using ScriptBuilder script = new ScriptBuilder();
            script.EmitSysCall(InteropService.Oracle.Neo_Oracle_Get, url, filter);

            return new Transaction()
            {
                Attributes = new TransactionAttribute[0],
                Cosigners = new Cosigner[0],
                Script = script.ToArray(),
                Sender = UInt160.Zero,
                Witnesses = new Witness[0],
                NetworkFee = 1_000_000,
                SystemFee = 1_000_000,
            };
        }

        [TestMethod]
        public void TestOracleHttpsRequest()
        {
            // With local access (Only for UT)

            OracleHttpsProtocol.AllowPrivateHost = true;

            // Timeout

            var request = new OracleHttpsRequest()
            {
                Filter = "",
                Method = HttpMethod.GET,
                URL = new Uri("https://127.0.0.1:9898/timeout")
            };

            var response = OracleService.Process(request);

            Assert.AreEqual(OracleResultError.Timeout, response.Error);
            Assert.IsTrue(response.Result.Length == 0);
            Assert.AreEqual(request.Hash, response.RequestHash);
            Assert.AreNotEqual(UInt160.Zero, response.Hash);

            // OK

            request = new OracleHttpsRequest()
            {
                Filter = "",
                Method = HttpMethod.GET,
                URL = new Uri("https://127.0.0.1:9898/ping")
            };

            response = OracleService.Process(request);

            Assert.AreEqual(OracleResultError.None, response.Error);
            Assert.AreEqual("pong", Encoding.UTF8.GetString(response.Result));
            Assert.AreEqual(request.Hash, response.RequestHash);
            Assert.AreNotEqual(UInt160.Zero, response.Hash);

            // Error

            request = new OracleHttpsRequest()
            {
                Filter = "",
                Method = HttpMethod.GET,
                URL = new Uri("https://127.0.0.1:9898/error")
            };

            response = OracleService.Process(request);

            Assert.AreEqual(OracleResultError.ServerError, response.Error);
            Assert.IsTrue(response.Result.Length == 0);
            Assert.AreEqual(request.Hash, response.RequestHash);
            Assert.AreNotEqual(UInt160.Zero, response.Hash);

            // Without local access

            OracleHttpsProtocol.AllowPrivateHost = false;
            response = OracleService.Process(request);

            Assert.AreEqual(OracleResultError.PolicyError, response.Error);
            Assert.IsTrue(response.Result.Length == 0);
            Assert.AreEqual(request.Hash, response.RequestHash);
            Assert.AreNotEqual(UInt160.Zero, response.Hash);
        }

        class ErrorRequest : OracleRequest
        {
            public override OracleRequestType Type => OracleRequestType.HTTPS;
            protected override byte[] GetHashData() => new byte[1];
        }

        [TestMethod]
        public void TestOracleErrorRequest()
        {
            var request = new ErrorRequest();
            var response = OracleService.Process(request);

            Assert.AreEqual(OracleResultError.ProtocolError, response.Error);
            CollectionAssert.AreEqual(new byte[0], response.Result);
            Assert.AreEqual(request.Hash, response.RequestHash);
            Assert.AreEqual("0x7202b7ba0427e8ea902bffac6ea76a45febecc03", response.Hash.ToString());
        }
    }
}
