// Copyright (C) 2015-2025 The Neo Project.
//
// MainService.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Neo.ConsoleService;
using Neo.Extensions;
using Neo.Json;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Persistence.Providers;
using Neo.Plugins;
using Neo.Sign;
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
using System.Linq;
using System.Net;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Array = System.Array;
using ECCurve = Neo.Cryptography.ECC.ECCurve;
using ECPoint = Neo.Cryptography.ECC.ECPoint;

namespace Neo.CLI
{
    public partial class MainService : ConsoleServiceBase, IWalletProvider
    {
        public event EventHandler<Wallet?>? WalletChanged = null;

        public const long TestModeGas = 20_00000000;

        private Wallet? _currentWallet;

        public Wallet? CurrentWallet
        {
            get => _currentWallet;
            private set
            {
                _currentWallet = value;
                WalletChanged?.Invoke(this, value);
            }
        }

        private NeoSystem? _neoSystem;
        public NeoSystem NeoSystem
        {
            get => _neoSystem!;
            private set => _neoSystem = value;
        }

        private LocalNode? _localNode;

        public LocalNode LocalNode
        {
            get => _localNode!;
            private set => _localNode = value;
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
            RegisterCommandHandler<string, JToken>(str => JToken.Parse(str)!);
            RegisterCommandHandler<string, JObject>(str => (JObject)JToken.Parse(str)!);
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

            if (input.IndexOf('.') > 0 && input.LastIndexOf('.') < input.Length)
            {
                return ResolveNeoNameServiceAddress(input) ?? UInt160.Zero;
            }

            // Try to parse as UInt160

            if (UInt160.TryParse(input, out var addr))
            {
                return addr;
            }

            // Accept wallet format

            return input.ToScriptHash(version);
        }

        Wallet? IWalletProvider.GetWallet()
        {
            return CurrentWallet;
        }

        public override void RunConsole()
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;

            var cliV = Assembly.GetAssembly(typeof(Program))!.GetName().Version;
            var neoV = Assembly.GetAssembly(typeof(NeoSystem))!.GetName().Version;
            var vmV = Assembly.GetAssembly(typeof(ExecutionEngine))!.GetName().Version;
            Console.WriteLine($"{ServiceName} v{cliV?.ToString(3)}  -  NEO v{neoV?.ToString(3)}  -  NEO-VM v{vmV?.ToString(3)}");
            Console.WriteLine();

            base.RunConsole();
        }

        public void CreateWallet(string path, string password, bool createDefaultAccount = true, string? walletName = null)
        {
            Wallet wallet = Wallet.Create(walletName, path, password, NeoSystem.Settings);
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
            SignerManager.RegisterSigner(wallet.Name, wallet);
        }

        private bool NoWallet()
        {
            if (CurrentWallet != null) return false;
            ConsoleHelper.Error("You have to open the wallet first.");
            return true;
        }

        private static ContractParameter? LoadScript(string nefFilePath, string? manifestFilePath, JObject? data,
            out NefFile nef, out ContractManifest manifest)
        {
            if (string.IsNullOrEmpty(manifestFilePath))
                manifestFilePath = Path.ChangeExtension(nefFilePath, ".manifest.json");

            // Read manifest
            var info = new FileInfo(manifestFilePath);
            if (!info.Exists)
                throw new ArgumentException($"Contract manifest file not found at path: {manifestFilePath}. Please ensure the manifest file exists and the path is correct.", nameof(manifestFilePath));
            if (info.Length >= Transaction.MaxTransactionSize)
                throw new ArgumentException($"Contract manifest file size ({info.Length} bytes) exceeds the maximum allowed transaction size ({Transaction.MaxTransactionSize} bytes). Please check the file size and ensure it's within limits.", nameof(manifestFilePath));

            manifest = ContractManifest.Parse(File.ReadAllBytes(manifestFilePath));

            // Read nef
            info = new FileInfo(nefFilePath);
            if (!info.Exists)
                throw new ArgumentException($"Contract NEF file not found at path: {nefFilePath}. Please ensure the NEF file exists and the path is correct.", nameof(nefFilePath));
            if (info.Length >= Transaction.MaxTransactionSize)
                throw new ArgumentException($"Contract NEF file size ({info.Length} bytes) exceeds the maximum allowed transaction size ({Transaction.MaxTransactionSize} bytes). Please check the file size and ensure it's within limits.", nameof(nefFilePath));

            nef = File.ReadAllBytes(nefFilePath).AsSerializable<NefFile>();

            // Basic script checks
            nef.Script.IsScriptValid(manifest.Abi);

            if (data is not null)
            {
                try
                {
                    return ContractParameter.FromJson(data);
                }
                catch (Exception ex)
                {
                    throw new FormatException($"Invalid contract deployment data format. The provided JSON data could not be parsed as valid contract parameters. Original error: {ex.Message}", ex);
                }
            }

            return null;
        }

        private byte[] LoadDeploymentScript(string nefFilePath, string? manifestFilePath, JObject? data,
            out NefFile nef, out ContractManifest manifest)
        {
            var parameter = LoadScript(nefFilePath, manifestFilePath, data, out nef, out manifest);
            var manifestJson = manifest.ToJson().ToString();

            // Build script
            using (var sb = new ScriptBuilder())
            {
                if (parameter is not null)
                    sb.EmitDynamicCall(NativeContract.ContractManagement.Hash, "deploy", nef.ToArray(), manifestJson, parameter);
                else
                    sb.EmitDynamicCall(NativeContract.ContractManagement.Hash, "deploy", nef.ToArray(), manifestJson);
                return sb.ToArray();
            }
        }

        private byte[] LoadUpdateScript(UInt160 scriptHash, string nefFilePath, string manifestFilePath, JObject? data,
            out NefFile nef, out ContractManifest manifest)
        {
            var parameter = LoadScript(nefFilePath, manifestFilePath, data, out nef, out manifest);
            var manifestJson = manifest.ToJson().ToString();

            // Build script
            using (var sb = new ScriptBuilder())
            {
                if (parameter is null)
                    sb.EmitDynamicCall(scriptHash, "update", nef.ToArray(), manifestJson);
                else
                    sb.EmitDynamicCall(scriptHash, "update", nef.ToArray(), manifestJson, parameter);
                return sb.ToArray();
            }
        }

        public override bool OnStart(string[] args)
        {
            if (!base.OnStart(args)) return false;
            return OnStartWithCommandLine(args) != 1;
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
                throw new FileNotFoundException($"Wallet file not found at path: {path}. Please verify the file path is correct and the wallet file exists.", path);
            }

            if (CurrentWallet is not null) SignerManager.UnregisterSigner(CurrentWallet.Name);

            CurrentWallet = Wallet.Open(path, password, NeoSystem.Settings) ?? throw new NotSupportedException($"Failed to open wallet at path: {path}. The wallet format may not be supported or the password may be incorrect. Please verify the wallet file integrity and password.");
            SignerManager.RegisterSigner(CurrentWallet.Name, CurrentWallet);
        }

        private static void ShowDllNotFoundError(DllNotFoundException ex)
        {
            void DisplayError(string primaryMessage, string? secondaryMessage = null)
            {
                ConsoleHelper.Error(primaryMessage + Environment.NewLine +
                                    (secondaryMessage != null ? secondaryMessage + Environment.NewLine : "") +
                                    "Press any key to exit.");
                Console.ReadKey();
                Environment.Exit(-1);
            }

            const string neoUrl = "https://github.com/neo-project/neo/releases";
            const string levelDbUrl = "https://github.com/neo-ngd/leveldb/releases";
            if (ex.Message.Contains("libleveldb"))
            {
                if (OperatingSystem.IsWindows())
                {
                    if (File.Exists("libleveldb.dll"))
                    {
                        DisplayError("Dependency DLL not found, please install Microsoft Visual C++ Redistributable.",
                            "See https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist");
                    }
                    else
                    {
                        DisplayError("DLL not found, please get libleveldb.dll.", $"Download from {levelDbUrl}");
                    }
                }
                else if (OperatingSystem.IsLinux())
                {
                    DisplayError("Shared library libleveldb.so not found, please get libleveldb.so.",
                        $"Use command \"sudo apt-get install libleveldb-dev\" in terminal or download from {levelDbUrl}");
                }
                else if (OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst())
                {
                    // Check if the error message contains information about missing dependencies
                    if (ex.Message.Contains("libtcmalloc") && ex.Message.Contains("gperftools"))
                    {
                        DisplayError("LevelDB dependency 'gperftools' not found. This is required for libleveldb on macOS.",
                            "To fix this issue:\n" +
                            "1. Install gperftools: brew install gperftools\n" +
                            "2. Install leveldb: brew install leveldb\n" +
                            "3. If the issue persists, try: brew reinstall gperftools leveldb\n" +
                            "\n" +
                            "Note: The system is looking for libtcmalloc.4.dylib which is provided by gperftools.");
                    }
                    else
                    {
                        DisplayError("Shared library libleveldb.dylib not found or has missing dependencies.",
                            "To fix this issue:\n" +
                            "1. Install dependencies: brew install gperftools snappy\n" +
                            "2. Install leveldb: brew install leveldb\n" +
                            "3. If already installed, try: brew reinstall gperftools leveldb\n" +
                            $"\n" +
                            $"Alternative: Download pre-compiled binaries from {levelDbUrl}");
                    }
                }
                else
                {
                    DisplayError("Neo CLI is broken, please reinstall it.", $"Download from {neoUrl}");
                }
            }
            else
            {
                DisplayError("Neo CLI is broken, please reinstall it.", $"Download from {neoUrl}");
            }
        }

        public async void Start(CommandLineOptions options)
        {
            if (NeoSystem != null) return;
            bool verifyImport = !(options.NoVerify ?? false);

            Utility.LogLevel = options.Verbose;
            var protocol = ProtocolSettings.Load("config.json");
            CustomProtocolSettings(options, protocol);
            CustomApplicationSettings(options, Settings.Default);

            var engineConfig = Settings.Default.Storage.Engine;
            var engine = engineConfig;
            if (string.IsNullOrWhiteSpace(engineConfig))
            {
                ConsoleHelper.Warning("No persistence engine specified, using MemoryStore now");
                engine = nameof(MemoryStore);
            }

            var storagePath = string.Format(Settings.Default.Storage.Path, protocol.Network.ToString("X8"));

            if (!engine.Equals(nameof(MemoryStore), StringComparison.OrdinalIgnoreCase))
            {
                var pluginDir = Plugin.PluginsDirectory;
                var hasProviderDLL = Directory.Exists(pluginDir) && Directory.EnumerateFiles(pluginDir, $"*{engine}*.dll", SearchOption.AllDirectories).Any();
                if (!hasProviderDLL)
                {
                    ConsoleHelper.Info($"Storage provider {engine} not found in {pluginDir}. Auto-install {engine}.", nameof(Settings.Default.Storage.Engine));
                    if (!await InstallPluginAsync(engine))
                    {
                        throw new ArgumentException($"Not possible to install provider {engine}", nameof(Settings.Default.Storage.Engine));
                    }
                }
            }

            try
            {
                NeoSystem = new NeoSystem(protocol, engine, storagePath);
            }
            catch (DllNotFoundException ex)
            {
                ShowDllNotFoundError(ex);
                return;
            }

            NeoSystem.AddService(this);

            LocalNode = NeoSystem.LocalNode.Ask<LocalNode>(new LocalNode.GetInstance()).Result;

            // installing plugins
            var installTasks = options.Plugins?.Select(p => p)
                .Where(p => !string.IsNullOrEmpty(p))
                .ToList()
                .Select(p => InstallPluginAsync(p));
            if (installTasks is not null)
            {
                await Task.WhenAll(installTasks);
            }

            foreach (var plugin in Plugin.Plugins)
            {
                // Register plugins commands
                RegisterCommand(plugin, plugin.Name);
            }

            await ImportBlocksFromFile(verifyImport);

            NeoSystem.StartNode(new ChannelsConfig
            {
                Tcp = new IPEndPoint(IPAddress.Any, Settings.Default.P2P.Port),
                MinDesiredConnections = Settings.Default.P2P.MinDesiredConnections,
                MaxConnections = Settings.Default.P2P.MaxConnections,
                MaxKnownHashes = Settings.Default.P2P.MaxKnownHashes,
                MaxConnectionsPerAddress = Settings.Default.P2P.MaxConnectionsPerAddress
            });

            if (Settings.Default.UnlockWallet.IsActive)
            {
                try
                {
                    if (Settings.Default.UnlockWallet.Path is null)
                    {
                        ConsoleHelper.Error("UnlockWallet.Path must be defined");
                    }
                    else if (Settings.Default.UnlockWallet.Password is null)
                    {
                        ConsoleHelper.Error("UnlockWallet.Password must be defined");
                    }
                    else
                    {
                        OpenWallet(Settings.Default.UnlockWallet.Path, Settings.Default.UnlockWallet.Password);
                    }
                }
                catch (FileNotFoundException)
                {
                    ConsoleHelper.Warning($"wallet file \"{Path.GetFullPath(Settings.Default.UnlockWallet.Path!)}\" not found.");
                }
                catch (CryptographicException)
                {
                    ConsoleHelper.Error($"Failed to open file \"{Path.GetFullPath(Settings.Default.UnlockWallet.Path!)}\"");
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

        /// <summary>
        /// Make and send transaction with script, sender
        /// </summary>
        /// <param name="script">script</param>
        /// <param name="account">sender</param>
        /// <param name="datoshi">Max fee for running the script, in the unit of datoshi, 1 datoshi = 1e-8 GAS</param>
        private void SendTransaction(byte[] script, UInt160? account = null, long datoshi = TestModeGas)
        {
            if (NoWallet()) return;

            var signers = Array.Empty<Signer>();
            var snapshot = NeoSystem.StoreView;
            if (account != null)
            {
                signers = CurrentWallet!.GetAccounts()
                .Where(p => !p.Lock && !p.WatchOnly && p.ScriptHash == account && NativeContract.GAS.BalanceOf(snapshot, p.ScriptHash).Sign > 0)
                .Select(p => new Signer { Account = p.ScriptHash, Scopes = WitnessScope.CalledByEntry })
                .ToArray();
            }

            try
            {
                var tx = CurrentWallet!.MakeTransaction(snapshot, script, account, signers, maxGas: datoshi);
                ConsoleHelper.Info("Invoking script with: ", $"'{Convert.ToBase64String(tx.Script.Span)}'");
                using (var engine = ApplicationEngine.Run(tx.Script, snapshot, container: tx, settings: NeoSystem.Settings, gas: datoshi))
                {
                    PrintExecutionOutput(engine, true);
                    if (engine.State == VMState.FAULT) return;
                }

                if (!ConsoleHelper.ReadUserInput("Relay tx(no|yes)").IsYes())
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
        /// <param name="datoshi">Max fee for running the script, in the unit of datoshi, 1 datoshi = 1e-8 GAS</param>
        /// <returns>Return true if it was successful</returns>
        private bool OnInvokeWithResult(UInt160 scriptHash, string operation, out StackItem result,
            IVerifiable? verifiable = null, JArray? contractParameters = null, bool showStack = true, long datoshi = TestModeGas)
        {
            var parameters = new List<ContractParameter>();
            if (contractParameters != null)
            {
                foreach (var contractParameter in contractParameters)
                {
                    if (contractParameter is not null)
                    {
                        parameters.Add(ContractParameter.FromJson((JObject)contractParameter));
                    }
                }
            }

            var contract = NativeContract.ContractManagement.GetContract(NeoSystem.StoreView, scriptHash);
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
            using (var scriptBuilder = new ScriptBuilder())
            {
                scriptBuilder.EmitDynamicCall(scriptHash, operation, parameters.ToArray());
                script = scriptBuilder.ToArray();
                ConsoleHelper.Info("Invoking script with: ", $"'{script.ToBase64String()}'");
            }

            if (verifiable is Transaction tx)
            {
                tx.Script = script;
            }

            using var engine = ApplicationEngine.Run(script, NeoSystem.StoreView, container: verifiable, settings: NeoSystem.Settings, gas: datoshi);
            PrintExecutionOutput(engine, showStack);
            result = engine.State == VMState.FAULT ? StackItem.Null : engine.ResultStack.Peek();
            return engine.State != VMState.FAULT;
        }

        private void PrintExecutionOutput(ApplicationEngine engine, bool showStack = true)
        {
            ConsoleHelper.Info("VM State: ", engine.State.ToString());
            ConsoleHelper.Info("Gas Consumed: ", new BigDecimal((BigInteger)engine.FeeConsumed, NativeContract.GAS.Decimals).ToString());

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

        public UInt160? ResolveNeoNameServiceAddress(string domain)
        {
            if (Settings.Default.Contracts.NeoNameService == UInt160.Zero)
                throw new Exception($"Neo Name Service (NNS) is not available on the current network. The NNS contract is not configured for network: {NeoSystem.Settings.Network}. Please ensure you are connected to a network that supports NNS functionality.");

            using var sb = new ScriptBuilder();
            sb.EmitDynamicCall(Settings.Default.Contracts.NeoNameService, "resolve", CallFlags.ReadOnly, domain, 16);

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
                            return addressData?.ToScriptHash(NeoSystem.Settings.AddressVersion);
                    }
                    catch { }
                }
                else if (data is Null)
                {
                    throw new Exception($"Neo Name Service (NNS): Domain '{domain}' was not found in the NNS registry. Please verify the domain name is correct and has been registered in the NNS system.");
                }
                throw new Exception($"Neo Name Service (NNS): The resolved record for domain '{domain}' contains an invalid address format. The NNS record exists but the address data is not in the expected format.");
            }
            else
            {
                if (appEng.FaultException is not null)
                {
                    throw new Exception($"Neo Name Service (NNS): Failed to resolve domain '{domain}' due to contract execution error: {appEng.FaultException.Message}. Please verify the domain exists and try again.");
                }
            }
            throw new Exception($"Neo Name Service (NNS): Domain '{domain}' was not found in the NNS registry. The resolution operation completed but no valid record was returned. Please verify the domain name is correct and has been registered.");
        }
    }
}
