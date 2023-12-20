// Copyright (C) 2016-2023 The Neo Project.
// 
// The neo-cli is free software distributed under the MIT software 
// license, see the accompanying file LICENSE in the main directory of
// the project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Neo.ConsoleService;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Json;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Plugins;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using Array = System.Array;

namespace Neo.CLI
{
    public partial class MainService : ConsoleServiceBase, IWalletProvider
    {
        public event EventHandler<Wallet> WalletChanged;

        public const long TestModeGas = 20_00000000;

        private Wallet _currentWallet;
        public LocalNode LocalNode;

        public Wallet CurrentWallet
        {
            get => _currentWallet;
            private set
            {
                _currentWallet = value;
                WalletChanged?.Invoke(this, value);
            }
        }

        private NeoSystem _neoSystem;
        public NeoSystem NeoSystem
        {
            get => _neoSystem;
            private set => _neoSystem = value;
        }

        protected override string Prompt => "neo";
        public override string ServiceName => "NEO-CLI";

        /// <summary>
        /// Constructor
        /// </summary>
        public MainService() : base()
        {
            RegisterCommandHandler<string, UInt160>(false, str => StringToAddress(str, NeoSystem.Settings.AddressVersion));
            RegisterCommandHandler<string, UInt256>(false, UInt256.Parse);
            RegisterCommandHandler<string[], UInt256[]>(str => str.Select(u => UInt256.Parse(u.Trim())).ToArray());
            RegisterCommandHandler<string[], UInt160[]>(arr => arr.Select(str => StringToAddress(str, NeoSystem.Settings.AddressVersion)).ToArray());
            RegisterCommandHandler<string, ECPoint>(str => ECPoint.Parse(str.Trim(), ECCurve.Secp256r1));
            RegisterCommandHandler<string[], ECPoint[]>(str => str.Select(u => ECPoint.Parse(u.Trim(), ECCurve.Secp256r1)).ToArray());
            RegisterCommandHandler<string, JToken>(str => JToken.Parse(str));
            RegisterCommandHandler<string, JObject>(str => (JObject)JToken.Parse(str));
            RegisterCommandHandler<string, decimal>(str => decimal.Parse(str, CultureInfo.InvariantCulture));
            RegisterCommandHandler<JToken, JArray>(obj => (JArray)obj);

            RegisterCommand(this);

            Initialize_Logger();
        }

        internal UInt160 StringToAddress(string input, byte version)
        {
            switch (input.ToLowerInvariant())
            {
                case "neo": return NativeContract.NEO.Hash;
                case "gas": return NativeContract.GAS.Hash;
            }

            if (input.EndsWith(".neo", StringComparison.InvariantCultureIgnoreCase) ||
                input.EndsWith(".neofs", StringComparison.InvariantCultureIgnoreCase))
            {
                return ResolveNeoNameServiceAddress(input);
            }

            // Try to parse as UInt160

            if (UInt160.TryParse(input, out var addr))
            {
                return addr;
            }

            // Accept wallet format

            return input.ToScriptHash(version);
        }

        Wallet IWalletProvider.GetWallet()
        {
            return CurrentWallet;
        }

        public override void RunConsole()
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;

            var cliV = Assembly.GetAssembly(typeof(Program)).GetVersion();
            var neoV = Assembly.GetAssembly(typeof(NeoSystem)).GetVersion();
            var vmV = Assembly.GetAssembly(typeof(ExecutionEngine)).GetVersion();
            Console.WriteLine($"{ServiceName} v{cliV}  -  NEO v{neoV}  -  NEO-VM v{vmV}");
            Console.WriteLine();

            base.RunConsole();
        }

        public void CreateWallet(string path, string password, bool createDefaultAccount = true)
        {
            Wallet wallet = Wallet.Create(null, path, password, NeoSystem.Settings);
            if (wallet == null)
            {
                ConsoleHelper.Warning("Wallet files in that format are not supported, please use a .json or .db3 file extension.");
                return;
            }
            if (createDefaultAccount)
            {
                WalletAccount account = wallet.CreateAccount();
                ConsoleHelper.Info("   Address: ", account.Address);
                ConsoleHelper.Info("    Pubkey: ", account.GetKey().PublicKey.EncodePoint(true).ToHexString());
                ConsoleHelper.Info("ScriptHash: ", $"{account.ScriptHash}");
            }
            wallet.Save();
            CurrentWallet = wallet;
        }

        private IEnumerable<Block> GetBlocks(Stream stream, bool read_start = false)
        {
            using BinaryReader r = new BinaryReader(stream);
            uint start = read_start ? r.ReadUInt32() : 0;
            uint count = r.ReadUInt32();
            uint end = start + count - 1;
            uint currentHeight = NativeContract.Ledger.CurrentIndex(NeoSystem.StoreView);
            if (end <= currentHeight) yield break;
            for (uint height = start; height <= end; height++)
            {
                var size = r.ReadInt32();
                if (size > Message.PayloadMaxSize)
                    throw new ArgumentException($"Block {height} exceeds the maximum allowed size");

                byte[] array = r.ReadBytes(size);
                if (height > currentHeight)
                {
                    Block block = array.AsSerializable<Block>();
                    yield return block;
                }
            }
        }

        private IEnumerable<Block> GetBlocksFromFile()
        {
            const string pathAcc = "chain.acc";
            if (File.Exists(pathAcc))
                using (FileStream fs = new FileStream(pathAcc, FileMode.Open, FileAccess.Read, FileShare.Read))
                    foreach (var block in GetBlocks(fs))
                        yield return block;

            const string pathAccZip = pathAcc + ".zip";
            if (File.Exists(pathAccZip))
                using (FileStream fs = new FileStream(pathAccZip, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Read))
                using (Stream zs = zip.GetEntry(pathAcc).Open())
                    foreach (var block in GetBlocks(zs))
                        yield return block;

            var paths = Directory.EnumerateFiles(".", "chain.*.acc", SearchOption.TopDirectoryOnly).Concat(Directory.EnumerateFiles(".", "chain.*.acc.zip", SearchOption.TopDirectoryOnly)).Select(p => new
            {
                FileName = Path.GetFileName(p),
                Start = uint.Parse(Regex.Match(p, @"\d+").Value),
                IsCompressed = p.EndsWith(".zip")
            }).OrderBy(p => p.Start);

            uint height = NativeContract.Ledger.CurrentIndex(NeoSystem.StoreView);
            foreach (var path in paths)
            {
                if (path.Start > height + 1) break;
                if (path.IsCompressed)
                    using (FileStream fs = new FileStream(path.FileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Read))
                    using (Stream zs = zip.GetEntry(Path.GetFileNameWithoutExtension(path.FileName)).Open())
                        foreach (var block in GetBlocks(zs, true))
                            yield return block;
                else
                    using (FileStream fs = new FileStream(path.FileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                        foreach (var block in GetBlocks(fs, true))
                            yield return block;
            }
        }

        private bool NoWallet()
        {
            if (CurrentWallet != null) return false;
            ConsoleHelper.Error("You have to open the wallet first.");
            return true;
        }

        private byte[] LoadDeploymentScript(string nefFilePath, string manifestFilePath, JObject data, out NefFile nef, out ContractManifest manifest)
        {
            if (string.IsNullOrEmpty(manifestFilePath))
            {
                manifestFilePath = Path.ChangeExtension(nefFilePath, ".manifest.json");
            }

            // Read manifest

            var info = new FileInfo(manifestFilePath);
            if (!info.Exists || info.Length >= Transaction.MaxTransactionSize)
            {
                throw new ArgumentException(nameof(manifestFilePath));
            }

            manifest = ContractManifest.Parse(File.ReadAllBytes(manifestFilePath));

            // Read nef

            info = new FileInfo(nefFilePath);
            if (!info.Exists || info.Length >= Transaction.MaxTransactionSize)
            {
                throw new ArgumentException(nameof(nefFilePath));
            }

            nef = File.ReadAllBytes(nefFilePath).AsSerializable<NefFile>();

            ContractParameter dataParameter = null;
            if (data is not null)
                try
                {
                    dataParameter = ContractParameter.FromJson(data);
                }
                catch
                {
                    throw new FormatException("invalid data");
                }

            // Basic script checks
            nef.Script.IsScriptValid(manifest.Abi);

            // Build script

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                if (dataParameter is not null)
                    sb.EmitDynamicCall(NativeContract.ContractManagement.Hash, "deploy", nef.ToArray(), manifest.ToJson().ToString(), dataParameter);
                else
                    sb.EmitDynamicCall(NativeContract.ContractManagement.Hash, "deploy", nef.ToArray(), manifest.ToJson().ToString());
                return sb.ToArray();
            }
        }

        private byte[] LoadUpdateScript(UInt160 scriptHash, string nefFilePath, string manifestFilePath, JObject data, out NefFile nef, out ContractManifest manifest)
        {
            if (string.IsNullOrEmpty(manifestFilePath))
            {
                manifestFilePath = Path.ChangeExtension(nefFilePath, ".manifest.json");
            }

            // Read manifest

            var info = new FileInfo(manifestFilePath);
            if (!info.Exists || info.Length >= Transaction.MaxTransactionSize)
            {
                throw new ArgumentException(nameof(manifestFilePath));
            }

            manifest = ContractManifest.Parse(File.ReadAllBytes(manifestFilePath));

            // Read nef

            info = new FileInfo(nefFilePath);
            if (!info.Exists || info.Length >= Transaction.MaxTransactionSize)
            {
                throw new ArgumentException(nameof(nefFilePath));
            }

            nef = File.ReadAllBytes(nefFilePath).AsSerializable<NefFile>();

            ContractParameter dataParameter = null;
            if (data is not null)
                try
                {
                    dataParameter = ContractParameter.FromJson(data);
                }
                catch
                {
                    throw new FormatException("invalid data");
                }

            // Basic script checks
            nef.Script.IsScriptValid(manifest.Abi);

            // Build script

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                if (dataParameter is null)
                    sb.EmitDynamicCall(scriptHash, "update", nef.ToArray(), manifest.ToJson().ToString());
                else
                    sb.EmitDynamicCall(scriptHash, "update", nef.ToArray(), manifest.ToJson().ToString(), dataParameter);
                return sb.ToArray();
            }
        }

        public override void OnStart(string[] args)
        {
            base.OnStart(args);
            Start(args);
        }

        public override void OnStop()
        {
            base.OnStop();
            Stop();
        }

        public void OpenWallet(string path, string password)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException();
            }

            CurrentWallet = Wallet.Open(path, password, NeoSystem.Settings) ?? throw new NotSupportedException();
        }

        public async void Start(string[] args)
        {
            if (NeoSystem != null) return;
            bool verifyImport = true;
            for (int i = 0; i < args.Length; i++)
                switch (args[i])
                {
                    case "/noverify":
                    case "--noverify":
                        verifyImport = false;
                        break;
                }

            ProtocolSettings protocol = ProtocolSettings.Load("config.json");

            NeoSystem = new NeoSystem(protocol, Settings.Default.Storage.Engine, string.Format(Settings.Default.Storage.Path, protocol.Network.ToString("X8")));
            NeoSystem.AddService(this);

            LocalNode = NeoSystem.LocalNode.Ask<LocalNode>(new LocalNode.GetInstance()).Result;

            foreach (var plugin in Plugin.Plugins)
            {
                // Register plugins commands

                RegisterCommand(plugin, plugin.Name);
            }

            using (IEnumerator<Block> blocksBeingImported = GetBlocksFromFile().GetEnumerator())
            {
                while (true)
                {
                    List<Block> blocksToImport = new List<Block>();
                    for (int i = 0; i < 10; i++)
                    {
                        if (!blocksBeingImported.MoveNext()) break;
                        blocksToImport.Add(blocksBeingImported.Current);
                    }
                    if (blocksToImport.Count == 0) break;
                    await NeoSystem.Blockchain.Ask<Blockchain.ImportCompleted>(new Blockchain.Import
                    {
                        Blocks = blocksToImport,
                        Verify = verifyImport
                    });
                    if (NeoSystem is null) return;
                }
            }
            NeoSystem.StartNode(new ChannelsConfig
            {
                Tcp = new IPEndPoint(IPAddress.Any, Settings.Default.P2P.Port),
                WebSocket = new IPEndPoint(IPAddress.Any, Settings.Default.P2P.WsPort),
                MinDesiredConnections = Settings.Default.P2P.MinDesiredConnections,
                MaxConnections = Settings.Default.P2P.MaxConnections,
                MaxConnectionsPerAddress = Settings.Default.P2P.MaxConnectionsPerAddress
            });

            if (Settings.Default.UnlockWallet.IsActive)
            {
                try
                {
                    OpenWallet(Settings.Default.UnlockWallet.Path, Settings.Default.UnlockWallet.Password);
                }
                catch (FileNotFoundException)
                {
                    ConsoleHelper.Warning($"wallet file \"{Settings.Default.UnlockWallet.Path}\" not found.");
                }
                catch (System.Security.Cryptography.CryptographicException)
                {
                    ConsoleHelper.Error($"Failed to open file \"{Settings.Default.UnlockWallet.Path}\"");
                }
                catch (Exception ex)
                {
                    ConsoleHelper.Error(ex.GetBaseException().Message);
                }
            }
        }

        public void Stop()
        {
            Dispose_Logger();
            Interlocked.Exchange(ref _neoSystem, null)?.Dispose();
        }

        private void WriteBlocks(uint start, uint count, string path, bool writeStart)
        {
            uint end = start + count - 1;
            using FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.WriteThrough);
            if (fs.Length > 0)
            {
                byte[] buffer = new byte[sizeof(uint)];
                if (writeStart)
                {
                    fs.Seek(sizeof(uint), SeekOrigin.Begin);
                    fs.Read(buffer, 0, buffer.Length);
                    start += BitConverter.ToUInt32(buffer, 0);
                    fs.Seek(sizeof(uint), SeekOrigin.Begin);
                }
                else
                {
                    fs.Read(buffer, 0, buffer.Length);
                    start = BitConverter.ToUInt32(buffer, 0);
                    fs.Seek(0, SeekOrigin.Begin);
                }
            }
            else
            {
                if (writeStart)
                {
                    fs.Write(BitConverter.GetBytes(start), 0, sizeof(uint));
                }
            }
            if (start <= end)
                fs.Write(BitConverter.GetBytes(count), 0, sizeof(uint));
            fs.Seek(0, SeekOrigin.End);
            Console.WriteLine("Export block from " + start + " to " + end);

            using (var percent = new ConsolePercent(start, end))
            {
                for (uint i = start; i <= end; i++)
                {
                    Block block = NativeContract.Ledger.GetBlock(NeoSystem.StoreView, i);
                    byte[] array = block.ToArray();
                    fs.Write(BitConverter.GetBytes(array.Length), 0, sizeof(int));
                    fs.Write(array, 0, array.Length);
                    percent.Value = i;
                }
            }
        }

        private static void WriteLineWithoutFlicker(string message = "", int maxWidth = 80)
        {
            if (message.Length > 0) Console.Write(message);
            var spacesToErase = maxWidth - message.Length;
            if (spacesToErase < 0) spacesToErase = 0;
            Console.WriteLine(new string(' ', spacesToErase));
        }

        /// <summary>
        /// Make and send transaction with script, sender
        /// </summary>
        /// <param name="script">script</param>
        /// <param name="account">sender</param>
        /// <param name="gas">Max fee for running the script</param>
        private void SendTransaction(byte[] script, UInt160 account = null, long gas = TestModeGas)
        {
            Signer[] signers = Array.Empty<Signer>();
            var snapshot = NeoSystem.StoreView;

            if (account != null)
            {
                signers = CurrentWallet.GetAccounts()
                .Where(p => !p.Lock && !p.WatchOnly && p.ScriptHash == account && NativeContract.GAS.BalanceOf(snapshot, p.ScriptHash).Sign > 0)
                .Select(p => new Signer { Account = p.ScriptHash, Scopes = WitnessScope.CalledByEntry })
                .ToArray();
            }

            try
            {
                Transaction tx = CurrentWallet.MakeTransaction(snapshot, script, account, signers, maxGas: gas);
                ConsoleHelper.Info("Invoking script with: ", $"'{Convert.ToBase64String(tx.Script.Span)}'");

                using (ApplicationEngine engine = ApplicationEngine.Run(tx.Script, snapshot, container: tx, settings: NeoSystem.Settings, gas: gas))
                {
                    PrintExecutionOutput(engine, true);
                    if (engine.State == VMState.FAULT) return;
                }

                if (!ReadUserInput("Relay tx(no|yes)").IsYes())
                {
                    return;
                }

                SignAndSendTx(NeoSystem.StoreView, tx);
            }
            catch (InvalidOperationException e)
            {
                ConsoleHelper.Error(GetExceptionMessage(e));
            }
        }

        /// <summary>
        /// Process "invoke" command
        /// </summary>
        /// <param name="scriptHash">Script hash</param>
        /// <param name="operation">Operation</param>
        /// <param name="result">Result</param>
        /// <param name="verifiable">Transaction</param>
        /// <param name="contractParameters">Contract parameters</param>
        /// <param name="showStack">Show result stack if it is true</param>
        /// <param name="gas">Max fee for running the script</param>
        /// <returns>Return true if it was successful</returns>
        private bool OnInvokeWithResult(UInt160 scriptHash, string operation, out StackItem result, IVerifiable verifiable = null, JArray contractParameters = null, bool showStack = true, long gas = TestModeGas)
        {
            List<ContractParameter> parameters = new List<ContractParameter>();

            if (contractParameters != null)
            {
                foreach (var contractParameter in contractParameters)
                {
                    parameters.Add(ContractParameter.FromJson((JObject)contractParameter));
                }
            }

            ContractState contract = NativeContract.ContractManagement.GetContract(NeoSystem.StoreView, scriptHash);
            if (contract == null)
            {
                ConsoleHelper.Error("Contract does not exist.");
                result = StackItem.Null;
                return false;
            }
            else
            {
                if (contract.Manifest.Abi.GetMethod(operation, parameters.Count) == null)
                {
                    ConsoleHelper.Error("This method does not not exist in this contract.");
                    result = StackItem.Null;
                    return false;
                }
            }

            byte[] script;

            using (ScriptBuilder scriptBuilder = new ScriptBuilder())
            {
                scriptBuilder.EmitDynamicCall(scriptHash, operation, parameters.ToArray());
                script = scriptBuilder.ToArray();
                ConsoleHelper.Info("Invoking script with: ", $"'{script.ToBase64String()}'");
            }

            if (verifiable is Transaction tx)
            {
                tx.Script = script;
            }

            using ApplicationEngine engine = ApplicationEngine.Run(script, NeoSystem.StoreView, container: verifiable, settings: NeoSystem.Settings, gas: gas);
            PrintExecutionOutput(engine, showStack);
            result = engine.State == VMState.FAULT ? null : engine.ResultStack.Peek();
            return engine.State != VMState.FAULT;
        }

        private void PrintExecutionOutput(ApplicationEngine engine, bool showStack = true)
        {
            ConsoleHelper.Info("VM State: ", engine.State.ToString());
            ConsoleHelper.Info("Gas Consumed: ", new BigDecimal((BigInteger)engine.GasConsumed, NativeContract.GAS.Decimals).ToString());

            if (showStack)
                ConsoleHelper.Info("Result Stack: ", new JArray(engine.ResultStack.Select(p => p.ToJson())).ToString());

            if (engine.State == VMState.FAULT)
                ConsoleHelper.Error(GetExceptionMessage(engine.FaultException));
        }

        static string GetExceptionMessage(Exception exception)
        {
            if (exception == null) return "Engine faulted.";

            if (exception.InnerException != null)
            {
                return GetExceptionMessage(exception.InnerException);
            }

            return exception.Message;
        }

        public UInt160 ResolveNeoNameServiceAddress(string domain)
        {
            if (Settings.Default.NNS.Contract == UInt160.Zero)
                throw new Exception("Neo Name Service (NNS): is disabled on this network.");

            using var sb = new ScriptBuilder();
            sb.EmitDynamicCall(Settings.Default.NNS.Contract, "resolve", domain, 16);

            using var appEng = ApplicationEngine.Run(sb.ToArray(), NeoSystem.StoreView, settings: NeoSystem.Settings);
            if (appEng.State == VMState.HALT)
            {
                var data = appEng.ResultStack.Pop();
                if (data is ByteString)
                {
                    try
                    {
                        var addressData = data.GetString();
                        if (UInt160.TryParse(addressData, out var address))
                            return address;
                        else
                            return addressData.ToScriptHash(NeoSystem.Settings.AddressVersion);
                    }
                    catch
                    {
                        throw new Exception("Neo Name Service (NNS): Record invalid address format.");
                    }
                }
            }
            throw new Exception($"Neo Name Service (NNS): \"{domain}\" domain not found.");
        }
    }
}
