// Copyright (C) 2015-2025 The Neo Project.
//
// TestBlockchain.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Neo.Extensions;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using System;

namespace Neo.Plugins.OracleService.Tests
{
    public static class TestBlockchain
    {
        public static readonly NeoSystem s_theNeoSystem;
        public static readonly MemoryStore s_store = new();
        public static readonly NEP6Wallet s_wallet;
        public static readonly WalletAccount s_walletAccount;
        public static readonly OracleService s_oracle;

        private class StoreProvider : IStoreProvider
        {
            public string Name => "TestProvider";
            public IStore GetStore(string path) => s_store;
        }

        static TestBlockchain()
        {
            Console.WriteLine("initialize NeoSystem");
            StoreProvider _memoryStoreProvider = new();
            s_oracle = new();
            s_theNeoSystem = new NeoSystem(TestUtils.settings, _memoryStoreProvider);
            s_wallet = TestUtils.GenerateTestWallet("123");
            s_walletAccount = s_wallet.Import("KxuRSsHgJMb3AMSN6B9P3JHNGMFtxmuimqgR9MmXPcv3CLLfusTd");
        }

        public static UInt160 InitializeContract()
        {
            /*
             
            //Oracle Contract Source Code
            using System.Numerics;
            using Neo.SmartContract.Framework;
            using Neo.SmartContract.Framework.Native;
            using Neo.SmartContract.Framework.Services;

            namespace oracle_demo
            {
                public class OracleDemo : SmartContract
                {
                    const byte PREFIX_COUNT = 0xcc;
                    const byte PREFIX_DATA = 0xdd;

                    public static string GetRequstData() =>
                        Storage.Get(Storage.CurrentContext, new byte[] { PREFIX_DATA });

                    public static BigInteger GetRequstCount() =>
                        (BigInteger)Storage.Get(Storage.CurrentContext, new byte[] { PREFIX_COUNT });

                    public static void CreateRequest(string url, string filter, string callback, byte[] userData, long gasForResponse) =>
                        Oracle.Request(url, filter, callback, userData, gasForResponse);

                    public static void Callback(string url, byte[] userData, int code, byte[] result)
                    {
                        ExecutionEngine.Assert(Runtime.CallingScriptHash == Oracle.Hash, "Unauthorized!");
                        StorageContext currentContext = Storage.CurrentContext;
                        Storage.Put(currentContext, new byte[] { PREFIX_DATA }, (ByteString)result);
                        Storage.Put(currentContext, new byte[] { PREFIX_COUNT },
                            (BigInteger)Storage.Get(currentContext, new byte[] { PREFIX_DATA }) + 1);
                    }
                }
            }
            */
            string base64NefFile = "TkVGM05lby5Db21waWxlci5DU2hhcnAgMy43LjQrNjAzNGExODIxY2E3MDk0NjBlYzMxMzZjNzBjMmRjYzNiZWEuLi4AAAFYhxcRfgqoEHKvq3HS3Yn+fEuS/gdyZXF1ZXN0BQAADwAAmAwB3dswQZv2Z85Bkl3oMUAMAczbMEGb9mfOQZJd6DFK2CYERRDbIUBXAAV8e3p5eDcAAEBXAQRBOVNuPAwUWIcXEX4KqBByr6tx0t2J/nxLkv6XDA1VbmF1dGhvcml6ZWQh4UGb9mfOcHvbKAwB3dswaEHmPxiEDAHd2zBoQZJd6DFK2CYERRDbIRGeDAHM2zBoQeY/GIRAnIyFhg==";
            string manifest = """{"name":"OracleDemo","groups":[],"features":{},"supportedstandards":[],"abi":{"methods":[{"name":"getRequstData","parameters":[],"returntype":"String","offset":0,"safe":false},{"name":"getRequstCount","parameters":[],"returntype":"Integer","offset":16,"safe":false},{"name":"createRequest","parameters":[{"name":"url","type":"String"},{"name":"filter","type":"String"},{"name":"callback","type":"String"},{"name":"userData","type":"ByteArray"},{"name":"gasForResponse","type":"Integer"}],"returntype":"Void","offset":40,"safe":false},{"name":"callback","parameters":[{"name":"url","type":"String"},{"name":"userData","type":"ByteArray"},{"name":"code","type":"Integer"},{"name":"result","type":"ByteArray"}],"returntype":"Void","offset":52,"safe":false}],"events":[]},"permissions":[{"contract":"0xfe924b7cfe89ddd271abaf7210a80a7e11178758","methods":["request"]}],"trusts":[],"extra":{"nef":{"optimization":"All"}}}""";
            byte[] script;
            using (ScriptBuilder sb = new())
            {
                sb.EmitDynamicCall(NativeContract.ContractManagement.Hash, "deploy", Convert.FromBase64String(base64NefFile), manifest);
                script = sb.ToArray();
            }
            SnapshotCache snapshot = s_theNeoSystem.GetSnapshotCache();
            Transaction tx = new Transaction
            {
                Nonce = 233,
                ValidUntilBlock = NativeContract.Ledger.CurrentIndex(snapshot) + s_theNeoSystem.Settings.MaxValidUntilBlockIncrement,
                Signers = [new Signer() { Account = TestUtils.ValidatorScriptHash, Scopes = WitnessScope.CalledByEntry }],
                Attributes = System.Array.Empty<TransactionAttribute>(),
                Script = script,
                Witnesses = null,
            };
            var engine = ApplicationEngine.Run(tx.Script, snapshot, container: tx, settings: s_theNeoSystem.Settings, gas: 1200_0000_0000);
            engine.SnapshotCache.Commit();
            var result = (Neo.VM.Types.Array)engine.ResultStack.Peek();
            return new UInt160(result[2].GetSpan());
        }

        internal static void ResetStore()
        {
            s_store.Reset();
            s_theNeoSystem.Blockchain.Ask(new Blockchain.Initialize()).Wait();
        }

        internal static SnapshotCache GetTestSnapshotCache()
        {
            ResetStore();
            return s_theNeoSystem.GetSnapshotCache();
        }
    }
}
