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

        private readonly ConsensusContext context;
        private readonly NeoSystem system;

        private DateTime block_received_time;

        public ConsensusService(NeoSystem system, Wallet wallet)
        {
            this.system = system;
            this.context = new ConsensusContext(wallet);
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
                if (context.VerifyRequest())
                {
                    Log($"send prepare response");
                    context.SignedPayloads[context.MyIndex] = context.SignPreparePayload();
                    context.State |= ConsensusState.SignatureSent;
                    system.LocalNode.Tell(new LocalNode.SendDirectly { Inventory = context.MakePrepareResponse(context.SignedPayloads[context.MyIndex]) });
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


        private void CheckFinalSignatures()
        {
            Log($"CheckFinalSignatures... FinalSignatures:{context.FinalSignatures.Count(p => p != null)}");

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
                        //Checking Speaker Final Signature that was given along with PrepareRequest Payload
                        if(context.PrimaryIndex == i && !Crypto.Default.VerifySignature(context.MakeHeader().GetHashData(), context.FinalSignatures[i], context.Validators[i].EncodePoint(false)))
                        {
                            Log($"CheckFinalSignatures...It looks like that Primary tried to cheat providing wrong final signature! His header signature will not be considered");
                            if ((context.FinalSignatures.Count(p => p != null) - 1) >= context.M)
                                continue;
                            else
                                break;
                        }
                            
                        sc.AddSignature(contract, context.Validators[i], context.FinalSignatures[i]);
                        j++;
                    }
                sc.Verifiable.Witnesses = sc.GetWitnesses();
                block.Transactions = context.TransactionHashes.Select(p => context.Transactions[p]).ToArray();
                Log($"{nameof(OnCommitAgreement)}: relay block: height={context.BlockIndex} hash={block.Hash}");
                system.LocalNode.Tell(new LocalNode.Relay { Inventory = block });
            }
        }
  

        private void InitializeConsensus(byte view_number)
        {
            if (view_number == 0)
                context.Reset();
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

        private bool CheckRegeneration()
        {
            if (context.State.HasFlag(ConsensusState.CommitSent))
            {
                Log($"CheckRegeneration: CommitSent flag. Sending Regeneration payload...");
                system.LocalNode.Tell(new LocalNode.SendDirectly { Inventory = context.MakeRegeneration() });
                Log($"Regeneration sent: height={context.BlockIndex} view={context.ViewNumber} state={context.State}");
                return true;
            }

            return false;
        }

        private void OnChangeViewReceived(ConsensusPayload payload, ChangeView message)
        {
            Log($"{nameof(OnChangeViewReceived)}: height={payload.BlockIndex} view={message.ViewNumber} index={payload.ValidatorIndex} nv={message.NewViewNumber}");

            if(CheckRegeneration()) return;

            if (message.NewViewNumber <= context.ExpectedView[payload.ValidatorIndex])
                return;
            Log($"{nameof(OnChangeViewReceived)}: height={payload.BlockIndex} view={message.ViewNumber} index={payload.ValidatorIndex} nv={message.NewViewNumber}");
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
                    // TODO Thinking about a delay here in order to ask for blocks and then initialize from view 1;
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

            if (message.ViewNumber != context.ViewNumber && message.Type != ConsensusMessageType.ChangeView && message.Type != ConsensusMessageType.Regeneration)
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
                case ConsensusMessageType.Regeneration:
                    OnRegeneration(payload, (Regeneration)message);
                    break;
            }
        }

        private void OnPersistCompleted(Block block)
        {
            Log($"persist block: {block.Hash}");
            block_received_time = DateTime.UtcNow;
            InitializeConsensus(0);
        }

        private void OnPrepareRequestReceived(ConsensusPayload payload, PrepareRequest message, bool regenerationCall = false)
        {
            if (context.State.HasFlag(ConsensusState.RequestReceived)) return;
            if (payload.ValidatorIndex != context.PrimaryIndex) return;
            Log($"{nameof(OnPrepareRequestReceived)}: height={payload.BlockIndex} view={message.ViewNumber} index={payload.ValidatorIndex} tx={message.TransactionHashes.Length}");
            if (!context.State.HasFlag(ConsensusState.Backup) && !regenerationCall) return;

            if (payload.Timestamp <= context.Snapshot.GetHeader(context.PrevHash).Timestamp || payload.Timestamp > DateTime.UtcNow.AddMinutes(10).ToTimestamp())
            {
                Log($"{nameof(OnPrepareRequestReceived)}: Timestamp incorrect: {payload.Timestamp}", LogLevel.Warning);
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
            context.FinalSignatures[payload.ValidatorIndex] = message.FinalSignature;

            if (!CheckPrimaryPayloadSignature(payload))
            {
                context.SignedPayloads[payload.ValidatorIndex] = null;
                return;
            }

            for (int i = 0; i < context.SignedPayloads.Length; i++)
                if (context.SignedPayloads[i] != null && i != payload.ValidatorIndex)
                    if (!Crypto.Default.VerifySignature(context.PreparePayload.GetHashData(), context.SignedPayloads[i], context.Validators[i].EncodePoint(false)))
                    {
                        Log($"{nameof(OnPrepareRequestReceived)}:Index {i} payload:{payload.ValidatorIndex} length:{context.SignedPayloads.Length} is being set to null");
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
            /// TODO Maybe include some verification here
            PrepareRequest message = context.GetPrepareRequestMessage(payload);

            /// The Speaker Signed the Payload without any signature (this was the trick/magic part), PrepReqSignature was empty
            /// But the payload was latter modified with his signature,
            /// We mean, the PrepareRequest message was filled with the PrepReqSignature of the Empty Payload and then serialized again into this Payload.
            /// Thus, we need to remove the signature from the Payload to correctly verify Speaker identity agreements with this block
            byte[] tempSignature = message.PrepReqSignature;
            message.PrepReqSignature = new byte[64];
            payload.Data = message.ToArray();

            if (!Crypto.Default.VerifySignature(payload.GetHashData(), tempSignature, context.Validators[payload.ValidatorIndex].EncodePoint(false)))
                return false;

            /// maybe these next 2 lines could be removed, because payload is not anymore used
            /// it was already saved before changed in the context.PreparePayload... However, let keep things clean for now
            message.PrepReqSignature = tempSignature;
            payload.Data = message.ToArray();
            return true;
        }


        private void OnPrepareResponseReceived(ConsensusPayload payload, PrepareResponse message)
        {
            if (context.State.HasFlag(ConsensusState.CommitSent)) return;

            /// This payload.ValidatorIndex already submitted a not null signature
            if (context.SignedPayloads[payload.ValidatorIndex] != null) return;

            Log($"{nameof(OnPrepareResponseReceived)}: height={payload.BlockIndex} view={message.ViewNumber} index={payload.ValidatorIndex}");

            /// ***** The following is like an additional feature ******
            /// ORIGINAL CODE: In the original code we were just storing the signature and later verifying when the Payload really arrives.
            /// FEATURED ONE: Here we can check the PreparePayload if it did not arrive, because all PrepareREsponse Payloads carries that
            if (context.PreparePayload == null)
            {
                Log($"{nameof(OnPrepareRequestReceived)}: Response before PrepRequest, trying to speed up p2p route...");
                if (message.PreparePayload.ValidatorIndex != context.PrimaryIndex) return;
                if (!CheckPrimaryPayloadSignature(message.PreparePayload)) return;
                Log($"{nameof(OnPrepareRequestReceived)}: indirectly from index={payload.ValidatorIndex}");
                OnPrepareRequestReceived(message.PreparePayload, context.GetPrepareRequestMessage(message.PreparePayload));
            }

            /// Time to check received Signature against our local context.PreparePayload
            if (!Crypto.Default.VerifySignature(context.PreparePayload.GetHashData(), message.ResponseSignature, context.Validators[payload.ValidatorIndex].EncodePoint(false))) return;

            context.SignedPayloads[payload.ValidatorIndex] = message.ResponseSignature;
            CheckPayloadSignatures();
        }

        private void CheckPayloadSignatures()
        {
            Log($"CheckPayloadSignatures... SignedPayloads:{context.SignedPayloads.Count(p => p != null)}");

            if (!context.State.HasFlag(ConsensusState.CommitSent) &&
                context.SignedPayloads.Count(p => p != null) >= context.M &&
                context.TransactionHashes.All(p => context.Transactions.ContainsKey(p)))
            {
                Block block = context.MakeHeader();
                if (block == null) return;

                /// Do not sign for Primary because it will generate a signature different than the one provide in the PrepareRequest to the Backups
                /// In principle, we could also skip the LocalNode.Tell of Primary (because they will discard it).
                /// However, it is a way of notifying the nodes that the primary is commited and will be surely be a metric in the future
                if ((uint)context.MyIndex != context.PrimaryIndex)
                    context.FinalSignatures[context.MyIndex] = context.SignBlock(block);
                
                context.State |= ConsensusState.CommitSent;

                system.LocalNode.Tell(new LocalNode.SendDirectly { Inventory = context.MakeCommitAgreement(context.FinalSignatures[context.MyIndex]) });
                Log($"Commit sent: height={context.BlockIndex} hash={block.Hash} state={context.State}");
                CheckFinalSignatures();
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
            CheckFinalSignatures();
        }

        private void OnRegeneration(ConsensusPayload payload, Regeneration message)
        {
            if (context.State.HasFlag(ConsensusState.CommitSent)) return;

            Log($"{nameof(OnRegeneration)}: height={payload.BlockIndex} view={message.ViewNumber} numberOfPartialSignatures={message.SignedPayloads.Count(p => p != null)} index={payload.ValidatorIndex}");

            uint nValidSignatures = 0;
            /// Time for checking if speaker really signed this payload
            if (!CheckPrimaryPayloadSignature(message.PrepareRequestPayload))
            {
                Log($"{nameof(OnRegeneration)}: Regenerating primary payload: {message.PrepareRequestPayload.ValidatorIndex} length:{message.SignedPayloads.Length} with a wrong Primary Payload");
                context.SignedPayloads[payload.ValidatorIndex] = null;
                return;
            }
            nValidSignatures++;

            /// Time for checking all Backups
            for (int i = 0; i < message.SignedPayloads.Length; i++)
                if (message.SignedPayloads[i] != null && i != message.PrepareRequestPayload.ValidatorIndex)
                    if (!Crypto.Default.VerifySignature(message.PrepareRequestPayload.GetHashData(), message.SignedPayloads[i], context.Validators[i].EncodePoint(false)))
                    {
                        Log($"{nameof(OnRegeneration)}: Regenerating {i} payload:{message.PrepareRequestPayload.ValidatorIndex} length:{message.SignedPayloads.Length} is being set to null");
                        PrintByteArray(message.SignedPayloads[i]);
                        message.SignedPayloads[i] = null;
                    }
                    else{
                        nValidSignatures++;
                    }

            /// In order to start Regeneration, at least M signatures should had been verified and true
            if (nValidSignatures >= context.M)
            {
                Log($"{nameof(OnRegeneration)}: Sorry. I lost some part of the history. I give up... Thanks index={payload.ValidatorIndex}");
                InitializeConsensus(message.ViewNumber);
                context.SignedPayloads = message.SignedPayloads;
                OnPrepareRequestReceived(message.PrepareRequestPayload, context.GetPrepareRequestMessage(message.PrepareRequestPayload), true);
                Log($"{nameof(OnRegeneration)}: OnConsensusPayload. message.PrepareRequestPayload has been sent.");
            }
            Log($"{nameof(OnRegeneration)}: Bye bye. I feel good now, connected and on top.");
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

        private void OnStart()
        {
            Log("OnStart");
            InitializeConsensus(0);
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
                    context.Fill();
                    context.Timestamp = Math.Max(DateTime.UtcNow.ToTimestamp(), context.Snapshot.GetHeader(context.PrevHash).Timestamp + 1);
                }
                Block block = context.MakeHeader();
                if (block == null) return;
                context.FinalSignatures[context.MyIndex] = context.SignBlock(block);
                context.PreparePayload = context.MakePrepareRequest(new byte[64], context.FinalSignatures[context.MyIndex]);
                context.UpdateSpeakerSignatureAtPreparePayload();

                if (context.PreparePayload == null)
                {
                    Log($"ONTIMER:  Error! PreparePayload is null");
                    return;
                }

                context.SignPayload(context.PreparePayload);
                system.LocalNode.Tell(new LocalNode.SendDirectly { Inventory = context.PreparePayload });

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
            if (!context.State.HasFlag(ConsensusState.Backup) || !context.State.HasFlag(ConsensusState.RequestReceived) || context.State.HasFlag(ConsensusState.ViewChanging) || context.State.HasFlag(ConsensusState.BlockSent))
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

            /// [PROBABLY OBSOLET] It should never send Regeneration from here anymore after flag of CommitSent placed OnTimer
            if (CheckRegeneration()) return;

            context.State |= ConsensusState.ViewChanging;
            context.ExpectedView[context.MyIndex]++;
            Log($"request change view: height={context.BlockIndex} view={context.ViewNumber} nv={context.ExpectedView[context.MyIndex]} state={context.State}");
            ChangeTimer(TimeSpan.FromSeconds(Blockchain.SecondsPerBlock << (context.ExpectedView[context.MyIndex] + 1)));
            //SignAndRelay(context.MakeChangeView());
            system.LocalNode.Tell(new LocalNode.SendDirectly { Inventory = context.MakeChangeView() });
            CheckExpectedView(context.ExpectedView[context.MyIndex]);
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
