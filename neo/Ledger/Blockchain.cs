using Akka.Actor;
using Akka.Configuration;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.IO.Actors;
using Neo.IO.Caching;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins;
using Neo.SmartContract;
using Neo.Trie.MPT;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Neo.Ledger
{
    public sealed partial class Blockchain : UntypedActor
    {
        public class ApplicationExecuted { public Transaction Transaction; public ApplicationExecutionResult[] ExecutionResults; }
        public class PersistCompleted { public Block Block; }
        public class Import { public IEnumerable<Block> Blocks; }
        public class ImportCompleted { }
        public class FillMemoryPool { public IEnumerable<Transaction> Transactions; }
        public class FillCompleted { }

        public static readonly uint SecondsPerBlock = ProtocolSettings.Default.SecondsPerBlock;
        public const uint DecrementInterval = 2000000;
        public const int MaxValidators = 1024;
        public static readonly uint[] GenerationAmount = { 8, 7, 6, 5, 4, 3, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
        public static readonly TimeSpan TimePerBlock = TimeSpan.FromSeconds(SecondsPerBlock);
        public static readonly ECPoint[] StandbyValidators = ProtocolSettings.Default.StandbyValidators.OfType<string>().Select(p => ECPoint.DecodePoint(p.HexToBytes(), ECCurve.Secp256r1)).ToArray();
        public static readonly uint FreeGasChangeHeight = ProtocolSettings.Default.FreeGasChangeHeight;

#pragma warning disable CS0612
        public static readonly RegisterTransaction GoverningToken = new RegisterTransaction
        {
            AssetType = AssetType.GoverningToken,
            Name = "[{\"lang\":\"zh-CN\",\"name\":\"小蚁股\"},{\"lang\":\"en\",\"name\":\"AntShare\"}]",
            Amount = Fixed8.FromDecimal(100000000),
            Precision = 0,
            Owner = ECCurve.Secp256r1.Infinity,
            Admin = (new[] { (byte)OpCode.PUSHT }).ToScriptHash(),
            Attributes = new TransactionAttribute[0],
            Inputs = new CoinReference[0],
            Outputs = new TransactionOutput[0],
            Witnesses = new Witness[0]
        };

        public static readonly RegisterTransaction UtilityToken = new RegisterTransaction
        {
            AssetType = AssetType.UtilityToken,
            Name = "[{\"lang\":\"zh-CN\",\"name\":\"小蚁币\"},{\"lang\":\"en\",\"name\":\"AntCoin\"}]",
            Amount = Fixed8.FromDecimal(GenerationAmount.Sum(p => p) * DecrementInterval),
            Precision = 8,
            Owner = ECCurve.Secp256r1.Infinity,
            Admin = (new[] { (byte)OpCode.PUSHF }).ToScriptHash(),
            Attributes = new TransactionAttribute[0],
            Inputs = new CoinReference[0],
            Outputs = new TransactionOutput[0],
            Witnesses = new Witness[0]
        };
#pragma warning restore CS0612

        public static readonly Block GenesisBlock = new Block
        {
            PrevHash = UInt256.Zero,
            Timestamp = (new DateTime(2016, 7, 15, 15, 8, 21, DateTimeKind.Utc)).ToTimestamp(),
            Index = 0,
            ConsensusData = 2083236893, //向比特币致敬
            NextConsensus = GetConsensusAddress(StandbyValidators),
            Witness = new Witness
            {
                InvocationScript = new byte[0],
                VerificationScript = new[] { (byte)OpCode.PUSHT }
            },
            Transactions = new Transaction[]
            {
                new MinerTransaction
                {
                    Nonce = 2083236893,
                    Attributes = new TransactionAttribute[0],
                    Inputs = new CoinReference[0],
                    Outputs = new TransactionOutput[0],
                    Witnesses = new Witness[0]
                },
                GoverningToken,
                UtilityToken,
                new IssueTransaction
                {
                    Attributes = new TransactionAttribute[0],
                    Inputs = new CoinReference[0],
                    Outputs = new[]
                    {
                        new TransactionOutput
                        {
                            AssetId = GoverningToken.Hash,
                            Value = GoverningToken.Amount,
                            ScriptHash = Contract.CreateMultiSigRedeemScript(StandbyValidators.Length / 2 + 1, StandbyValidators).ToScriptHash()
                        }
                    },
                    Witnesses = new[]
                    {
                        new Witness
                        {
                            InvocationScript = new byte[0],
                            VerificationScript = new[] { (byte)OpCode.PUSHT }
                        }
                    }
                }
            }
        };

        private const int MemoryPoolMaxTransactions = 50_000;
        private const int MaxTxToReverifyPerIdle = 10;
        private static readonly object lockObj = new object();
        private readonly NeoSystem system;
        private readonly List<UInt256> header_index = new List<UInt256>();
        private uint stored_header_count = 0;
        private readonly Dictionary<UInt256, Block> block_cache = new Dictionary<UInt256, Block>();
        private readonly Dictionary<uint, LinkedList<Block>> block_cache_unverified = new Dictionary<uint, LinkedList<Block>>();
        internal readonly RelayCache RelayCache = new RelayCache(100);
        private Snapshot currentSnapshot;

        public Store Store { get; }
        public MemoryPool MemPool { get; }
        public uint Height => currentSnapshot.Height;
        public uint HeaderHeight => currentSnapshot.HeaderHeight;
        public UInt256 CurrentBlockHash => currentSnapshot.CurrentBlockHash;
        public UInt256 CurrentHeaderHash => currentSnapshot.CurrentHeaderHash;

        private static Blockchain singleton;
        public static Blockchain Singleton
        {
            get
            {
                while (singleton == null) Thread.Sleep(10);
                return singleton;
            }
        }

        static Blockchain()
        {
            GenesisBlock.RebuildMerkleRoot();
        }

        public Blockchain(NeoSystem system, Store store)
        {
            this.system = system;
            this.MemPool = new MemoryPool(system, MemoryPoolMaxTransactions);
            this.Store = store;
            lock (lockObj)
            {
                if (singleton != null)
                    throw new InvalidOperationException();
                header_index.AddRange(store.GetHeaderHashList().Find().OrderBy(p => (uint)p.Key).SelectMany(p => p.Value.Hashes));
                stored_header_count += (uint)header_index.Count;
                if (stored_header_count == 0)
                {
                    header_index.AddRange(store.GetBlocks().Find().OrderBy(p => p.Value.TrimmedBlock.Index).Select(p => p.Key));
                }
                else
                {
                    HashIndexState hashIndex = store.GetHeaderHashIndex().Get();
                    if (hashIndex.Index >= stored_header_count)
                    {
                        DataCache<UInt256, BlockState> cache = store.GetBlocks();
                        for (UInt256 hash = hashIndex.Hash; hash != header_index[(int)stored_header_count - 1];)
                        {
                            header_index.Insert((int)stored_header_count, hash);
                            hash = cache[hash].TrimmedBlock.PrevHash;
                        }
                    }
                }
                if (header_index.Count == 0)
                    Persist(GenesisBlock);
                else
                    UpdateCurrentSnapshot();
                singleton = this;
            }
        }

        public bool ContainsBlock(UInt256 hash)
        {
            if (block_cache.ContainsKey(hash)) return true;
            return Store.ContainsBlock(hash);
        }

        public bool ContainsTransaction(UInt256 hash)
        {
            if (MemPool.ContainsKey(hash)) return true;
            return Store.ContainsTransaction(hash);
        }

        public Block GetBlock(UInt256 hash)
        {
            if (block_cache.TryGetValue(hash, out Block block))
                return block;
            return Store.GetBlock(hash);
        }

        public UInt256 GetBlockHash(uint index)
        {
            if (header_index.Count <= index) return null;
            return header_index[(int)index];
        }

        public static UInt160 GetConsensusAddress(ECPoint[] validators)
        {
            return Contract.CreateMultiSigRedeemScript(validators.Length - (validators.Length - 1) / 3, validators).ToScriptHash();
        }

        public Snapshot GetSnapshot()
        {
            return Store.GetSnapshot();
        }

        public Transaction GetTransaction(UInt256 hash)
        {
            if (MemPool.TryGetValue(hash, out Transaction transaction))
                return transaction;
            return Store.GetTransaction(hash);
        }

        private void OnImport(IEnumerable<Block> blocks)
        {
            foreach (Block block in blocks)
            {
                if (block.Index <= Height) continue;
                if (block.Index != Height + 1)
                    throw new InvalidOperationException();
                Persist(block);
                SaveHeaderHashList();
            }
            Sender.Tell(new ImportCompleted());
        }

        private void AddUnverifiedBlockToCache(Block block)
        {
            if (!block_cache_unverified.TryGetValue(block.Index, out LinkedList<Block> blocks))
            {
                blocks = new LinkedList<Block>();
                block_cache_unverified.Add(block.Index, blocks);
            }

            blocks.AddLast(block);
        }

        private void OnFillMemoryPool(IEnumerable<Transaction> transactions)
        {
            // Invalidate all the transactions in the memory pool, to avoid any failures when adding new transactions.
            MemPool.InvalidateAllTransactions();

            // Add the transactions to the memory pool
            foreach (var tx in transactions)
            {
                if (tx.Type == TransactionType.MinerTransaction)
                    continue;
                if (Store.ContainsTransaction(tx.Hash))
                    continue;
                if (!Plugin.CheckPolicy(tx))
                    continue;
                // First remove the tx if it is unverified in the pool.
                MemPool.TryRemoveUnVerified(tx.Hash, out _);
                // Verify the the transaction
                if (!tx.Verify(currentSnapshot, MemPool.GetVerifiedTransactions()))
                    continue;
                // Add to the memory pool
                MemPool.TryAdd(tx.Hash, tx);
            }
            // Transactions originally in the pool will automatically be reverified based on their priority.

            Sender.Tell(new FillCompleted());
        }

        private RelayResultReason OnNewBlock(Block block)
        {
            if (block.Index <= Height)
                return RelayResultReason.AlreadyExists;
            if (block_cache.ContainsKey(block.Hash))
                return RelayResultReason.AlreadyExists;
            if (block.Index - 1 >= header_index.Count)
            {
                AddUnverifiedBlockToCache(block);
                return RelayResultReason.UnableToVerify;
            }
            if (block.Index == header_index.Count)
            {
                if (!block.Verify(currentSnapshot))
                    return RelayResultReason.Invalid;
            }
            else
            {
                if (!block.Hash.Equals(header_index[(int)block.Index]))
                    return RelayResultReason.Invalid;
            }
            if (block.Index == Height + 1)
            {
                Block block_persist = block;
                List<Block> blocksToPersistList = new List<Block>();
                while (true)
                {
                    blocksToPersistList.Add(block_persist);
                    if (block_persist.Index + 1 >= header_index.Count) break;
                    UInt256 hash = header_index[(int)block_persist.Index + 1];
                    if (!block_cache.TryGetValue(hash, out block_persist)) break;
                }

                int blocksPersisted = 0;
                foreach (Block blockToPersist in blocksToPersistList)
                {
                    block_cache_unverified.Remove(blockToPersist.Index);
                    Persist(blockToPersist);

                    if (blocksPersisted++ < blocksToPersistList.Count - (2 + Math.Max(0, (15 - SecondsPerBlock)))) continue;
                    // Empirically calibrated for relaying the most recent 2 blocks persisted with 15s network
                    // Increase in the rate of 1 block per second in configurations with faster blocks

                    if (blockToPersist.Index + 100 >= header_index.Count)
                        system.LocalNode.Tell(new LocalNode.RelayDirectly { Inventory = blockToPersist });
                }
                SaveHeaderHashList();

                if (block_cache_unverified.TryGetValue(Height + 1, out LinkedList<Block> unverifiedBlocks))
                {
                    foreach (var unverifiedBlock in unverifiedBlocks)
                        Self.Tell(unverifiedBlock, ActorRefs.NoSender);
                    block_cache_unverified.Remove(Height + 1);
                }
            }
            else
            {
                block_cache.Add(block.Hash, block);
                if (block.Index + 100 >= header_index.Count)
                    system.LocalNode.Tell(new LocalNode.RelayDirectly { Inventory = block });
                if (block.Index == header_index.Count)
                {
                    header_index.Add(block.Hash);
                    using (Snapshot snapshot = GetSnapshot())
                    {
                        snapshot.Blocks.Add(block.Hash, new BlockState
                        {
                            SystemFeeAmount = 0,
                            TrimmedBlock = block.Header.Trim()
                        });
                        snapshot.HeaderHashIndex.GetAndChange().Hash = block.Hash;
                        snapshot.HeaderHashIndex.GetAndChange().Index = block.Index;
                        SaveHeaderHashList(snapshot);
                        snapshot.Commit();
                    }
                    UpdateCurrentSnapshot();
                }
            }
            return RelayResultReason.Succeed;
        }

        private RelayResultReason OnNewConsensus(ConsensusPayload payload)
        {
            if (!payload.Verify(currentSnapshot)) return RelayResultReason.Invalid;
            system.Consensus?.Tell(payload);
            RelayCache.Add(payload);
            system.LocalNode.Tell(new LocalNode.RelayDirectly { Inventory = payload });
            return RelayResultReason.Succeed;
        }

        private void OnNewHeaders(Header[] headers)
        {
            using (Snapshot snapshot = GetSnapshot())
            {
                foreach (Header header in headers)
                {
                    if (header.Index - 1 >= header_index.Count) break;
                    if (header.Index < header_index.Count) continue;
                    if (!header.Verify(snapshot)) break;
                    header_index.Add(header.Hash);
                    snapshot.Blocks.Add(header.Hash, new BlockState
                    {
                        SystemFeeAmount = 0,
                        TrimmedBlock = header.Trim()
                    });
                    snapshot.HeaderHashIndex.GetAndChange().Hash = header.Hash;
                    snapshot.HeaderHashIndex.GetAndChange().Index = header.Index;
                }
                SaveHeaderHashList(snapshot);
                snapshot.Commit();
            }
            UpdateCurrentSnapshot();
            system.TaskManager.Tell(new TaskManager.HeaderTaskCompleted(), Sender);
        }

        private RelayResultReason OnNewTransaction(Transaction transaction)
        {
            try
            {
                if (transaction.Type == TransactionType.MinerTransaction)
                    return RelayResultReason.Invalid;
                if (ContainsTransaction(transaction.Hash))
                    return RelayResultReason.AlreadyExists;
                if (!MemPool.CanTransactionFitInPool(transaction))
                    return RelayResultReason.OutOfMemory;
                if (!transaction.Verify(currentSnapshot, MemPool.GetVerifiedTransactions()))
                    return RelayResultReason.Invalid;
                if (!Plugin.CheckPolicy(transaction))
                    return RelayResultReason.PolicyFail;

                if (!MemPool.TryAdd(transaction.Hash, transaction))
                    return RelayResultReason.OutOfMemory;
            }
            catch
            {
                return RelayResultReason.Invalid;
            }
            system.LocalNode.Tell(new LocalNode.RelayDirectly { Inventory = transaction });
            return RelayResultReason.Succeed;
        }

        private void OnPersistCompleted(Block block)
        {
            block_cache.Remove(block.Hash);
            MemPool.UpdatePoolForBlockPersisted(block, currentSnapshot);
            Context.System.EventStream.Publish(new PersistCompleted { Block = block });
            CheckRootOnBlockPersistCompleted();
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case Import import:
                    OnImport(import.Blocks);
                    break;
                case ImportRoots importRoots:
                    OnImportRoots(importRoots.Roots);
                    break;
                case FillMemoryPool fill:
                    OnFillMemoryPool(fill.Transactions);
                    break;
                case Header[] headers:
                    OnNewHeaders(headers);
                    break;
                case Block block:
                    Sender.Tell(OnNewBlock(block));
                    break;
                case Transaction transaction:
                    Sender.Tell(OnNewTransaction(transaction));
                    break;
                case StateRoot stateRoot:
                    OnNewStateRoot(stateRoot);
                    break;
                case StateRoot[] stateRoots:
                    OnStateRoots(stateRoots);
                    break;
                case ConsensusPayload payload:
                    Sender.Tell(OnNewConsensus(payload));
                    break;
                case Idle _:
                    if (MemPool.ReVerifyTopUnverifiedTransactionsIfNeeded(MaxTxToReverifyPerIdle, currentSnapshot))
                        Self.Tell(Idle.Instance, ActorRefs.NoSender);
                    break;
            }
        }

        private void Persist(Block block)
        {
            using (Snapshot snapshot = GetSnapshot())
            {
                List<ApplicationExecuted> all_application_executed = new List<ApplicationExecuted>();
                snapshot.PersistingBlock = block;
                snapshot.Blocks.Add(block.Hash, new BlockState
                {
                    SystemFeeAmount = snapshot.GetSysFeeAmount(block.PrevHash) + (long)block.Transactions.Sum(p => p.SystemFee),
                    TrimmedBlock = block.Trim()
                });
                foreach (Transaction tx in block.Transactions)
                {
                    snapshot.Transactions.Add(tx.Hash, new TransactionState
                    {
                        BlockIndex = block.Index,
                        Transaction = tx
                    });
                    snapshot.UnspentCoins.Add(tx.Hash, new UnspentCoinState
                    {
                        Items = Enumerable.Repeat(CoinState.Confirmed, tx.Outputs.Length).ToArray()
                    });
                    foreach (TransactionOutput output in tx.Outputs)
                    {
                        AccountState account = snapshot.Accounts.GetAndChange(output.ScriptHash, () => new AccountState(output.ScriptHash));
                        if (account.Balances.ContainsKey(output.AssetId))
                            account.Balances[output.AssetId] += output.Value;
                        else
                            account.Balances[output.AssetId] = output.Value;
                        if (output.AssetId.Equals(GoverningToken.Hash) && account.Votes.Length > 0)
                        {
                            foreach (ECPoint pubkey in account.Votes)
                                snapshot.Validators.GetAndChange(pubkey, () => new ValidatorState(pubkey)).Votes += output.Value;
                            snapshot.ValidatorsCount.GetAndChange().Votes[account.Votes.Length - 1] += output.Value;
                        }
                    }
                    foreach (var group in tx.Inputs.GroupBy(p => p.PrevHash))
                    {
                        TransactionState tx_prev = snapshot.Transactions[group.Key];
                        foreach (CoinReference input in group)
                        {
                            snapshot.UnspentCoins.GetAndChange(input.PrevHash).Items[input.PrevIndex] |= CoinState.Spent;
                            TransactionOutput out_prev = tx_prev.Transaction.Outputs[input.PrevIndex];
                            AccountState account = snapshot.Accounts.GetAndChange(out_prev.ScriptHash);
                            if (out_prev.AssetId.Equals(GoverningToken.Hash))
                            {
                                snapshot.SpentCoins.GetAndChange(input.PrevHash, () => new SpentCoinState
                                {
                                    TransactionHash = input.PrevHash,
                                    TransactionHeight = tx_prev.BlockIndex,
                                    Items = new Dictionary<ushort, uint>()
                                }).Items.Add(input.PrevIndex, block.Index);
                                if (account.Votes.Length > 0)
                                {
                                    foreach (ECPoint pubkey in account.Votes)
                                    {
                                        ValidatorState validator = snapshot.Validators.GetAndChange(pubkey);
                                        validator.Votes -= out_prev.Value;
                                        if (!validator.Registered && validator.Votes.Equals(Fixed8.Zero))
                                            snapshot.Validators.Delete(pubkey);
                                    }
                                    snapshot.ValidatorsCount.GetAndChange().Votes[account.Votes.Length - 1] -= out_prev.Value;
                                }
                            }
                            account.Balances[out_prev.AssetId] -= out_prev.Value;
                        }
                    }
                    List<ApplicationExecutionResult> execution_results = new List<ApplicationExecutionResult>();
                    switch (tx)
                    {
#pragma warning disable CS0612
                        case RegisterTransaction tx_register:
                            snapshot.Assets.Add(tx.Hash, new AssetState
                            {
                                AssetId = tx_register.Hash,
                                AssetType = tx_register.AssetType,
                                Name = tx_register.Name,
                                Amount = tx_register.Amount,
                                Available = Fixed8.Zero,
                                Precision = tx_register.Precision,
                                Fee = Fixed8.Zero,
                                FeeAddress = new UInt160(),
                                Owner = tx_register.Owner,
                                Admin = tx_register.Admin,
                                Issuer = tx_register.Admin,
                                Expiration = block.Index + 2 * 2000000,
                                IsFrozen = false
                            });
                            break;
#pragma warning restore CS0612
                        case IssueTransaction _:
                            foreach (TransactionResult result in tx.GetTransactionResults().Where(p => p.Amount < Fixed8.Zero))
                                snapshot.Assets.GetAndChange(result.AssetId).Available -= result.Amount;
                            break;
                        case ClaimTransaction _:
                            foreach (CoinReference input in ((ClaimTransaction)tx).Claims)
                            {
                                if (snapshot.SpentCoins.TryGet(input.PrevHash)?.Items.Remove(input.PrevIndex) == true)
                                    snapshot.SpentCoins.GetAndChange(input.PrevHash);
                            }
                            break;
#pragma warning disable CS0612
                        case EnrollmentTransaction tx_enrollment:
                            snapshot.Validators.GetAndChange(tx_enrollment.PublicKey, () => new ValidatorState(tx_enrollment.PublicKey)).Registered = true;
                            break;
#pragma warning restore CS0612
                        case StateTransaction tx_state:
                            foreach (StateDescriptor descriptor in tx_state.Descriptors)
                                switch (descriptor.Type)
                                {
                                    case StateType.Account:
                                        ProcessAccountStateDescriptor(descriptor, snapshot);
                                        break;
                                    case StateType.Validator:
                                        ProcessValidatorStateDescriptor(descriptor, snapshot);
                                        break;
                                }
                            break;
#pragma warning disable CS0612
                        case PublishTransaction tx_publish:
                            snapshot.Contracts.GetOrAdd(tx_publish.ScriptHash, () => new ContractState
                            {
                                Script = tx_publish.Script,
                                ParameterList = tx_publish.ParameterList,
                                ReturnType = tx_publish.ReturnType,
                                ContractProperties = (ContractPropertyState)Convert.ToByte(tx_publish.NeedStorage),
                                Name = tx_publish.Name,
                                CodeVersion = tx_publish.CodeVersion,
                                Author = tx_publish.Author,
                                Email = tx_publish.Email,
                                Description = tx_publish.Description
                            });
                            break;
#pragma warning restore CS0612
                        case InvocationTransaction tx_invocation:
                            using (ApplicationEngine engine = new ApplicationEngine(TriggerType.Application, tx_invocation, snapshot.Clone(), tx_invocation.Gas))
                            {
                                engine.LoadScript(tx_invocation.Script);
                                engine.Execute();
                                if (!engine.State.HasFlag(VMState.FAULT))
                                {
                                    engine.Service.Commit();
                                }
                                execution_results.Add(new ApplicationExecutionResult
                                {
                                    Trigger = TriggerType.Application,
                                    ScriptHash = tx_invocation.Script.ToScriptHash(),
                                    VMState = engine.State,
                                    GasConsumed = engine.GasConsumed,
                                    Stack = engine.ResultStack.ToArray(),
                                    Notifications = engine.Service.Notifications.ToArray()
                                });
                            }
                            break;
                    }
                    if (execution_results.Count > 0)
                    {
                        ApplicationExecuted application_executed = new ApplicationExecuted
                        {
                            Transaction = tx,
                            ExecutionResults = execution_results.ToArray()
                        };
                        Context.System.EventStream.Publish(application_executed);
                        all_application_executed.Add(application_executed);
                    }
                }
                snapshot.BlockHashIndex.GetAndChange().Hash = block.Hash;
                snapshot.BlockHashIndex.GetAndChange().Index = block.Index;
                if (block.Index == header_index.Count)
                {
                    header_index.Add(block.Hash);
                    snapshot.HeaderHashIndex.GetAndChange().Hash = block.Hash;
                    snapshot.HeaderHashIndex.GetAndChange().Index = block.Index;
                }
                foreach (IPersistencePlugin plugin in Plugin.PersistencePlugins)
                    plugin.OnPersist(snapshot, all_application_executed);
                snapshot.Commit();
                List<Exception> commitExceptions = null;
                foreach (IPersistencePlugin plugin in Plugin.PersistencePlugins)
                {
                    try
                    {
                        plugin.OnCommit(snapshot);
                    }
                    catch (Exception ex)
                    {
                        if (plugin.ShouldThrowExceptionFromCommit(ex))
                        {
                            if (commitExceptions == null)
                                commitExceptions = new List<Exception>();

                            commitExceptions.Add(ex);
                        }
                    }
                }
                if (commitExceptions != null) throw new AggregateException(commitExceptions);
            }
            PersistLocalStateRoot();
            UpdateCurrentSnapshot();
            OnPersistCompleted(block);
        }

        protected override void PostStop()
        {
            base.PostStop();
            currentSnapshot?.Dispose();
        }

        internal static void ProcessAccountStateDescriptor(StateDescriptor descriptor, Snapshot snapshot)
        {
            UInt160 hash = new UInt160(descriptor.Key);
            AccountState account = snapshot.Accounts.GetAndChange(hash, () => new AccountState(hash));
            switch (descriptor.Field)
            {
                case "Votes":
                    Fixed8 balance = account.GetBalance(GoverningToken.Hash);
                    foreach (ECPoint pubkey in account.Votes)
                    {
                        ValidatorState validator = snapshot.Validators.GetAndChange(pubkey);
                        validator.Votes -= balance;
                        if (!validator.Registered && validator.Votes.Equals(Fixed8.Zero))
                            snapshot.Validators.Delete(pubkey);
                    }
                    ECPoint[] votes = descriptor.Value.AsSerializableArray<ECPoint>().Distinct().ToArray();
                    if (votes.Length != account.Votes.Length)
                    {
                        ValidatorsCountState count_state = snapshot.ValidatorsCount.GetAndChange();
                        if (account.Votes.Length > 0)
                            count_state.Votes[account.Votes.Length - 1] -= balance;
                        if (votes.Length > 0)
                            count_state.Votes[votes.Length - 1] += balance;
                    }
                    account.Votes = votes;
                    foreach (ECPoint pubkey in account.Votes)
                        snapshot.Validators.GetAndChange(pubkey, () => new ValidatorState(pubkey)).Votes += balance;
                    break;
            }
        }

        internal static void ProcessValidatorStateDescriptor(StateDescriptor descriptor, Snapshot snapshot)
        {
            ECPoint pubkey = ECPoint.DecodePoint(descriptor.Key, ECCurve.Secp256r1);
            ValidatorState validator = snapshot.Validators.GetAndChange(pubkey, () => new ValidatorState(pubkey));
            switch (descriptor.Field)
            {
                case "Registered":
                    validator.Registered = BitConverter.ToBoolean(descriptor.Value, 0);
                    break;
            }
        }

        public static Props Props(NeoSystem system, Store store)
        {
            return Akka.Actor.Props.Create(() => new Blockchain(system, store)).WithMailbox("blockchain-mailbox");
        }

        private void SaveHeaderHashList(Snapshot snapshot = null)
        {
            if ((header_index.Count - stored_header_count < 2000))
                return;
            bool snapshot_created = snapshot == null;
            if (snapshot_created) snapshot = GetSnapshot();
            try
            {
                while (header_index.Count - stored_header_count >= 2000)
                {
                    snapshot.HeaderHashList.Add(stored_header_count, new HeaderHashList
                    {
                        Hashes = header_index.Skip((int)stored_header_count).Take(2000).ToArray()
                    });
                    stored_header_count += 2000;
                }
                if (snapshot_created) snapshot.Commit();
            }
            finally
            {
                if (snapshot_created) snapshot.Dispose();
            }
        }

        private void UpdateCurrentSnapshot()
        {
            Interlocked.Exchange(ref currentSnapshot, GetSnapshot())?.Dispose();
        }
    }

    internal class BlockchainMailbox : PriorityMailbox
    {
        public BlockchainMailbox(Akka.Actor.Settings settings, Config config)
            : base(settings, config)
        {
        }

        internal protected override bool IsHighPriority(object message)
        {
            switch (message)
            {
                case Header[] _:
                case Block _:
                case StateRoot _:
                case StateRoot[] _:
                case ConsensusPayload _:
                case Terminated _:
                    return true;
                default:
                    return false;
            }
        }
    }
}
