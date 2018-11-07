using Akka.Actor;
using Akka.Configuration;
using Neo.Cryptography;
using Neo.IO;
using Neo.IO.Actors;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins;
using Neo.SmartContract;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Neo.Consensus
{
    public sealed class ConsensusService : UntypedActor
    {
        public class Start { }
        public class SetViewNumber { public byte ViewNumber; }
        internal class Timer { public uint Height; public byte ViewNumber; }

        private readonly ConsensusContext context = new ConsensusContext();
        private readonly NeoSystem system;
        private readonly Wallet wallet;
        private DateTime block_received_time;

        public ConsensusService(NeoSystem system, Wallet wallet)
        {
            this.system = system;
            this.wallet = wallet;
        }

        /// <summary>
        /// Serialize PreparePayload Data into the desired PrepareRequest message
        /// </summary>
        private PrepareRequest GetPrepareRequestMessage(ConsensusPayload PreparePayloadToGet)
        {
            ConsensusMessage message;
            try
            {
                message = ConsensusMessage.DeserializeFrom(PreparePayloadToGet.Data);
            }
            catch
            {
                return new PrepareRequest();
            }
            return (PrepareRequest)message;
        }

        private bool AddTransaction(Transaction tx, bool verify)
        {
            if (context.Snapshot.ContainsTransaction(tx.Hash) ||
                (verify && !tx.Verify(context.Snapshot, context.Transactions.Values)) ||
                !Plugin.CheckPolicy(tx))
            {
                Log($"reject tx: {tx.Hash}{Environment.NewLine}{tx.ToArray().ToHexString()}", LogLevel.Warning);
                RequestChangeView();
                return false;
            }
            context.Transactions[tx.Hash] = tx;
            if (context.TransactionHashes.Length == context.Transactions.Count)
            {
                if (Blockchain.GetConsensusAddress(context.Snapshot.GetValidators(context.Transactions.Values).ToArray()).Equals(context.NextConsensus))
                {
                    Log($"send prepare response");
                    context.SignedPayloads[context.MyIndex] = context.PreparePayload.Sign(context.KeyPair);
                    context.State |= ConsensusState.SignatureSent;
                    SignAndRelay(context.MakePrepareResponse(context.SignedPayloads[context.MyIndex]));
                    CheckPayloadSignatures();
                }
                else
                {
                    RequestChangeView();
                    return false;
                }
            }
            return true;
        }

        private void ChangeTimer(TimeSpan delay)
        {
            Context.System.Scheduler.ScheduleTellOnce(delay, Self, new Timer
            {
                Height = context.BlockIndex,
                ViewNumber = context.ViewNumber
            }, ActorRefs.NoSender);
        }

        private void CheckExpectedView(byte view_number)
        {
            Log($"CheckExpectedView: view_number={view_number} context.ViewNumber={context.ViewNumber} nv={context.ExpectedView[context.MyIndex]} state={context.State}");

            if (context.ViewNumber == view_number) return;

            if (context.ExpectedView.Count(p => p == view_number) >= context.M)
            {
                InitializeConsensus(view_number);
            }
        }

        private void FillContext()
        {
            IEnumerable<Transaction> mem_pool = Blockchain.Singleton.GetMemoryPool();
            foreach (IPolicyPlugin plugin in Plugin.Policies)
                mem_pool = plugin.FilterForBlock(mem_pool);
            List<Transaction> transactions = mem_pool.ToList();
            Fixed8 amount_netfee = Block.CalculateNetFee(transactions);
            TransactionOutput[] outputs = amount_netfee == Fixed8.Zero ? new TransactionOutput[0] : new[] { new TransactionOutput
            {
                AssetId = Blockchain.UtilityToken.Hash,
                Value = amount_netfee,
                ScriptHash = wallet.GetChangeAddress()
            } };
            while (true)
            {
                ulong nonce = GetNonce();
                MinerTransaction tx = new MinerTransaction
                {
                    Nonce = (uint)(nonce % (uint.MaxValue + 1ul)),
                    Attributes = new TransactionAttribute[0],
                    Inputs = new CoinReference[0],
                    Outputs = outputs,
                    Witnesses = new Witness[0]
                };
                if (!context.Snapshot.ContainsTransaction(tx.Hash))
                {
                    context.Nonce = nonce;
                    transactions.Insert(0, tx);
                    break;
                }
            }
            context.TransactionHashes = transactions.Select(p => p.Hash).ToArray();
            context.Transactions = transactions.ToDictionary(p => p.Hash);
            context.NextConsensus = Blockchain.GetConsensusAddress(context.Snapshot.GetValidators(transactions).ToArray());
        }

        private static ulong GetNonce()
        {
            byte[] nonce = new byte[sizeof(ulong)];
            Random rand = new Random();
            rand.NextBytes(nonce);
            return nonce.ToUInt64(0);
        }

        private void OnStart()
        {
            Log("OnStart");
            InitializeConsensus(0);
        }

        private void InitializeConsensus(byte view_number)
        {
            if (view_number == 0)
                context.Reset(wallet);
            else
                context.ChangeView(view_number);
            if (context.MyIndex < 0) return;
            if (view_number > 0)
                Log($"changeview: view={view_number} primary={context.Validators[context.GetPrimaryIndex((byte)(view_number - 1u))]}", LogLevel.Warning);
            Log($"initialize: height={context.BlockIndex} view={view_number} index={context.MyIndex} role={(context.MyIndex == context.PrimaryIndex ? ConsensusState.Primary : ConsensusState.Backup)}");
            if (context.MyIndex == context.PrimaryIndex)
            {
                context.State |= ConsensusState.Primary;
                TimeSpan span = DateTime.UtcNow - block_received_time;
                if (span >= Blockchain.TimePerBlock)
                    ChangeTimer(TimeSpan.Zero);
                else
                    ChangeTimer(Blockchain.TimePerBlock - span);
            }
            else
            {
                context.State = ConsensusState.Backup;
                ChangeTimer(TimeSpan.FromSeconds(Blockchain.SecondsPerBlock << (view_number + 1)));
            }
        }

        private void Log(string message, LogLevel level = LogLevel.Info)
        {
            Plugin.Log(nameof(ConsensusService), level, message);
        }

        private bool CheckRenegeration()
        {
            if (context.State.HasFlag(ConsensusState.CommitSent))
            {
                Log($"Sending Regeneration payload...");
                SignAndRelay(context.MakeRenegeration());
                Log($"Regeneration sent: height={context.BlockIndex} state={context.State}");
                return true;
            }

            return false;
        }

        private void OnChangeViewReceived(ConsensusPayload payload, ChangeView message)
        {
            Log($"{nameof(OnChangeViewReceived)}: height={payload.BlockIndex} view={message.ViewNumber} index={payload.ValidatorIndex} nv={message.NewViewNumber}");

            if(CheckRenegeration()) return;

            if (message.NewViewNumber <= context.ExpectedView[payload.ValidatorIndex])
                return;
            context.ExpectedView[payload.ValidatorIndex] = message.NewViewNumber;
            CheckExpectedView(message.NewViewNumber);
        }

        private void OnConsensusPayload(ConsensusPayload payload)
        {
            if (context.State.HasFlag(ConsensusState.BlockSent)) return;
            if (payload.ValidatorIndex == context.MyIndex) return;
            if (payload.Version != ConsensusContext.Version)
                return;

            if (payload.PrevHash != context.PrevHash || payload.BlockIndex != context.BlockIndex)
            {
                if (context.Snapshot.Height + 1 < payload.BlockIndex)
                {
                    Log($"chain sync: expected={payload.BlockIndex} current: {context.Snapshot.Height} nodes={LocalNode.Singleton.ConnectedCount}", LogLevel.Warning);
                }
                return;
            }

            if (payload.ValidatorIndex >= context.Validators.Length) return;

            ConsensusMessage message;
            try
            {
                message = ConsensusMessage.DeserializeFrom(payload.Data);
            }
            catch
            {
                return;
            }

            if (message.ViewNumber != context.ViewNumber && (message.Type != ConsensusMessageType.ChangeView || message.Type != ConsensusMessageType.Renegeration))
                return;

            switch (message.Type)
            {
                case ConsensusMessageType.ChangeView:
                    OnChangeViewReceived(payload, (ChangeView)message);
                    break;
                case ConsensusMessageType.PrepareRequest:
                    OnPrepareRequestReceived(payload, (PrepareRequest)message);
                    break;
                case ConsensusMessageType.PrepareResponse:
                    OnPrepareResponseReceived(payload, (PrepareResponse)message);
                    break;
                case ConsensusMessageType.CommitAgreement:
                    OnCommitAgreement(payload, (CommitAgreement)message);
                    break;
                case ConsensusMessageType.Renegeration:
                    OnRenegeration(payload, (Renegeration)message);
                    break;
            }
        }

        private void OnPersistCompleted(Block block)
        {
            Log($"persist block: {block.Hash}");
            block_received_time = DateTime.UtcNow;
            InitializeConsensus(0);
        }

        private void OnPrepareRequestReceived(ConsensusPayload payload, PrepareRequest message)
        {
            if (context.State.HasFlag(ConsensusState.RequestReceived)) return;
            if (payload.ValidatorIndex != context.PrimaryIndex) return;
            Log($"{nameof(OnPrepareRequestReceived)}: height={payload.BlockIndex} view={message.ViewNumber} index={payload.ValidatorIndex} tx={message.TransactionHashes.Length}");
            if (!context.State.HasFlag(ConsensusState.Backup)) return;

            if (payload.Timestamp <= context.Snapshot.GetHeader(context.PrevHash).Timestamp || payload.Timestamp > DateTime.UtcNow.AddMinutes(10).ToTimestamp())
            {
                Log($"Timestamp incorrect: {payload.Timestamp}", LogLevel.Warning);
                return;
            }

            context.State |= ConsensusState.RequestReceived;
            context.Timestamp = payload.Timestamp;
            context.Nonce = message.Nonce;
            context.NextConsensus = message.NextConsensus;
            context.TransactionHashes = message.TransactionHashes;
            context.Transactions = new Dictionary<UInt256, Transaction>();
            context.PreparePayload = payload;
            context.SignedPayloads[payload.ValidatorIndex] = message.PrepReqSignature;


            if (!CheckPrimaryPayloadSignature(payload))
            {
                context.SignedPayloads[payload.ValidatorIndex] = null;
                return;
            }

            for (int i = 0; i < context.SignedPayloads.Length; i++)
                if (context.SignedPayloads[i] != null && i != payload.ValidatorIndex)
                    if (!Crypto.Default.VerifySignature(context.PreparePayload.GetHashData(), context.SignedPayloads[i], context.Validators[i].EncodePoint(false)))
                    {
                        Log($"Index {i} paylod:{payload.ValidatorIndex} lenght:{context.SignedPayloads.Length} is being set to null");
                        context.SignedPayloads[i] = null;
                    }

  
            Dictionary<UInt256, Transaction> mempool = Blockchain.Singleton.GetMemoryPool().ToDictionary(p => p.Hash);
            List<Transaction> unverified = new List<Transaction>();
            foreach (UInt256 hash in context.TransactionHashes.Skip(1))
            {
                if (mempool.TryGetValue(hash, out Transaction tx))
                {
                    if (!AddTransaction(tx, false))
                        return;
                }
                else
                {
                    tx = Blockchain.Singleton.GetUnverifiedTransaction(hash);
                    if (tx != null)
                        unverified.Add(tx);
                }
            }
            foreach (Transaction tx in unverified)
                if (!AddTransaction(tx, true))
                    return;
            if (!AddTransaction(message.MinerTransaction, true)) return;
            if (context.Transactions.Count < context.TransactionHashes.Length)
            {
                UInt256[] hashes = context.TransactionHashes.Where(i => !context.Transactions.ContainsKey(i)).ToArray();
                system.TaskManager.Tell(new TaskManager.RestartTasks
                {
                    Payload = InvPayload.Create(InventoryType.TX, hashes)
                });
            }
        }

        public void PrintByteArray(byte[] bytes)
        {
            var sb = new StringBuilder("new byte[] { ");
            foreach (var b in bytes)
            {
                sb.Append(b + ", ");
            }
            sb.Append("}");
            Log($"{sb.ToString()}");
        }

        private bool CheckPrimaryPayloadSignature(ConsensusPayload payload)
        {
            // TODO Maybe include some verification here
            PrepareRequest message = GetPrepareRequestMessage(payload);

            /// <summary>
            /// The Speaker Signed the Payload without any signature (this was the trick/magic part), PrepReqSignature was empty
            /// But the payload was latter modified with his signature, 
            /// We mean, the PrepareRequest message was filled with the PrepReqSignature of the Empty Payload and then serialized again into this Payload.
            /// Thus, we need to remove the signature from the Payload to correctly verify Speaker identity agreements with this block
            /// </summary>
            byte[] tempSignature = message.PrepReqSignature;
            message.PrepReqSignature = new byte[64];

            payload.Data = message.ToArray();
            if (!Crypto.Default.VerifySignature(payload.GetHashData(), tempSignature, context.Validators[payload.ValidatorIndex].EncodePoint(false)))
                return false;

            /// <summary>
            /// These next 2 lines could be removed, because payload is not anymore used
            /// it was already saved before changed in the context.PreparePayload... However, let keep things clean for now
            /// </summary>
            message.PrepReqSignature = tempSignature;
            payload.Data = message.ToArray();
            return true;
        }


        private void OnPrepareResponseReceived(ConsensusPayload payload, PrepareResponse message)
        {
            if (context.State.HasFlag(ConsensusState.CommitSent) && context.State.HasFlag(ConsensusState.SignatureSent)) return;
            /// <summary>
            /// This payload.ValidatorIndex already submitted a not null signature
            /// </summary>
            if (context.SignedPayloads[payload.ValidatorIndex] != null) return;

            Log($"{nameof(OnPrepareResponseReceived)}: height={payload.BlockIndex} view={message.ViewNumber} index={payload.ValidatorIndex}");

            /// <summary>
            /// ***** The following is like an additional feature ******
            /// ORIGINAL CODE: In the original code we were storing the signature and later verifying.
            /// FEATURED ONE: Here we can check the PreparePayload if it did not arrive, because all PrepareREsponse Payloads carries that
            /// </summary>
            if (context.PreparePayload == null)
            {
                // We need to check if the Node send the orrect PreparePayload from the expected PrimaryIndex
                if (message.PreparePayload.ValidatorIndex != context.PrimaryIndex) return;
                if (!Crypto.Default.VerifySignature(message.PreparePayload.GetHashData(), GetPrepareRequestMessage(message.PreparePayload).PrepReqSignature, context.Validators[message.PreparePayload.ValidatorIndex].EncodePoint(false))) return;
                Log($"{nameof(OnPrepareRequestReceived)}: indirectly from index={payload.ValidatorIndex}");
                OnPrepareRequestReceived(message.PreparePayload, GetPrepareRequestMessage(message.PreparePayload));
            }

            /// <summary>
            /// Time to check received Signature against our local context.PreparePayload
            /// </summary>
            if (!Crypto.Default.VerifySignature(context.PreparePayload.GetHashData(), message.ResponseSignature, context.Validators[payload.ValidatorIndex].EncodePoint(false))) return;
            context.SignedPayloads[payload.ValidatorIndex] = message.ResponseSignature;
            CheckPayloadSignatures();
        }

        private void CheckPayloadSignatures()
        {
            Log($"CheckPayloadSignatures....SignedPayloads:{context.SignedPayloads.Count(p => p != null)}");

            if (!context.State.HasFlag(ConsensusState.CommitSent) &&
                context.SignedPayloads.Count(p => p != null) >= context.M &&
                context.TransactionHashes.All(p => context.Transactions.ContainsKey(p)))
            {
                Block block = context.MakeHeader();
                if (block == null) return;
                context.State |= ConsensusState.CommitSent;
                context.FinalSignatures[context.MyIndex] = block.Sign(context.KeyPair);
                SignAndRelay(context.MakeCommitAgreement(context.FinalSignatures[context.MyIndex]));
                Log($"Commit sent: height={context.BlockIndex} hash={block.Hash} state={context.State}");
            }
        }


        private void OnCommitAgreement(ConsensusPayload payload, CommitAgreement message)
        {
            if (context.FinalSignatures[payload.ValidatorIndex] != null) return;

            Log($"{nameof(OnCommitAgreement)}: height={payload.BlockIndex} hash={context.MakeHeader().Hash.ToString()} view={message.ViewNumber} index={payload.ValidatorIndex}");

            if (!Crypto.Default.VerifySignature(context.MakeHeader().GetHashData(), message.FinalSignature, context.Validators[payload.ValidatorIndex].EncodePoint(false)))
            {
                Log($"{nameof(OnCommitAgreement)}: SIGNATURE verification with problem");
                return;
            }

            context.FinalSignatures[payload.ValidatorIndex] = message.FinalSignature;

            if (context.FinalSignatures.Count(p => p != null) >= context.M && context.TransactionHashes.All(p => context.Transactions.ContainsKey(p)))
            {
                Contract contract = Contract.CreateMultiSigContract(context.M, context.Validators);
                Block block = context.MakeHeader();
                if (block == null) return;
                context.State |= ConsensusState.BlockSent;

                ContractParametersContext sc = new ContractParametersContext(block);
                for (int i = 0, j = 0; i < context.Validators.Length && j < context.M; i++)
                    if (context.FinalSignatures[i] != null)
                    {
                        sc.AddSignature(contract, context.Validators[i], context.FinalSignatures[i]);
                        j++;
                    }
                sc.Verifiable.Witnesses = sc.GetWitnesses();
                block.Transactions = context.TransactionHashes.Select(p => context.Transactions[p]).ToArray();
                Log($"relay block: height={context.BlockIndex} hash={block.Hash}");

                system.LocalNode.Tell(new LocalNode.Relay { Inventory = block });
            }
        }

        private void OnRenegeration(ConsensusPayload payload, Renegeration message)
        {
            if (context.State.HasFlag(ConsensusState.CommitSent)) return;

            Log($"{nameof(OnRenegeration)}: height={payload.BlockIndex} view={message.ViewNumber} numberOfPartialSignatures={message.SignedPayloads.Count(p => p != null)} index={payload.ValidatorIndex}");

            uint nValidSignatures = 0;
            /// <summary>
            /// Time for checking if speaker really signed this payload
            /// </summary>
            if (!CheckPrimaryPayloadSignature(message.PrepareRequestPayload))
            {
                Log($"Regerating primary payload: {message.PrepareRequestPayload.ValidatorIndex} lenght:{message.SignedPayloads.Length} with a wrong Primary Payload");
                context.SignedPayloads[payload.ValidatorIndex] = null;
                return;
            }
            nValidSignatures++;

            /// <summary>
            /// Time for checking all Backups
            /// </summary>
            for (int i = 0; i < message.SignedPayloads.Length; i++)
                if (message.SignedPayloads[i] != null && i != message.PrepareRequestPayload.ValidatorIndex)
                    if (!Crypto.Default.VerifySignature(message.PrepareRequestPayload.GetHashData(), message.SignedPayloads[i], context.Validators[i].EncodePoint(false)))
                    {
                        Log($"Regerating {i} payload:{message.PrepareRequestPayload.ValidatorIndex} lenght:{message.SignedPayloads.Length} is being set to null");
                        PrintByteArray(message.SignedPayloads[i]);
                        message.SignedPayloads[i] = null;
                    }
                    else{
                        nValidSignatures++;
                    }
            /// <summary>
            /// In order to start Regeneration, at least M signatures should had been verified and true
            /// </summary>
            if (nValidSignatures >= context.M)
            {
                Log($"Sorry. I lost some part of the history. I give up...");
                InitializeConsensus(message.ViewNumber);
                context.SignedPayloads = message.SignedPayloads;
                OnConsensusPayload(message.PrepareRequestPayload);
            }
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case Start _:
                    OnStart();
                    break;
                case SetViewNumber setView:
                    InitializeConsensus(setView.ViewNumber);
                    break;
                case Timer timer:
                    OnTimer(timer);
                    break;
                case ConsensusPayload payload:
                    OnConsensusPayload(payload);
                    break;
                case Transaction transaction:
                    OnTransaction(transaction);
                    break;
                case Blockchain.PersistCompleted completed:
                    OnPersistCompleted(completed.Block);
                    break;
            }
        }

        private void OnTimer(Timer timer)
        {
            if (context.State.HasFlag(ConsensusState.BlockSent)) return;
            if (timer.Height != context.BlockIndex || timer.ViewNumber != context.ViewNumber) return;
            Log($"timeout: height={timer.Height} view={timer.ViewNumber} state={context.State}");
            if (context.State.HasFlag(ConsensusState.Primary) && !context.State.HasFlag(ConsensusState.RequestSent))
            {
                Log($"send prepare request: height={timer.Height} view={timer.ViewNumber}");
                context.State |= ConsensusState.RequestSent;
                if (!context.State.HasFlag(ConsensusState.SignatureSent))
                {
                    FillContext();
                    context.Timestamp = Math.Max(DateTime.UtcNow.ToTimestamp(), context.Snapshot.GetHeader(context.PrevHash).Timestamp + 1);
                    context.SignedPayloads[context.MyIndex] = new byte[64];
                    context.PreparePayload = context.MakePrepareRequest(context.SignedPayloads[context.MyIndex]);
                    context.SignedPayloads[context.MyIndex] = context.PreparePayload.Sign(context.KeyPair);
                    PrepareRequest tempPrePrepareWithSignature = GetPrepareRequestMessage(context.PreparePayload);
                    tempPrePrepareWithSignature.PrepReqSignature = context.SignedPayloads[context.MyIndex];
                    context.PreparePayload.Data = tempPrePrepareWithSignature.ToArray();
                }

                if (context.PreparePayload == null)
                {
                    Log($"Error! PreparePayload is null");
                    return;
                }

                SignAndRelay(context.PreparePayload);
                if (context.TransactionHashes.Length > 1)
                {
                    foreach (InvPayload payload in InvPayload.CreateGroup(InventoryType.TX, context.TransactionHashes.Skip(1).ToArray()))
                        system.LocalNode.Tell(Message.Create("inv", payload));
                }
                ChangeTimer(TimeSpan.FromSeconds(Blockchain.SecondsPerBlock << (timer.ViewNumber + 1)));
            }
            else if ((context.State.HasFlag(ConsensusState.Primary) && context.State.HasFlag(ConsensusState.RequestSent)) || context.State.HasFlag(ConsensusState.Backup))
            {
                if (!context.State.HasFlag(ConsensusState.CommitSent))
                    RequestChangeView();
            }
        }

        private void OnTransaction(Transaction transaction)
        {
            if (transaction.Type == TransactionType.MinerTransaction) return;
            if (!context.State.HasFlag(ConsensusState.Backup) || !context.State.HasFlag(ConsensusState.RequestReceived) || context.State.HasFlag(ConsensusState.SignatureSent) || context.State.HasFlag(ConsensusState.ViewChanging) || context.State.HasFlag(ConsensusState.BlockSent))
                return;
            if (context.Transactions.ContainsKey(transaction.Hash)) return;
            if (!context.TransactionHashes.Contains(transaction.Hash)) return;
            AddTransaction(transaction, true);
        }

        protected override void PostStop()
        {
            Log("OnStop");
            context.Dispose();
            base.PostStop();
        }

        public static Props Props(NeoSystem system, Wallet wallet)
        {
            return Akka.Actor.Props.Create(() => new ConsensusService(system, wallet)).WithMailbox("consensus-service-mailbox");
        }

        private void RequestChangeView()
        {
            Log($"request change view: height={context.BlockIndex} view={context.ViewNumber} nv={context.ExpectedView[context.MyIndex]} state={context.State}");

            /// <summary>
            /// It should never Send Renegeration from here anymore after flag of CommitSent placed OnTimer
            /// TODO Remove this check.
            /// </summary>
            if (CheckRenegeration()) return;

            context.State |= ConsensusState.ViewChanging;
            context.ExpectedView[context.MyIndex]++;
            ChangeTimer(TimeSpan.FromSeconds(Blockchain.SecondsPerBlock << (context.ExpectedView[context.MyIndex] + 1)));
            SignAndRelay(context.MakeChangeView());
            CheckExpectedView(context.ExpectedView[context.MyIndex]);
        }

        private void SignAndRelay(ConsensusPayload payload)
        {
            ContractParametersContext sc;
            try
            {
                sc = new ContractParametersContext(payload);
                wallet.Sign(sc);
            }
            catch (InvalidOperationException)
            {
                return;
            }

            sc.Verifiable.Witnesses = sc.GetWitnesses();
            system.LocalNode.Tell(new LocalNode.SendDirectly { Inventory = payload });
        }
    }

    internal class ConsensusServiceMailbox : PriorityMailbox
    {
        public ConsensusServiceMailbox(Akka.Actor.Settings settings, Config config)
            : base(settings, config)
        {
        }

        protected override bool IsHighPriority(object message)
        {
            switch (message)
            {
                case ConsensusPayload _:
                case ConsensusService.SetViewNumber _:
                case ConsensusService.Timer _:
                case Blockchain.PersistCompleted _:
                    return true;
                default:
                    return false;
            }
        }
    }
}
