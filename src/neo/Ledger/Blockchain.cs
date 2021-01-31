using Akka.Actor;
using Akka.Configuration;
using Akka.IO;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.IO.Actors;
using Neo.IO.Caching;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Neo.Ledger
{
    public sealed partial class Blockchain : UntypedActor
    {
        public partial class ApplicationExecuted { }
        public class PersistCompleted { public Block Block; }
        public class Import { public IEnumerable<Block> Blocks; public bool Verify = true; }
        public class ImportCompleted { }
        public class FillMemoryPool { public IEnumerable<Transaction> Transactions; }
        public class FillCompleted { }
        internal class PreverifyCompleted { public Transaction Transaction; public VerifyResult Result; }
        public class RelayResult { public IInventory Inventory; public VerifyResult Result; }
        private class UnverifiedBlocksList { public LinkedList<Block> Blocks = new LinkedList<Block>(); public HashSet<IActorRef> Nodes = new HashSet<IActorRef>(); }

        public static readonly uint MillisecondsPerBlock = ProtocolSettings.Default.MillisecondsPerBlock;
        public static readonly TimeSpan TimePerBlock = TimeSpan.FromMilliseconds(MillisecondsPerBlock);
        public static readonly ECPoint[] StandbyCommittee = ProtocolSettings.Default.StandbyCommittee.Select(p => ECPoint.DecodePoint(p.HexToBytes(), ECCurve.Secp256r1)).ToArray();
        public static readonly ECPoint[] StandbyValidators = StandbyCommittee[0..ProtocolSettings.Default.ValidatorsCount];

        public static readonly Block GenesisBlock = new Block
        {
            PrevHash = UInt256.Zero,
            Timestamp = (new DateTime(2016, 7, 15, 15, 8, 21, DateTimeKind.Utc)).ToTimestampMS(),
            Index = 0,
            NextConsensus = Contract.GetBFTAddress(StandbyValidators),
            Witness = new Witness
            {
                InvocationScript = Array.Empty<byte>(),
                VerificationScript = new[] { (byte)OpCode.PUSH1 }
            },
            ConsensusData = new ConsensusData
            {
                PrimaryIndex = 0,
                Nonce = 2083236893
            },
            Transactions = Array.Empty<Transaction>()
        };

        private readonly static Script onPersistScript, postPersistScript;
        private const int MaxTxToReverifyPerIdle = 10;
        private static readonly object lockObj = new object();
        private readonly NeoSystem system;
        private readonly IActorRef txrouter;
        private readonly ConcurrentDictionary<UInt256, Block> block_cache = new ConcurrentDictionary<UInt256, Block>();
        private readonly Dictionary<uint, UnverifiedBlocksList> block_cache_unverified = new Dictionary<uint, UnverifiedBlocksList>();
        internal readonly RelayCache RelayCache = new RelayCache(100);
        private ImmutableHashSet<UInt160> extensibleWitnessWhiteList;

        public IStore Store { get; }
        /// <summary>
        /// A readonly view of the blockchain store.
        /// Note: It doesn't need to be disposed because the <see cref="ISnapshot"/> inside it is null.
        /// </summary>
        public DataCache View => new SnapshotCache(Store);
        public MemoryPool MemPool { get; }
        public HeaderCache HeaderCache { get; } = new HeaderCache();

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

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitSysCall(ApplicationEngine.System_Contract_NativeOnPersist);
                onPersistScript = sb.ToArray();
            }
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitSysCall(ApplicationEngine.System_Contract_NativePostPersist);
                postPersistScript = sb.ToArray();
            }
        }

        public Blockchain(NeoSystem system, IStore store)
        {
            this.system = system;
            this.txrouter = Context.ActorOf(TransactionRouter.Props(system));
            this.MemPool = new MemoryPool(system, ProtocolSettings.Default.MemoryPoolMaxTransactions);
            this.Store = store;
            lock (lockObj)
            {
                if (singleton != null)
                    throw new InvalidOperationException();
                DataCache snapshot = View;
                if (!NativeContract.Ledger.Initialized(snapshot))
                {
                    Persist(GenesisBlock);
                }
                else
                {
                    UpdateExtensibleWitnessWhiteList(snapshot);
                }
                singleton = this;
            }
        }

        protected override void PostStop()
        {
            base.PostStop();
            HeaderCache.Dispose();
        }

        private bool ContainsTransaction(UInt256 hash)
        {
            if (MemPool.ContainsKey(hash)) return true;
            return NativeContract.Ledger.ContainsTransaction(View, hash);
        }

        public SnapshotCache GetSnapshot()
        {
            return new SnapshotCache(Store.GetSnapshot());
        }

        private void OnImport(IEnumerable<Block> blocks, bool verify)
        {
            uint currentHeight = NativeContract.Ledger.CurrentIndex(View);
            foreach (Block block in blocks)
            {
                if (block.Index <= currentHeight) continue;
                if (block.Index != currentHeight + 1)
                    throw new InvalidOperationException();
                if (verify && !block.Verify(View))
                    throw new InvalidOperationException();
                Persist(block);
                ++currentHeight;
            }
            Sender.Tell(new ImportCompleted());
        }

        private void AddUnverifiedBlockToCache(Block block)
        {
            // Check if any block proposal for height `block.Index` exists
            if (!block_cache_unverified.TryGetValue(block.Index, out var list))
            {
                // There are no blocks, a new UnverifiedBlocksList is created and, consequently, the current block is added to the list
                list = new UnverifiedBlocksList();
                block_cache_unverified.Add(block.Index, list);
            }
            else
            {
                // Check if any block with the hash being added already exists on possible candidates to be processed
                foreach (var unverifiedBlock in list.Blocks)
                {
                    if (block.Hash == unverifiedBlock.Hash)
                        return;
                }

                if (!list.Nodes.Add(Sender))
                {
                    // Same index with different hash
                    Sender.Tell(Tcp.Abort.Instance);
                    return;
                }
            }

            list.Blocks.AddLast(block);
        }

        private void OnFillMemoryPool(IEnumerable<Transaction> transactions)
        {
            // Invalidate all the transactions in the memory pool, to avoid any failures when adding new transactions.
            MemPool.InvalidateAllTransactions();

            DataCache snapshot = View;

            // Add the transactions to the memory pool
            foreach (var tx in transactions)
            {
                if (NativeContract.Ledger.ContainsTransaction(snapshot, tx.Hash))
                    continue;
                // First remove the tx if it is unverified in the pool.
                MemPool.TryRemoveUnVerified(tx.Hash, out _);
                // Add to the memory pool
                MemPool.TryAdd(tx, snapshot);
            }
            // Transactions originally in the pool will automatically be reverified based on their priority.

            Sender.Tell(new FillCompleted());
        }

        private void OnInventory(IInventory inventory, bool relay = true)
        {
            VerifyResult result = inventory switch
            {
                Block block => OnNewBlock(block),
                Transaction transaction => OnNewTransaction(transaction),
                _ => OnNewInventory(inventory)
            };
            if (result == VerifyResult.Succeed && relay)
            {
                system.LocalNode.Tell(new LocalNode.RelayDirectly { Inventory = inventory });
            }
            SendRelayResult(inventory, result);
        }

        private VerifyResult OnNewBlock(Block block)
        {
            DataCache snapshot = View;
            uint currentHeight = NativeContract.Ledger.CurrentIndex(snapshot);
            uint headerHeight = HeaderCache.Last?.Index ?? NativeContract.Ledger.CurrentIndex(snapshot);
            if (block.Index <= currentHeight)
                return VerifyResult.AlreadyExists;
            if (block.Index - 1 > headerHeight)
            {
                AddUnverifiedBlockToCache(block);
                return VerifyResult.UnableToVerify;
            }
            if (block.Index == headerHeight + 1)
            {
                if (!block.Verify(snapshot))
                    return VerifyResult.Invalid;
            }
            else
            {
                if (!block.Hash.Equals(HeaderCache[block.Index].Hash))
                    return VerifyResult.Invalid;
            }
            block_cache.TryAdd(block.Hash, block);
            if (block.Index == currentHeight + 1)
            {
                Block block_persist = block;
                List<Block> blocksToPersistList = new List<Block>();
                while (true)
                {
                    blocksToPersistList.Add(block_persist);
                    if (block_persist.Index + 1 > headerHeight) break;
                    UInt256 hash = HeaderCache[block_persist.Index + 1].Hash;
                    if (!block_cache.TryGetValue(hash, out block_persist)) break;
                }

                int blocksPersisted = 0;
                // 15000 is the default among of seconds per block, while MilliSecondsPerBlock is the current
                uint extraBlocks = (ProtocolSettings.Default.MillisecondsPerBlock - MillisecondsPerBlock) / 1000;
                foreach (Block blockToPersist in blocksToPersistList)
                {
                    block_cache_unverified.Remove(blockToPersist.Index);
                    Persist(blockToPersist);

                    if (blocksPersisted++ < blocksToPersistList.Count - (2 + Math.Max(0, extraBlocks))) continue;
                    // Empirically calibrated for relaying the most recent 2 blocks persisted with 15s network
                    // Increase in the rate of 1 block per second in configurations with faster blocks

                    if (blockToPersist.Index + 99 >= headerHeight)
                        system.LocalNode.Tell(new LocalNode.RelayDirectly { Inventory = blockToPersist });
                }
                if (block_cache_unverified.TryGetValue(currentHeight + 1, out var unverifiedBlocks))
                {
                    foreach (var unverifiedBlock in unverifiedBlocks.Blocks)
                        Self.Tell(unverifiedBlock, ActorRefs.NoSender);
                    block_cache_unverified.Remove(block.Index + 1);
                }
            }
            else
            {
                if (block.Index + 99 >= headerHeight)
                    system.LocalNode.Tell(new LocalNode.RelayDirectly { Inventory = block });
                if (block.Index == headerHeight + 1)
                    HeaderCache.Add(block.Header);
            }
            return VerifyResult.Succeed;
        }

        private void OnNewHeaders(Header[] headers)
        {
            if (HeaderCache.Full) return;
            DataCache snapshot = View;
            uint headerHeight = HeaderCache.Last?.Index ?? NativeContract.Ledger.CurrentIndex(snapshot);
            foreach (Header header in headers)
            {
                if (header.Index > headerHeight + 1) break;
                if (header.Index < headerHeight + 1) continue;
                if (!header.Verify(snapshot)) break;
                HeaderCache.Add(header);
                ++headerHeight;
            }
        }

        private VerifyResult OnNewInventory(IInventory inventory)
        {
            if (!inventory.Verify(View)) return VerifyResult.Invalid;
            RelayCache.Add(inventory);
            return VerifyResult.Succeed;
        }

        private VerifyResult OnNewTransaction(Transaction transaction)
        {
            if (ContainsTransaction(transaction.Hash)) return VerifyResult.AlreadyExists;
            return MemPool.TryAdd(transaction, View);
        }

        private void OnPreverifyCompleted(PreverifyCompleted task)
        {
            if (task.Result == VerifyResult.Succeed)
                OnInventory(task.Transaction, true);
            else
                SendRelayResult(task.Transaction, task.Result);
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case Import import:
                    OnImport(import.Blocks, import.Verify);
                    break;
                case FillMemoryPool fill:
                    OnFillMemoryPool(fill.Transactions);
                    break;
                case Header[] headers:
                    OnNewHeaders(headers);
                    break;
                case Block block:
                    OnInventory(block, false);
                    break;
                case Transaction tx:
                    OnTransaction(tx);
                    break;
                case IInventory inventory:
                    OnInventory(inventory);
                    break;
                case PreverifyCompleted task:
                    OnPreverifyCompleted(task);
                    break;
                case Idle _:
                    if (MemPool.ReVerifyTopUnverifiedTransactionsIfNeeded(MaxTxToReverifyPerIdle, View))
                        Self.Tell(Idle.Instance, ActorRefs.NoSender);
                    break;
            }
        }

        private void OnTransaction(Transaction tx)
        {
            if (ContainsTransaction(tx.Hash))
                SendRelayResult(tx, VerifyResult.AlreadyExists);
            else
                txrouter.Tell(tx, Sender);
        }

        private void Persist(Block block)
        {
            using (SnapshotCache snapshot = GetSnapshot())
            {
                HeaderCache.TryRemoveFirst();
                List<ApplicationExecuted> all_application_executed = new List<ApplicationExecuted>();
                using (ApplicationEngine engine = ApplicationEngine.Create(TriggerType.OnPersist, null, snapshot, block))
                {
                    engine.LoadScript(onPersistScript);
                    if (engine.Execute() != VMState.HALT) throw new InvalidOperationException();
                    ApplicationExecuted application_executed = new ApplicationExecuted(engine);
                    Context.System.EventStream.Publish(application_executed);
                    all_application_executed.Add(application_executed);
                }
                DataCache clonedSnapshot = snapshot.CreateSnapshot();
                // Warning: Do not write into variable snapshot directly. Write into variable clonedSnapshot and commit instead.
                foreach (Transaction tx in block.Transactions)
                {
                    using (ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Application, tx, clonedSnapshot, block, tx.SystemFee))
                    {
                        engine.LoadScript(tx.Script);
                        if (engine.Execute() == VMState.HALT)
                        {
                            clonedSnapshot.Commit();
                        }
                        else
                        {
                            clonedSnapshot = snapshot.CreateSnapshot();
                        }
                        ApplicationExecuted application_executed = new ApplicationExecuted(engine);
                        Context.System.EventStream.Publish(application_executed);
                        all_application_executed.Add(application_executed);
                    }
                }
                using (ApplicationEngine engine = ApplicationEngine.Create(TriggerType.PostPersist, null, snapshot, block))
                {
                    engine.LoadScript(postPersistScript);
                    if (engine.Execute() != VMState.HALT) throw new InvalidOperationException();
                    ApplicationExecuted application_executed = new ApplicationExecuted(engine);
                    Context.System.EventStream.Publish(application_executed);
                    all_application_executed.Add(application_executed);
                }
                foreach (IPersistencePlugin plugin in Plugin.PersistencePlugins)
                    plugin.OnPersist(block, snapshot, all_application_executed);
                snapshot.Commit();
                List<Exception> commitExceptions = null;
                foreach (IPersistencePlugin plugin in Plugin.PersistencePlugins)
                {
                    try
                    {
                        plugin.OnCommit(block, snapshot);
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
                UpdateExtensibleWitnessWhiteList(snapshot);
                MemPool.UpdatePoolForBlockPersisted(block, snapshot);
            }
            block_cache.TryRemove(block.PrevHash, out _);
            Context.System.EventStream.Publish(new PersistCompleted { Block = block });
        }

        public static Props Props(NeoSystem system, IStore store)
        {
            return Akka.Actor.Props.Create(() => new Blockchain(system, store)).WithMailbox("blockchain-mailbox");
        }

        private void SendRelayResult(IInventory inventory, VerifyResult result)
        {
            RelayResult rr = new RelayResult
            {
                Inventory = inventory,
                Result = result
            };
            Sender.Tell(rr);
            Context.System.EventStream.Publish(rr);
        }

        private void UpdateExtensibleWitnessWhiteList(DataCache snapshot)
        {
            uint currentHeight = NativeContract.Ledger.CurrentIndex(snapshot);
            var builder = ImmutableHashSet.CreateBuilder<UInt160>();
            builder.Add(NativeContract.NEO.GetCommitteeAddress(snapshot));
            var validators = NativeContract.NEO.GetNextBlockValidators(snapshot);
            builder.Add(Contract.GetBFTAddress(validators));
            builder.UnionWith(validators.Select(u => Contract.CreateSignatureRedeemScript(u).ToScriptHash()));
            var oracles = NativeContract.RoleManagement.GetDesignatedByRole(snapshot, Role.Oracle, currentHeight);
            if (oracles.Length > 0)
            {
                builder.Add(Contract.GetBFTAddress(oracles));
                builder.UnionWith(oracles.Select(u => Contract.CreateSignatureRedeemScript(u).ToScriptHash()));
            }
            var stateValidators = NativeContract.RoleManagement.GetDesignatedByRole(snapshot, Role.StateValidator, currentHeight);
            if (stateValidators.Length > 0)
            {
                builder.Add(Contract.GetBFTAddress(stateValidators));
                builder.UnionWith(stateValidators.Select(u => Contract.CreateSignatureRedeemScript(u).ToScriptHash()));
            }
            extensibleWitnessWhiteList = builder.ToImmutable();
        }

        internal bool IsExtensibleWitnessWhiteListed(UInt160 address)
        {
            return extensibleWitnessWhiteList.Contains(address);
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
                case ExtensiblePayload _:
                case Terminated _:
                    return true;
                default:
                    return false;
            }
        }
    }
}
