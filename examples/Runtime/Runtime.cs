using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;

using System.ComponentModel;
using System.Numerics;
using Neo;
using Neo.SmartContract.Framework.Services;

namespace Runtime
{
    [DisplayName("SampleRuntime")]
    [ManifestExtra("Author", "code-dev")]
    [ManifestExtra("Description", "A sample contract to demonstrate how to use runtime interface")]
    [ManifestExtra("Email", "core@neo.org")]
    [ManifestExtra("Version", "0.0.1")]
    [ContractSourceCode("https://github.com/neo-project/examples/Runtime")]
    [ContractPermission("*", "*")]
    public class SampleRuntime : SmartContract
    {
        public static uint GetInvocationCounter()
        {
            return Neo.SmartContract.Framework.Services.Runtime.InvocationCounter;
        }

        public static ulong GetTime()
        {
            return Neo.SmartContract.Framework.Services.Runtime.Time;
        }

        public static BigInteger GetRandom()
        {
            return Neo.SmartContract.Framework.Services.Runtime.GetRandom();
        }

        public static long GetGasLeft()
        {
            return Neo.SmartContract.Framework.Services.Runtime.GasLeft;
        }

        public static string GetPlatform()
        {
            return Neo.SmartContract.Framework.Services.Runtime.Platform;
        }

        public static uint GetNetwork()
        {
            return Neo.SmartContract.Framework.Services.Runtime.GetNetwork();
        }

        public static uint GetAddressVersion()
        {
            return Neo.SmartContract.Framework.Services.Runtime.AddressVersion;
        }

        public static byte GetTrigger()
        {
            return (byte)Neo.SmartContract.Framework.Services.Runtime.Trigger;
        }

        public static void Log(string message)
        {
            Neo.SmartContract.Framework.Services.Runtime.Log(message);
        }

        public static bool CheckWitness(UInt160 hash)
        {
            return Neo.SmartContract.Framework.Services.Runtime.CheckWitness(hash);
        }

        public static int GetNotificationsCount(UInt160 hash)
        {
            var notifications = Neo.SmartContract.Framework.Services.Runtime.GetNotifications(hash);
            return notifications.Length;
        }

        public static int GetAllNotifications()
        {
            int sum = 0;
            var notifications = Neo.SmartContract.Framework.Services.Runtime.GetNotifications();

            for (int x = 0; x < notifications.Length; x++)
            {
                var notify = notifications[x];
                sum += (int)notify.State[0];
            }

            return sum;
        }

        public static int GetNotifications(UInt160 hash)
        {
            int sum = 0;
            var notifications = Neo.SmartContract.Framework.Services.Runtime.GetNotifications(hash);

            for (int x = 0; x < notifications.Length; x++)
            {
                sum += (int)notifications[x].State[0];
            }

            return sum;
        }

        public static object GetTransactionHash()
        {
            var tx = (Transaction)Neo.SmartContract.Framework.Services.Runtime.ScriptContainer;
            return tx?.Hash;
        }

        public static object GetTransactionVersion()
        {
            var tx = (Transaction)Neo.SmartContract.Framework.Services.Runtime.ScriptContainer;
            return tx?.Version;
        }

        public static object GetTransactionNonce()
        {
            var tx = (Transaction)Neo.SmartContract.Framework.Services.Runtime.ScriptContainer;
            return tx?.Nonce;
        }

        public static object GetTransactionSender()
        {
            var tx = (Transaction)Neo.SmartContract.Framework.Services.Runtime.ScriptContainer;
            return tx?.Sender;
        }

        public static object GetTransactionSystemFee()
        {
            var tx = (Transaction)Neo.SmartContract.Framework.Services.Runtime.ScriptContainer;
            return tx?.SystemFee;
        }

        public static object GetTransactionNetworkFee()
        {
            var tx = (Transaction)Neo.SmartContract.Framework.Services.Runtime.ScriptContainer;
            return tx?.NetworkFee;
        }

        public static object GetTransactionValidUntilBlock()
        {
            var tx = (Transaction)Neo.SmartContract.Framework.Services.Runtime.ScriptContainer;
            return tx?.ValidUntilBlock;
        }

        public static object GetTransactionScript()
        {
            var tx = (Transaction)Neo.SmartContract.Framework.Services.Runtime.ScriptContainer;
            return tx?.Script;
        }

        public static int DynamicSum(int a, int b)
        {
            ByteString script = (ByteString)new byte[] { 0x9E }; // ADD
            return (int)Neo.SmartContract.Framework.Services.Runtime.LoadScript(script, CallFlags.All, new object[] { a, b });
        }
    }
}
