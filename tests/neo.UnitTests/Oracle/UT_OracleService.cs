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
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.UnitTests.Oracle
{
    [TestClass, NotReRunnable]
    public class UT_OracleService : TestKit
    {
        [TestInitialize]
        public void Init()
        {
            TestBlockchain.InitializeMockNeoSystem();
        }

        public static IWebHost CreateServer(int port = 19898)
        {

            var server = new WebHostBuilder().UseKestrel(options =>
             {
                 options.Listen(IPAddress.Any, port, listenOptions =>
                 {
                     if (File.Exists("UT-cert.pfx"))
                     {
                         listenOptions.UseHttps("UT-cert.pfx", "123");
                     }
                     else if (File.Exists("../../../UT-cert.pfx"))
                     {
                         // Unix doesn't copy to the output dir

                         listenOptions.UseHttps("../../../UT-cert.pfx", "123");
                     }
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
            return server;
        }

        private static async Task ProcessAsync(HttpContext context)
        {
            var response = "";
            context.Response.ContentType = "text/plain";

            switch (context.Request.Path.Value)
            {
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
                        Thread.Sleep((int)(OracleService.HTTPSProtocol.TimeOut.TotalMilliseconds + 250));
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
            using var server = CreateServer();

            OracleService.HTTPSProtocol.AllowPrivateHost = true;

            TestProbe subscriber = CreateTestProbe();

            var wallet = TestUtils.GenerateTestWallet();
            TestActorRef<OracleService> service = ActorOfAsTestActorRef<OracleService>(
                Akka.Actor.Props.Create(() => new OracleService(subscriber, wallet)));

            service.UnderlyingActor.Start();

            // Send tx

            var tx = CreateTx("https://127.0.0.1:19898/ping", null);
            service.Tell(tx);

            // Receive response

            var response = subscriber.ExpectMsg<OracleService.OracleServiceResponse>(TimeSpan.FromSeconds(10));
            Assert.AreEqual(0, response.OracleResponseSignature.Length);
            Assert.AreEqual(1, response.ExecutionResult.Count);

            var entry = response.ExecutionResult.First();
            Assert.AreEqual(OracleResultError.None, entry.Value.Error);
            Assert.AreEqual("pong", Encoding.UTF8.GetString(entry.Value.Result));
            Assert.AreEqual(tx.Hash, response.UserTxHash);

            service.UnderlyingActor.Stop();
            OracleService.HTTPSProtocol.AllowPrivateHost = false;
        }

        private Transaction CreateTx(string url, OracleFilter filter)
        {
            using ScriptBuilder script = new ScriptBuilder();
            script.EmitSysCall(InteropService.Oracle.Neo_Oracle_Get, url, filter?.ContractHash, filter?.FilterMethod);

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
        public void TestOracleHttpsRequestTimeout()
        {
            using var server = CreateServer();

            // With local access (Only for UT)

            OracleService.HTTPSProtocol.AllowPrivateHost = true;

            // Timeout

            OracleService.HTTPSProtocol.TimeOut = TimeSpan.FromSeconds(2);

            var request = new OracleHttpsRequest()
            {
                Method = HttpMethod.GET,
                URL = new Uri("https://127.0.0.1:19898/timeout")
            };

            var response = OracleService.Process(request);

            OracleService.HTTPSProtocol.TimeOut = TimeSpan.FromSeconds(5);

            Assert.AreEqual(OracleResultError.Timeout, response.Error);
            Assert.IsTrue(response.Result.Length == 0);
            Assert.AreEqual(request.Hash, response.RequestHash);
            Assert.AreNotEqual(UInt160.Zero, response.Hash);
        }

        [TestMethod]
        public void TestOracleHttpsRequest()
        {
            using var server = CreateServer();

            // With local access (Only for UT)

            OracleService.HTTPSProtocol.AllowPrivateHost = true;

            // OK

            var request = new OracleHttpsRequest()
            {
                Method = HttpMethod.GET,
                URL = new Uri("https://127.0.0.1:19898/ping")
            };

            var response = OracleService.Process(request);

            Assert.AreEqual(OracleResultError.None, response.Error);
            Assert.AreEqual("pong", Encoding.UTF8.GetString(response.Result));
            Assert.AreEqual(request.Hash, response.RequestHash);
            Assert.AreNotEqual(UInt160.Zero, response.Hash);

            // Error

            request = new OracleHttpsRequest()
            {
                Method = HttpMethod.GET,
                URL = new Uri("https://127.0.0.1:19898/error")
            };

            response = OracleService.Process(request);

            Assert.AreEqual(OracleResultError.ResponseError, response.Error);
            Assert.IsTrue(response.Result.Length == 0);
            Assert.AreEqual(request.Hash, response.RequestHash);
            Assert.AreNotEqual(UInt160.Zero, response.Hash);

            // Without local access

            OracleService.HTTPSProtocol.AllowPrivateHost = false;
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
            Assert.AreEqual("0x6d53b08489400de6e2d3bf0edfd1385e14dbde68", response.Hash.ToString());
        }
    }
}
