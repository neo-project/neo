// Copyright (C) 2015-2025 The Neo Project.
//
// NativeContractBenchmarkContext.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.IO;
using Neo.Json;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Neo.Benchmarks.NativeContracts
{
    /// <summary>
    /// Provides access to shared runtime assets needed to invoke native contract methods.
    /// </summary>
    public sealed class NativeContractBenchmarkContext : IDisposable
    {
        private long _nonceCounter;
        private readonly Signer[] _templateSigners;
        private readonly UInt160[] _benchmarkAccounts;
        private uint _seededNotaryTill;
        private uint _seededLedgerHeight;
        private bool _hasSeededNotaryTill;
        private readonly UInt160 _callbackContractHash;
        private readonly Dictionary<NativeContractInputSize, UInt160> _contractManagementCallers = new();
        private readonly KeyPair _notaryKeyPair;
        private readonly UInt160 _notaryDepositAccount;
        private BigInteger _neoRegisterPrice;
        private UInt256 _seededLedgerTransactionHash = UInt256.Zero;
        private const ulong OracleSeedRequestId = 0;
        private static readonly byte[] s_notaryPrivateKey = Convert.FromHexString("0102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F20");
        private static readonly UInt160 s_notaryAttributeHash = Neo.SmartContract.Helper.GetContractHash(UInt160.Zero, 0, "Notary");
        private static readonly BigInteger s_notarySeedDepositAmount = NativeContract.GAS.Factor * 100;

        public NativeContractBenchmarkContext(NeoSystem system, ProtocolSettings protocolSettings)
        {
            System = system ?? throw new ArgumentNullException(nameof(system));
            ProtocolSettings = protocolSettings ?? throw new ArgumentNullException(nameof(protocolSettings));
            _benchmarkAccounts = BuildBenchmarkAccounts(protocolSettings);
            _notaryKeyPair = new KeyPair(s_notaryPrivateKey);
            _notaryDepositAccount = _benchmarkAccounts[0];
            _templateSigners = BuildDefaultSigners(_benchmarkAccounts);
            SeedPolicyContractDefaults();
            SeedLedgerContractDefaults();
            SeedRoleManagementDefaults();
            SeedContractManagementDefaults();
            SeedNotaryDefaults();
            _callbackContractHash = SeedOracleCallbackContract();
            SeedOracleContractDefaults();
            SeedNeoTokenDefaults();
        }

        public NeoSystem System { get; }

        public ProtocolSettings ProtocolSettings { get; }

        public StoreCache GetSnapshot() => System.GetSnapshotCache();

        public DataCache StoreView => System.StoreView;

        public ApplicationEngine CreateEngine(StoreCache snapshot, NativeContractBenchmarkCase benchmarkCase)
        {
            ArgumentNullException.ThrowIfNull(snapshot);
            var tx = CreateBenchmarkTransaction(benchmarkCase);
            return ApplicationEngine.Create(
                trigger: TriggerType.Application,
                container: tx,
                snapshot: snapshot,
                persistingBlock: System.GenesisBlock,
                settings: ProtocolSettings,
                gas: tx.SystemFee + tx.NetworkFee);
        }

        private Transaction CreateBenchmarkTransaction(NativeContractBenchmarkCase benchmarkCase)
        {
            var nonce = unchecked((uint)Interlocked.Increment(ref _nonceCounter));
            List<TransactionAttribute> attributes = new();

            var transaction = new Transaction
            {
                Version = 0,
                Nonce = nonce,
                SystemFee = 20_00000000,
                NetworkFee = 1_00000000,
                ValidUntilBlock = ProtocolSettings.MaxTraceableBlocks,
                Signers = CloneSigners(_templateSigners),
                Script = global::System.Array.Empty<byte>(),
                Witnesses = global::System.Array.Empty<Witness>()
            };

            if (RequiresOracleResponseAttribute(benchmarkCase))
            {
                attributes.Add(new OracleResponse
                {
                    Id = OracleSeedRequestId,
                    Code = OracleResponseCode.Success,
                    Result = Array.Empty<byte>()
                });
            }

            if (RequiresNotaryAssistedAttribute(benchmarkCase))
            {
                attributes.Add(new NotaryAssisted
                {
                    NKeys = 1
                });
            }

            transaction.Attributes = attributes.ToArray();

            return transaction;
        }

        public UInt160 GetAccount(NativeContractInputSize size, int slot = 0)
        {
            int index = ((int)size + slot) % _benchmarkAccounts.Length;
            return _benchmarkAccounts[index];
        }

        public UInt160 PrimaryBenchmarkAccount => _benchmarkAccounts[0];

        public UInt160 CallbackContractHash => _callbackContractHash;

        public UInt160 NotaryDepositAccount => _notaryDepositAccount;

        public BigInteger NeoRegisterPrice => _neoRegisterPrice;

        public bool TryGetContractManagementCaller(NativeContractInputSize size, out UInt160 hash)
        {
            return _contractManagementCallers.TryGetValue(size, out hash);
        }

        public uint SeededLedgerHeight => _seededLedgerHeight;

        public uint GetSeededNotaryTill()
        {
            if (!_hasSeededNotaryTill)
                return Math.Max(ProtocolSettings.MaxTraceableBlocks, 100u);
            return _seededNotaryTill;
        }

        public byte[] CreateNotaryVerificationSignature(Transaction tx)
        {
            ArgumentNullException.ThrowIfNull(tx);
            var hash = tx.GetSignData(ProtocolSettings.Network);
            return Crypto.Sign(hash, _notaryKeyPair.PrivateKey);
        }

        public void Dispose()
        {
            System.Dispose();
        }

        private static UInt160[] BuildBenchmarkAccounts(ProtocolSettings settings)
        {
            var committee = settings.StandbyCommittee;
            int m = committee.Count - (committee.Count - 1) / 2;
            var committeeAccount = Contract.CreateMultiSigRedeemScript(m, committee).ToScriptHash();

            List<UInt160> accounts =
            [
                committeeAccount
            ];

            accounts.AddRange(committee.Take(Math.Min(3, committee.Count))
                .Select(p => Contract.CreateSignatureRedeemScript(p).ToScriptHash()));

            accounts.Add(NativeContract.Notary.Hash);
            accounts.Add(s_notaryAttributeHash);
            accounts.Add(UInt160.Zero);
            return accounts.Distinct().ToArray();
        }

        private static Signer[] BuildDefaultSigners(UInt160[] accounts)
        {
            return accounts.Select(account => new Signer
            {
                Account = account,
                Scopes = account == NativeContract.Notary.Hash ? WitnessScope.None : WitnessScope.Global
            }).ToArray();
        }

        private static Signer[] CloneSigners(Signer[] source)
        {
            if (source is null || source.Length == 0)
                return global::System.Array.Empty<Signer>();

            var clones = new Signer[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                var signer = source[i];
                clones[i] = new Signer
                {
                    Account = signer.Account,
                    Scopes = signer.Scopes,
                    AllowedContracts = signer.AllowedContracts is null ? null : signer.AllowedContracts.ToArray(),
                    AllowedGroups = signer.AllowedGroups is null ? null : signer.AllowedGroups.ToArray(),
                    Rules = signer.Rules is null ? null : signer.Rules.ToArray()
                };
            }

            return clones;
        }

        private static bool RequiresOracleResponseAttribute(NativeContractBenchmarkCase benchmarkCase)
        {
            return benchmarkCase is not null &&
                   benchmarkCase.ContractName == nameof(OracleContract) &&
                   string.Equals(benchmarkCase.MethodName, "Finish", StringComparison.OrdinalIgnoreCase);
        }

        private static bool RequiresNotaryAssistedAttribute(NativeContractBenchmarkCase benchmarkCase)
        {
            if (benchmarkCase is null)
                return false;

            if (benchmarkCase.ContractName != nameof(Notary))
                return false;

            return string.Equals(benchmarkCase.MethodName, "Verify", StringComparison.OrdinalIgnoreCase);
        }

        private void SeedPolicyContractDefaults()
        {
            var policy = NativeContract.Policy;
            using var snapshot = System.GetSnapshotCache();
            var seeded = false;
            seeded |= EnsurePolicyValue(snapshot, policy, "_feePerByte", new StorageItem(PolicyContract.DefaultFeePerByte));
            seeded |= EnsurePolicyValue(snapshot, policy, "_execFeeFactor", new StorageItem(PolicyContract.DefaultExecFeeFactor));
            seeded |= EnsurePolicyValue(snapshot, policy, "_storagePrice", new StorageItem(PolicyContract.DefaultStoragePrice));
            seeded |= EnsurePolicyValue(snapshot, policy, "_millisecondsPerBlock", new StorageItem(ProtocolSettings.MillisecondsPerBlock));
            seeded |= EnsurePolicyValue(snapshot, policy, "_maxValidUntilBlockIncrement", new StorageItem(ProtocolSettings.MaxValidUntilBlockIncrement));
            seeded |= EnsurePolicyValue(snapshot, policy, "_maxTraceableBlocks", new StorageItem(ProtocolSettings.MaxTraceableBlocks));
            if (seeded)
                snapshot.Commit();
        }

        private static bool EnsurePolicyValue(StoreCache snapshot, PolicyContract policy, string fieldName, StorageItem value)
        {
            var field = typeof(PolicyContract).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException($"Unable to access field {fieldName} on PolicyContract.");
            var key = (StorageKey)field.GetValue(policy)!;
            if (snapshot.Contains(key))
                return false;
            snapshot.Add(key, value);
            return true;
        }

        private void SeedOracleContractDefaults()
        {
            var oracle = NativeContract.Oracle;
            using var snapshot = System.GetSnapshotCache();
            var mutated = false;

            var priceKey = StorageKey.Create(oracle.Id, GetOraclePrefix("Prefix_Price"));
            if (!snapshot.Contains(priceKey))
            {
                snapshot.Add(priceKey, new StorageItem(0_50000000));
                mutated = true;
            }

            var requestIdKey = StorageKey.Create(oracle.Id, GetOraclePrefix("Prefix_RequestId"));
            var requestIdItem = new StorageItem(BigInteger.One);
            if (snapshot.Contains(requestIdKey))
            {
                snapshot.GetAndChange(requestIdKey).Value = requestIdItem.Value;
            }
            else
            {
                snapshot.Add(requestIdKey, requestIdItem);
                mutated = true;
            }

            var requestId = OracleSeedRequestId;
            const string seedUrl = "https://oracle.neo/seed";
            var requestKey = StorageKey.Create(oracle.Id, GetOraclePrefix("Prefix_Request"), requestId);
            if (!snapshot.Contains(requestKey))
            {
                var userData = BinarySerializer.Serialize(Neo.VM.Types.StackItem.Null, ExecutionEngineLimits.Default);
                var request = new OracleRequest
                {
                    OriginalTxid = _seededLedgerTransactionHash == UInt256.Zero
                        ? UInt256.Zero
                        : _seededLedgerTransactionHash,
                    GasForResponse = 1_0000000,
                    Url = seedUrl,
                    Filter = string.Empty,
                    CallbackContract = _callbackContractHash,
                    CallbackMethod = "oracleHandler",
                    UserData = userData
                };
                snapshot.Add(requestKey, StorageItem.CreateSealed(request));
                mutated = true;
            }

            var urlHash = Crypto.Hash160(Encoding.UTF8.GetBytes(seedUrl));
            var listKey = StorageKey.Create(oracle.Id, GetOraclePrefix("Prefix_IdList"), urlHash);
            if (!snapshot.Contains(listKey))
            {
                var idListType = typeof(OracleContract).GetNestedType("IdList", BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("Unable to access OracleContract.IdList.");
                var idList = (IInteroperable)Activator.CreateInstance(idListType)!;
                idListType.GetMethod("Add")!.Invoke(idList, new object[] { requestId });
                snapshot.Add(listKey, StorageItem.CreateSealed(idList));
                mutated = true;
            }

            if (mutated)
                snapshot.Commit();
        }

        private static byte GetOraclePrefix(string name)
        {
            var field = typeof(OracleContract).GetField(name, BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException($"Unable to access field {name} on OracleContract.");
            return (byte)field.GetValue(null)!;
        }

        private void SeedLedgerContractDefaults()
        {
            var ledger = NativeContract.Ledger;
            using var snapshot = System.GetSnapshotCache();
            var currentKey = (StorageKey)typeof(LedgerContract)
                .GetField("_currentBlock", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(ledger)!;

            var block = CreateLedgerSeedBlock();
            _seededLedgerTransactionHash = block.Transactions.FirstOrDefault()?.Hash ?? UInt256.Zero;
            var blockHashPrefix = GetLedgerPrefix("Prefix_BlockHash");
            var blockHashKey = StorageKey.Create(ledger.Id, blockHashPrefix, block.Index);
            if (snapshot.Contains(blockHashKey))
                snapshot.Delete(blockHashKey);
            snapshot.Add(blockHashKey, new StorageItem(block.Hash.ToArray()));

            var blockPrefix = GetLedgerPrefix("Prefix_Block");
            var trimmed = TrimmedBlock.Create(block);
            var blockKey = StorageKey.Create(ledger.Id, blockPrefix, block.Hash);
            if (snapshot.Contains(blockKey))
                snapshot.Delete(blockKey);
            snapshot.Add(blockKey, new StorageItem(trimmed.ToArray()));

            var txPrefix = GetLedgerPrefix("Prefix_Transaction");
            foreach (var tx in block.Transactions)
            {
                var state = new TransactionState
                {
                    BlockIndex = block.Index,
                    Transaction = tx,
                    State = VMState.HALT
                };
                var txKey = StorageKey.Create(ledger.Id, txPrefix, tx.Hash);
                if (snapshot.Contains(txKey))
                    snapshot.Delete(txKey);
                snapshot.Add(txKey, StorageItem.CreateSealed(state));
            }

            if (snapshot.Contains(currentKey))
                snapshot.Delete(currentKey);
            snapshot.Add(currentKey, CreateHashIndexStorageItem(block.Hash, block.Index));
            _seededLedgerHeight = block.Index;

            snapshot.Commit();
        }

        private static byte GetLedgerPrefix(string name)
        {
            var field = typeof(LedgerContract).GetField(name, BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException($"Unable to access field {name} on LedgerContract.");
            return (byte)field.GetValue(null)!;
        }

        private static StorageItem CreateHashIndexStorageItem(UInt256 hash, uint index)
        {
            var type = typeof(LedgerContract).Assembly.GetType("Neo.SmartContract.Native.HashIndexState", throwOnError: true)!;
            var instance = (IInteroperable)Activator.CreateInstance(type)!;
            type.GetProperty("Hash")!.SetValue(instance, hash);
            type.GetProperty("Index")!.SetValue(instance, index);
            return StorageItem.CreateSealed(instance);
        }

        private Block CreateLedgerSeedBlock()
        {
            var tx = CreateLedgerSeedTransaction();

            var hashes = new[] { tx.Hash };
            var nextConsensus = Contract.GetBFTAddress(
                ProtocolSettings.StandbyCommittee
                    .Take(Math.Max(1, ProtocolSettings.ValidatorsCount))
                    .ToArray());

            var header = new Header
            {
                PrevHash = UInt256.Zero,
                MerkleRoot = MerkleTree.ComputeRoot(hashes),
                Timestamp = System.GenesisBlock.Timestamp + ProtocolSettings.MillisecondsPerBlock,
                Nonce = 1,
                Index = 0,
                PrimaryIndex = 0,
                NextConsensus = nextConsensus,
                Witness = new Witness
                {
                    InvocationScript = ReadOnlyMemory<byte>.Empty,
                    VerificationScript = new[] { (byte)OpCode.PUSH1 }
                }
            };

            return new Block
            {
                Header = header,
                Transactions = new[] { tx }
            };
        }

        private Transaction CreateLedgerSeedTransaction()
        {
            var signer = new Signer
            {
                Account = _benchmarkAccounts[0],
                Scopes = WitnessScope.Global
            };

            return new Transaction
            {
                Version = 0,
                Nonce = unchecked((uint)Interlocked.Increment(ref _nonceCounter)),
                Script = new[] { (byte)OpCode.RET },
                SystemFee = 1_00000000,
                NetworkFee = 1_0000000,
                ValidUntilBlock = ProtocolSettings.MaxTraceableBlocks,
                Signers = new[] { signer },
                Attributes = Array.Empty<TransactionAttribute>(),
                Witnesses = new[]
                {
                    new Witness
                    {
                        InvocationScript = ReadOnlyMemory<byte>.Empty,
                        VerificationScript = new[] { (byte)OpCode.PUSH1 }
                    }
                }
            };
        }

        private void SeedNotaryDefaults()
        {
            var notary = NativeContract.Notary;
            using var snapshot = System.GetSnapshotCache();
            var mutated = false;

            var defaultDeltaField = typeof(Notary).GetField("DefaultMaxNotValidBeforeDelta", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Unable to access Notary.DefaultMaxNotValidBeforeDelta.");
            var defaultDelta = (int)defaultDeltaField.GetValue(null)!;
            var maxPrefix = GetNotaryPrefix("Prefix_MaxNotValidBeforeDelta");
            var maxKey = StorageKey.Create(notary.Id, maxPrefix);
            if (!snapshot.Contains(maxKey))
            {
                snapshot.Add(maxKey, new StorageItem((BigInteger)defaultDelta));
                mutated = true;
            }

            var depositPrefix = GetNotaryPrefix("Prefix_Deposit");
            var account = _notaryDepositAccount;
            var depositKey = StorageKey.Create(notary.Id, depositPrefix, account);
            if (!snapshot.Contains(depositKey))
            {
                var deposit = new Notary.Deposit
                {
                    Amount = s_notarySeedDepositAmount,
                    Till = 0
                };
                snapshot.Add(depositKey, StorageItem.CreateSealed(deposit));
                _seededNotaryTill = deposit.Till;
                _hasSeededNotaryTill = true;
                EnsureGasLiquidity(snapshot, NativeContract.Notary.Hash, deposit.Amount);
                mutated = true;
            }
            else
            {
                if (snapshot.TryGet(depositKey, out var item))
                {
                    _seededNotaryTill = item.GetInteroperable<Notary.Deposit>().Till;
                    _hasSeededNotaryTill = true;
                }
            }

            if (mutated)
                snapshot.Commit();
        }

        private static byte GetNotaryPrefix(string name)
        {
            var field = typeof(Notary).GetField(name, BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException($"Unable to access field {name} on Notary.");
            return (byte)field.GetValue(null)!;
        }

        private void EnsureGasLiquidity(StoreCache snapshot, UInt160 account, BigInteger minimumBalance)
        {
            if (minimumBalance <= BigInteger.Zero)
                return;

            var gas = NativeContract.GAS;
            var accountPrefix = GetFungibleTokenPrefix(typeof(GasToken), "Prefix_Account");
            var totalPrefix = GetFungibleTokenPrefix(typeof(GasToken), "Prefix_TotalSupply");
            var accountKey = StorageKey.Create(gas.Id, accountPrefix, account);
            var totalKey = StorageKey.Create(gas.Id, totalPrefix);
            var totalItem = snapshot.GetAndChange(totalKey, () => new StorageItem(BigInteger.Zero));

            if (!snapshot.TryGet(accountKey, out var accountItem))
            {
                AccountState state = new()
                {
                    Balance = minimumBalance
                };
                snapshot.Add(accountKey, StorageItem.CreateSealed(state));
                totalItem.Add(minimumBalance);
                return;
            }

            var accountState = accountItem.GetInteroperable<AccountState>();
            if (accountState.Balance >= minimumBalance)
                return;

            var delta = minimumBalance - accountState.Balance;
            accountState.Balance = minimumBalance;
            totalItem.Add(delta);
        }

        private void SeedNeoTokenDefaults()
        {
            var neo = NativeContract.NEO;
            using var snapshot = System.GetSnapshotCache();
            var mutated = false;

            var registerPriceField = typeof(NeoToken).GetField("_registerPrice", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Unable to access NeoToken._registerPrice.");
            var registerPriceKey = (StorageKey)registerPriceField.GetValue(neo)!;
            _neoRegisterPrice = 10 * NativeContract.GAS.Factor;
            var priceBytes = _neoRegisterPrice.ToByteArray();
            var priceItem = snapshot.Contains(registerPriceKey)
                ? snapshot.GetAndChange(registerPriceKey)
                : null;
            if (priceItem is null)
            {
                snapshot.Add(registerPriceKey, new StorageItem(priceBytes));
                mutated = true;
            }
            else
            {
                priceItem.Value = priceBytes;
                mutated = true;
            }

            var accountPrefixField = typeof(NeoToken).BaseType?
                .GetField("Prefix_Account", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Unable to access FungibleToken.Prefix_Account.");
            var accountPrefix = (byte)accountPrefixField.GetValue(null)!;
            var accountKey = StorageKey.Create(neo.Id, accountPrefix, PrimaryBenchmarkAccount);
            if (!snapshot.Contains(accountKey))
            {
                var state = new NeoToken.NeoAccountState
                {
                    Balance = 100,
                    BalanceHeight = 0,
                    VoteTo = null,
                    LastGasPerVote = BigInteger.Zero
                };
                snapshot.Add(accountKey, StorageItem.CreateSealed(state));
                mutated = true;
            }

            EnsureGasLiquidity(snapshot, NativeContract.NEO.Hash, _neoRegisterPrice * 5);

            if (mutated)
                snapshot.Commit();
        }

        private void SeedRoleManagementDefaults()
        {
            var roleManagement = NativeContract.RoleManagement;
            using var snapshot = System.GetSnapshotCache();
            const uint index = 0;
            var key = StorageKey.Create(roleManagement.Id, (byte)Role.P2PNotary, index);
            if (snapshot.Contains(key))
                return;

            var nodeListType = typeof(RoleManagement).GetNestedType("NodeList", BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Unable to access RoleManagement.NodeList.");
            var nodeList = (IInteroperable)Activator.CreateInstance(nodeListType)!
                ?? throw new InvalidOperationException("Unable to instantiate RoleManagement.NodeList.");
            nodeListType.GetMethod("Add")!.Invoke(nodeList, new object[] { _notaryKeyPair.PublicKey });
            nodeListType.GetMethod("Sort")!.Invoke(nodeList, null);
            snapshot.Add(key, StorageItem.CreateSealed(nodeList));
            snapshot.Commit();
        }

        private void SeedContractManagementDefaults()
        {
            var contractManagement = NativeContract.ContractManagement;
            using var snapshot = System.GetSnapshotCache();
            var mutated = false;

            var minimumPrefix = GetContractManagementPrefix("Prefix_MinimumDeploymentFee");
            var minimumKey = StorageKey.Create(contractManagement.Id, minimumPrefix);
            if (!snapshot.Contains(minimumKey))
            {
                snapshot.Add(minimumKey, new StorageItem(10_00000000));
                mutated = true;
            }

            var nextPrefix = GetContractManagementPrefix("Prefix_NextAvailableId");
            var nextKey = StorageKey.Create(contractManagement.Id, nextPrefix);
            if (!snapshot.Contains(nextKey))
            {
                snapshot.Add(nextKey, new StorageItem(10_000));
                mutated = true;
            }

            var prefixContract = GetContractManagementPrefix("Prefix_Contract");
            var prefixContractHash = GetContractManagementPrefix("Prefix_ContractHash");
            const int ContractBaseId = 50_000;

            foreach (var profile in NativeContractInputProfiles.Default)
            {
                var manifest = NativeContractBenchmarkArtifacts.CreateBenchmarkManifestDefinition(profile);
                var nef = NativeContractBenchmarkArtifacts.CreateBenchmarkNef();
                var hash = Neo.SmartContract.Helper.GetContractHash(UInt160.Zero, nef.CheckSum, manifest.Name);
                var contractKey = StorageKey.Create(contractManagement.Id, prefixContract, hash);
                if (!snapshot.Contains(contractKey))
                {
                    ContractState state = new()
                    {
                        Id = ContractBaseId + (int)profile.Size,
                        Hash = hash,
                        Manifest = manifest,
                        Nef = nef,
                        UpdateCounter = 0
                    };
                    snapshot.Add(contractKey, StorageItem.CreateSealed(state));
                    snapshot.Add(StorageKey.Create(contractManagement.Id, prefixContractHash, state.Id), new StorageItem(hash.ToArray()));
                    mutated = true;
                }

                _contractManagementCallers[profile.Size] = hash;
            }

            if (mutated)
                snapshot.Commit();
        }

        private UInt160 SeedOracleCallbackContract()
        {
            var contractManagement = NativeContract.ContractManagement;
            using var snapshot = System.GetSnapshotCache();

            var script = CreateOracleCallbackScript();
            var nef = new NefFile
            {
                Compiler = "benchmark",
                Source = "benchmark",
                Tokens = Array.Empty<MethodToken>(),
                Script = script
            };
            nef.CheckSum = NefFile.ComputeChecksum(nef);
            var manifest = CreateOracleCallbackManifest();
            var hash = Neo.SmartContract.Helper.GetContractHash(UInt160.Zero, nef.CheckSum, manifest.Name);

            var prefixContract = GetContractManagementPrefix("Prefix_Contract");
            var prefixContractHash = GetContractManagementPrefix("Prefix_ContractHash");
            var contractKey = StorageKey.Create(contractManagement.Id, prefixContract, hash);
            if (!snapshot.Contains(contractKey))
            {
                ContractState state = new()
                {
                    Id = int.MaxValue - 2,
                    Hash = hash,
                    Manifest = manifest,
                    Nef = nef,
                    UpdateCounter = 0
                };
                snapshot.Add(contractKey, StorageItem.CreateSealed(state));
                snapshot.Add(StorageKey.Create(contractManagement.Id, prefixContractHash, state.Id), new StorageItem(hash.ToArray()));
                snapshot.Commit();
            }

            return hash;
        }

        private static byte[] CreateOracleCallbackScript()
        {
            var builder = new ScriptBuilder();
            builder.Emit(OpCode.DROP);
            builder.Emit(OpCode.DROP);
            builder.Emit(OpCode.DROP);
            builder.Emit(OpCode.DROP);
            builder.Emit(OpCode.RET);
            return builder.ToArray();
        }

        private static ContractManifest CreateOracleCallbackManifest()
        {
            return new ContractManifest
            {
                Name = "Benchmark.Callback",
                Groups = Array.Empty<ContractGroup>(),
                SupportedStandards = Array.Empty<string>(),
                Abi = new ContractAbi
                {
                    Events = Array.Empty<ContractEventDescriptor>(),
                    Methods =
                    [
                        new ContractMethodDescriptor
                        {
                            Name = "oracleHandler",
                            Parameters =
                            [
                                new ContractParameterDefinition { Name = "url", Type = ContractParameterType.String },
                                new ContractParameterDefinition { Name = "userData", Type = ContractParameterType.Any },
                                new ContractParameterDefinition { Name = "code", Type = ContractParameterType.Integer },
                                new ContractParameterDefinition { Name = "result", Type = ContractParameterType.ByteArray }
                            ],
                            ReturnType = ContractParameterType.Void,
                            Offset = 0,
                            Safe = true
                        }
                    ]
                },
                Permissions = new[] { ContractPermission.DefaultPermission },
                Trusts = WildcardContainer<ContractPermissionDescriptor>.Create(),
                Extra = new JObject()
            };
        }

        private static byte GetContractManagementPrefix(string name)
        {
            var field = typeof(ContractManagement).GetField(name, BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException($"Unable to access field {name} on ContractManagement.");
            return (byte)field.GetValue(null)!;
        }

        private static byte GetFungibleTokenPrefix(Type tokenType, string fieldName)
        {
            var baseType = tokenType.BaseType
                ?? throw new InvalidOperationException($"Unable to access base type for {tokenType.Name}.");
            var field = baseType.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException($"Unable to access field {fieldName} on {baseType.Name}.");
            return (byte)field.GetValue(null)!;
        }
    }
}
