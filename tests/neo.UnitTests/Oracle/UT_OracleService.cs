using Akka.TestKit;
using Akka.TestKit.Xunit2;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Oracle;
using Neo.Oracle.Protocols.Https;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.SmartContract.Native.Tokens;
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
    [TestClass]
    public class UT_OracleService : TestKit
    {
        [TestInitialize]
        public void Init()
        {
            TestBlockchain.InitializeMockNeoSystem();
        }

        public static IWebHost CreateServer(int port)
        {
            var server = new WebHostBuilder().UseKestrel(options =>
             {
                 options.Listen(IPAddress.Any, port, listenOptions =>
                 {
                     if (File.Exists("UT-cert.pfx"))
                     {
                         listenOptions.UseHttps("UT-cert.pfx", "123", https =>
                         {
                             https.CheckCertificateRevocation = false;
                             https.SslProtocols = System.Security.Authentication.SslProtocols.None;
                         });
                     }
                     else if (File.Exists("../../../UT-cert.pfx"))
                     {
                         // Unix doesn't copy to the output dir

                         listenOptions.UseHttps("../../../UT-cert.pfx", "123", https =>
                         {
                             https.CheckCertificateRevocation = false;
                             https.SslProtocols = System.Security.Authentication.SslProtocols.None;
                         });
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
            var port = 8443;
            using var server = CreateServer(port);

            OracleService.HTTPSProtocol.AllowPrivateHost = true;

            TestProbe subscriber = CreateTestProbe();

            var wallet = TestUtils.GenerateTestWallet();
            TestActorRef<OracleService> service = ActorOfAsTestActorRef<OracleService>(
                Akka.Actor.Props.Create(() => new OracleService(subscriber, wallet)));

            service.UnderlyingActor.Start();

            // Send tx

            var tx = CreateTx($"https://127.0.0.1:{port}/ping", null);
            service.Tell(tx);

            // Receive response

            var response = subscriber.ExpectMsg<OraclePayload>(TimeSpan.FromSeconds(10));
            Assert.AreEqual(0, response.OracleSignature.Signature.Length);
            Assert.AreEqual(1, response.Data.Length);

            var entry = response.ExecutionResult.First();
            Assert.IsFalse(entry.Value.Error);
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
        public void TestOracleTx()
        {
            var port = 8443;
            using var server = CreateServer(port);

            var wallet = TestUtils.GenerateTestWallet();
            var snapshot = Blockchain.Singleton.GetSnapshot();

            // no password on this wallet
            using (var unlock = wallet.Unlock(""))
            {
                var acc = wallet.CreateAccount();

                // Fake balance

                var key = NativeContract.GAS.CreateStorageKey(20, acc.ScriptHash);

                var entry = snapshot.Storages.GetAndChange(key, () => new StorageItem
                {
                    Value = new Nep5AccountState().ToByteArray()
                });

                entry.Value = new Nep5AccountState()
                {
                    Balance = 10000 * NativeContract.GAS.Factor
                }
                .ToByteArray();

                snapshot.Commit();

                // Manually creating script

                byte[] script;
                using (ScriptBuilder sb = new ScriptBuilder())
                {
                    sb.EmitSysCall(InteropService.Oracle.Neo_Oracle_Get, $"https://127.0.0.1:{port}/ping", null, null);
                    script = sb.ToArray();
                }

                // WithoutOracle

                Assert.ThrowsException<InvalidOperationException>(() =>
                {
                    _ = wallet.MakeTransaction(script, acc.ScriptHash, new TransactionAttribute[0], new Cosigner[0], oracle: OracleWalletBehaviour.WithoutOracle);
                });

                // OracleWithoutAssert

                var txWithout = wallet.MakeTransaction(script, acc.ScriptHash, new TransactionAttribute[0], new Cosigner[0], oracle: OracleWalletBehaviour.OracleWithoutAssert);

                Assert.IsNotNull(txWithout);
                Assert.IsNull(txWithout.Witnesses);
                Assert.AreEqual(TransactionVersion.OracleRequest, txWithout.Version);

                // OracleWithoutAssert

                var txWith = wallet.MakeTransaction(script, acc.ScriptHash, new TransactionAttribute[0], new Cosigner[0], oracle: OracleWalletBehaviour.OracleWithAssert);

                Assert.IsNotNull(txWith);
                Assert.IsNull(txWith.Witnesses);
                Assert.AreEqual(TransactionVersion.OracleRequest, txWith.Version);

                // Check that has more fee and the script is longer

                Assert.IsTrue(txWith.Script.Length > txWithout.Script.Length);
                Assert.IsTrue(txWith.NetworkFee > txWithout.NetworkFee);
            }
        }

        [TestMethod]
        public void TestOracleHttpsRequest()
        {
            var port = 8443;
            using var server = CreateServer(port);

            // With local access (Only for UT)

            OracleService.HTTPSProtocol.AllowPrivateHost = true;

            // Timeout

            OracleService.HTTPSProtocol.TimeOut = TimeSpan.FromSeconds(2);

            var request = new OracleHttpsRequest()
            {
                Method = HttpMethod.GET,
                URL = new Uri($"https://127.0.0.1:{port}/timeout")
            };

            var response = OracleService.Process(request);

            OracleService.HTTPSProtocol.TimeOut = TimeSpan.FromSeconds(5);

            Assert.IsTrue(response.Error);
            Assert.IsTrue(response.Result == null);
            Assert.AreEqual(request.Hash, response.RequestHash);
            Assert.AreNotEqual(UInt160.Zero, response.Hash);

            // OK

            request = new OracleHttpsRequest()
            {
                Method = HttpMethod.GET,
                URL = new Uri($"https://127.0.0.1:{port}/ping")
            };

            response = OracleService.Process(request);

            Assert.IsFalse(response.Error);
            Assert.AreEqual("pong", Encoding.UTF8.GetString(response.Result));
            Assert.AreEqual(request.Hash, response.RequestHash);
            Assert.AreNotEqual(UInt160.Zero, response.Hash);

            // Error

            request = new OracleHttpsRequest()
            {
                Method = HttpMethod.GET,
                URL = new Uri($"https://127.0.0.1:{port}/error")
            };

            response = OracleService.Process(request);

            Assert.IsTrue(response.Error);
            Assert.IsTrue(response.Result == null);
            Assert.AreEqual(request.Hash, response.RequestHash);
            Assert.AreNotEqual(UInt160.Zero, response.Hash);

            // Without local access

            OracleService.HTTPSProtocol.AllowPrivateHost = false;
            response = OracleService.Process(request);

            Assert.IsTrue(response.Error);
            Assert.IsTrue(response.Result == null);
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

            Assert.IsTrue(response.Error);
            Assert.AreEqual(null, response.Result);
            Assert.AreEqual(request.Hash, response.RequestHash);
            Assert.AreEqual("0xe62b56e4b43b01411403058ba53fc5e6dbdf8fba", response.Hash.ToString());
        }
    }
}
