using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.RPC.Server;
using Neo.SmartContract;
using Newtonsoft.Json.Linq;

namespace Neo.UnitTests.Network.RPC.Server
{
    [TestClass]
    public class UT_RpcServer
    {
        [TestMethod]
        public void bindAndCall()
        {
            RpcServer rpcServer = new RpcServer(GetRpcConfig(10322));

            // binding

            rpcServer.BindOperation(null, "checkDifferent", new Func<int, string, bool>((a, b) =>
            {
                int.TryParse(b, out var bInt);
                return !bInt.Equals(a);
            }));

            rpcServer.BindOperation("math", "checkEquals", new Func<int, string, bool>((a, b) =>
            {
                int.TryParse(b, out var bInt);
                return bInt.Equals(a);
            }));

            rpcServer.BindController<MathController>();
            rpcServer.BindController(typeof(Foo));

            // inject

            rpcServer.InjectSpecialParameter(context => new RandomObjectForTest { RandomProp = "Hello" });

            // calling

            var resp1 = (bool) rpcServer.CallOperation(null, null, "checkDifferent", 2, "2");
            Assert.IsFalse(resp1);

            var resp2 = (bool) rpcServer.CallOperation(null, "math", "checkEquals", new Dictionary<string, object>
            {
                {"a", 2 },
                {"b", "2"}
            });
            Assert.IsTrue(resp2);

            var resp3 = (int) rpcServer.CallOperation(null, "math", "sum", 2, 3);
            Assert.AreEqual(resp3, 5);

            var resp4 = (int) rpcServer.CallOperation(null, "math", "sub", 3, 1);
            Assert.AreEqual(resp4, 2);

            var resp5 = (string) rpcServer.CallOperation(null, "Foo", "Bar");
            Assert.AreEqual(resp5, "foo bar :)");

            var resp6 = (string) rpcServer.CallOperation(null, "Foo", "PrettyMessage", "how are you?");
            Assert.AreEqual(resp6, "Hello, how are you?");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void unbind()
        {
            RpcServer rpcServer = new RpcServer(GetRpcConfig(10323));

            // binding

            rpcServer.BindOperation(null, "checkDifferent", new Func<int, string, bool>((a, b) =>
            {
                int.TryParse(b, out var bInt);
                return !bInt.Equals(a);
            }));
            rpcServer.BindController<Foo>();

            // unbinding

            rpcServer.UnbindController("Foo");

            // calling

            var resp = (bool) rpcServer.CallOperation(null, null, "checkDifferent", 2, "2");
            Assert.IsFalse(resp);

            rpcServer.CallOperation(null, "Foo", "Bar");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void unbindAll()
        {
            RpcServer rpcServer = new RpcServer(GetRpcConfig(10324));

            // binding

            rpcServer.BindOperation(null, "checkDifferent", new Func<int, string, bool>((a, b) =>
            {
                int.TryParse(b, out var bInt);
                return !bInt.Equals(a);
            }));

            // unbinding

            rpcServer.UnbindAllOperations();

            // calling

            rpcServer.CallOperation(null, null, "checkDifferent", 2, "2");
        }

        private static RpcConfig GetRpcConfig(int port)
        {
            return new RpcConfig
            {
                ListenEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port),
                Ssl = new RpcConfig.SslCert
                {
                    Path = "./rpc-ssl.cert",
                    Password = "changeme"
                }
            };
        }
    }

    [RpcController(Name = "math")]
    public class MathController
    {
        [RpcMethod]
        public int sum(int a, int b)
        {
            return a + b;
        }

        [RpcMethod(Name = "sub")]
        public int Subtract(int a, int b)
        {
            return a - b;
        }
    }

    public class Foo
    {
        [RpcMethod]
        public string Bar()
        {
            return "foo bar :)";
        }

        [RpcMethod]
        public string PrettyMessage(RandomObjectForTest mo, string message)
        {
            return mo.RandomProp + ", " + message;
        }
    }

    public class RandomObjectForTest
    {
        public string RandomProp { get; set; }
    }
}
