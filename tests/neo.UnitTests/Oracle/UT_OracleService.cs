using Akka.TestKit;
using Akka.TestKit.Xunit2;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Oracle;
using Neo.Oracle.Protocols.Https;
using Neo.Persistence;
using Neo.SmartContract.Native;
using Neo.SmartContract.Native.Oracle;
using Neo.SmartContract.Native.Tokens;
using Neo.VM;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using System;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.UnitTests.Oracle
{
    [TestClass]
    public class UT_OracleService : TestKit
    {
        private WalletAccount _account;
        private NEP6Wallet _wallet;

        [TestInitialize]
        public void Init()
        {
            _wallet = TestUtils.GenerateTestWallet();
            using var unlockA = _wallet.Unlock("123");
            _account = _wallet.CreateAccount();

            TestBlockchain.InitializeMockNeoSystem();
        }

        SnapshotView MockedSnapshotFactory()
        {
            // Mock blockchain is not possible

            var snapshot = Blockchain.Singleton.GetSnapshot();

            foreach (var cn in Blockchain.StandbyValidators)
            {
                var key = NativeContract.Oracle.CreateStorageKey(OracleContract.Prefix_Validator, cn);
                var value = snapshot.Storages.GetOrAdd(key, () => new StorageItem() { IsConstant = false });
                value.Value = _account.GetKey().PublicKey.ToArray();
            }

            return snapshot;
        }

        private static X509Certificate2 buildSelfSignedServerCertificate(string certificateName)
        {
            SubjectAlternativeNameBuilder sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddIpAddress(IPAddress.Loopback);
            sanBuilder.AddIpAddress(IPAddress.IPv6Loopback);
            sanBuilder.AddDnsName("localhost");
            sanBuilder.AddDnsName(Environment.MachineName);

            X500DistinguishedName distinguishedName = new X500DistinguishedName($"CN={certificateName}");

            using RSA rsa = RSA.Create(2048);
            var request = new CertificateRequest(distinguishedName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature, false));
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));
            request.CertificateExtensions.Add(sanBuilder.Build());

            var certificate = request.CreateSelfSigned(new DateTimeOffset(DateTime.UtcNow.AddDays(-1)), new DateTimeOffset(DateTime.UtcNow.AddDays(3650)));
            //certificate.FriendlyName = certificateName;

            return new X509Certificate2(certificate.Export(X509ContentType.Pfx, "pass"), "pass", X509KeyStorageFlags.MachineKeySet);
        }

        public static IWebHost CreateServer(int port)
        {
            var server = new WebHostBuilder().UseKestrel(options =>
             {
                 options.Listen(IPAddress.Any, port, listenOptions =>
                 {
                     listenOptions.UseHttps(buildSelfSignedServerCertificate("neo"), c =>
                     {
                         c.CheckCertificateRevocation = false;
                         c.SslProtocols = SslProtocols.None;
                     });
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
            context.Response.ContentType = "application/json";

            switch (context.Request.Path.Value)
            {
                case "/json":
                    {
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
                        response = "\"pong\"";
                        break;
                    }
                case "/text":
                    {
                        context.Response.ContentType = "text/plain";
                        break;
                    }
                case "/timeout":
                    {
                        Thread.Sleep((int)(OracleService.HTTPSProtocol.Config.TimeOut + 250));
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

            var service = new OracleService(subscriber, null, _wallet, MockedSnapshotFactory, 10);

            Assert.IsFalse(service.IsStarted);
            Assert.AreEqual(10, service.PendingCapacity);
            Assert.AreEqual(0, service.PendingRequestCount);
            Assert.AreEqual(0, service.PendingResponseCount);

            service.Stop(); // Without error
            service.Start();
            Assert.IsTrue(service.IsStarted);
            service.Start(); // Without error

            service.Stop();
        }

        [TestMethod]
        public void ProcessTx()
        {
            var port = 8443;
            using var server = CreateServer(port);

            OracleService.HTTPSProtocol.AllowPrivateHost = true;

            TestProbe subscriber = CreateTestProbe();
            TestActorRef<OracleService> service = ActorOfAsTestActorRef(() => new OracleService(subscriber, null, _wallet, MockedSnapshotFactory, 10));

            service.UnderlyingActor.Start();

            // Send start

            service.Tell(new OracleService.StartMessage() { NumberOfTasks = 1 });

            // Send tx

            var rr = new Blockchain.RelayResult
            {
                Result = VerifyResult.Succeed,
                Inventory = CreateTx($"https://127.0.0.1:{port}/ping", null)
            };
            service.Tell(rr);

            // Receive response

            var responseMsg = subscriber.ExpectMsg<LocalNode.SendDirectly>(TimeSpan.FromSeconds(10));
            Assert.IsInstanceOfType(responseMsg.Inventory, typeof(Transaction));

            var response = responseMsg.Inventory as Transaction;

            Assert.IsTrue(response.IsOracleResponse(out var requestTxHash));
            Assert.AreEqual(rr.Inventory.Hash, requestTxHash);

            //var response = responseMsg.Inventory as OraclePayload;
            //Assert.AreEqual(117, response.Data.Length);
            //Assert.AreEqual(_account.GetKey().PublicKey, response.OraclePub);

            //Assert.IsNotNull(response.OracleSignature);
            //// pong
            //Assert.AreEqual("0x6f458eea71a0b63d3e9efc8cc54608e14d89a5cd", response.OracleSignature.OracleExecutionCacheHash.ToString());
            //Assert.AreEqual(64, response.OracleSignature.Signature.Length);
            //Assert.AreEqual(tx.Hash, response.OracleSignature.TransactionRequestHash);

            service.UnderlyingActor.Stop();
            OracleService.HTTPSProtocol.AllowPrivateHost = false;
        }

        private Transaction CreateTx(string url, OracleFilter filter)
        {
            using ScriptBuilder script = new ScriptBuilder();
            script.EmitAppCall(NativeContract.Oracle.Hash, "get", url, filter?.ContractHash, filter?.FilterMethod, filter?.FilterArgs);

            return new Transaction()
            {
                Version = 0,
                Attributes = new TransactionAttribute[]
                {
                    new OracleRequestAttribute()
                },
                Script = script.ToArray(),
                Signers = new Signer[] { new Signer() { Account = UInt160.Zero, Scopes = WitnessScope.CalledByEntry } },
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
                var entry = snapshot.Storages.GetAndChange(key, () => new StorageItem(new AccountState()
                {
                    Balance = 10000 * NativeContract.GAS.Factor
                }));

                snapshot.Commit();

                // Manually creating script

                byte[] script;
                using (ScriptBuilder sb = new ScriptBuilder())
                {
                    sb.EmitAppCall(NativeContract.Oracle.Hash, "get", $"https://127.0.0.1:{port}/ping", null, null, null);
                    script = sb.ToArray();
                }

                // WithoutOracle

                Assert.ThrowsException<InvalidOperationException>(() =>
                {
                    _ = wallet.MakeTransaction(script, acc.ScriptHash, oracle: OracleWalletBehaviour.WithoutOracle);
                });

                // OracleWithoutAssert

                var txWithout = wallet.MakeTransaction(script, acc.ScriptHash, oracle: OracleWalletBehaviour.OracleWithoutAssert);

                Assert.IsNotNull(txWithout);
                Assert.IsNull(txWithout.Witnesses);
                Assert.IsTrue(txWithout.IsOracleRequest());

                // OracleWithoutAssert

                var txWith = wallet.MakeTransaction(script, acc.ScriptHash, oracle: OracleWalletBehaviour.OracleWithAssert);

                Assert.IsNotNull(txWith);
                Assert.IsNull(txWith.Witnesses);
                Assert.IsTrue(txWith.IsOracleRequest());

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

            OracleService.HTTPSProtocol.Config.TimeOut = (int)TimeSpan.FromSeconds(2).TotalMilliseconds;

            var request = new OracleHttpsRequest()
            {
                Method = HttpMethod.GET,
                URL = new Uri($"https://127.0.0.1:{port}/timeout")
            };

            var response = OracleService.Process(request);

            OracleService.HTTPSProtocol.Config.TimeOut = (int)TimeSpan.FromSeconds(5).TotalMilliseconds;

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
            Assert.AreEqual("\"pong\"", Encoding.UTF8.GetString(response.Result));
            Assert.AreEqual(request.Hash, response.RequestHash);
            Assert.AreNotEqual(UInt160.Zero, response.Hash);

            // Error 404

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

            // Error ContentType

            request = new OracleHttpsRequest()
            {
                Method = HttpMethod.GET,
                URL = new Uri($"https://127.0.0.1:{port}/text")
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
