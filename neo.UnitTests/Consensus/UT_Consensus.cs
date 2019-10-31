using Akka.TestKit;
using Akka.TestKit.Xunit2;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Consensus;
using Neo.Cryptography;
using Neo.UnitTests.Cryptography;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using ECPoint = Neo.Cryptography.ECC.ECPoint;

namespace Neo.UnitTests.Consensus
{

    [TestClass]
    public class ConsensusTests : TestKit
    {
        [TestInitialize]
        public void TestSetup()
        {
            TestBlockchain.InitializeMockNeoSystem();
        }

        [TestCleanup]
        public void Cleanup()
        {
            Shutdown();
        }

        [TestMethod]
        public void ConsensusService_SingleNodeActors_OnStart_PrepReq_PrepResponses_Commits()
        {
            var mockWallet = new Mock<Wallet>();
            mockWallet.Setup(p => p.GetAccount(It.IsAny<UInt160>())).Returns<UInt160>(p => new TestWalletAccount(p));
            var mockContext = new Mock<ConsensusContext>(mockWallet.Object, TestBlockchain.GetStore());
            mockContext.Object.LastSeenMessage = new int[] { 0, 0, 0, 0, 0, 0, 0 };

            var timeValues = new[] {
              new DateTime(1980, 06, 01, 0, 0, 1, 001, DateTimeKind.Utc),  // For tests, used below
              new DateTime(1980, 06, 01, 0, 0, 3, 001, DateTimeKind.Utc),  // For receiving block
              new DateTime(1980, 05, 01, 0, 0, 5, 001, DateTimeKind.Utc), // For Initialize
              new DateTime(1980, 06, 01, 0, 0, 15, 001, DateTimeKind.Utc), // unused
            };
            Console.WriteLine($"time 0: {timeValues[0].ToString()} 1: {timeValues[1].ToString()} 2: {timeValues[2].ToString()} 3: {timeValues[3].ToString()}");

            int timeIndex = 0;
            var timeMock = new Mock<TimeProvider>();
            timeMock.SetupGet(tp => tp.UtcNow).Returns(() => timeValues[timeIndex]);
            //.Callback(() => timeIndex = timeIndex + 1); //Comment while index is not fixed

            //new DateTime(1968, 06, 01, 0, 0, 15, DateTimeKind.Utc));
            TimeProvider.Current = timeMock.Object;
            TimeProvider.Current.UtcNow.ToTimestampMS().Should().Be(328665601001); //1980-06-01 00:00:15:001

            //public void Log(string message, LogLevel level)
            // TODO: create ILogPlugin for Tests
            /*
            mockConsensusContext.Setup(mr => mr.Log(It.IsAny<string>(), It.IsAny<LogLevel>()))
                         .Callback((string message, LogLevel level) => {
                                         Console.WriteLine($"CONSENSUS LOG: {message}");
                                                                   }
                                  );
             */

            // Creating proposed block
            Header header = new Header();
            TestUtils.SetupHeaderWithValues(header, UInt256.Zero, out UInt256 merkRootVal, out UInt160 val160, out ulong timestampVal, out uint indexVal, out Witness scriptVal);
            header.Size.Should().Be(105);

            Console.WriteLine($"header {header} hash {header.Hash} timestamp {timestampVal}");

            timestampVal.Should().Be(328665601001);    // GMT: Sunday, June 1, 1980 12:00:01.001 AM
                                                       // check basic ConsensusContext
                                                       // mockConsensusContext.Object.block_received_time.ToTimestamp().Should().Be(4244941697); //1968-06-01 00:00:01

            // ============================================================================
            //                      creating ConsensusService actor
            // ============================================================================
            TestProbe subscriber = CreateTestProbe();
            TestActorRef<ConsensusService> actorConsensus = ActorOfAsTestActorRef<ConsensusService>(
                                     Akka.Actor.Props.Create(() => (ConsensusService)Activator.CreateInstance(typeof(ConsensusService), BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { subscriber, subscriber, mockContext.Object }, null))
                                     );

            Console.WriteLine("will trigger OnPersistCompleted!");
            actorConsensus.Tell(new Blockchain.PersistCompleted
            {
                Block = new Block
                {
                    Version = header.Version,
                    PrevHash = header.PrevHash,
                    MerkleRoot = header.MerkleRoot,
                    Timestamp = header.Timestamp,
                    Index = header.Index,
                    NextConsensus = header.NextConsensus
                }
            });
            // OnPersist will not launch timer, we need OnStart

            Console.WriteLine("will start consensus!");
            actorConsensus.Tell(new ConsensusService.Start
            {
                IgnoreRecoveryLogs = true
            });

            Console.WriteLine("Waiting for subscriber recovery message...");
            var askingForInitialRecovery = subscriber.ExpectMsg<LocalNode.SendDirectly>();
            Console.WriteLine($"Recovery Message I: {askingForInitialRecovery}");
            ConsensusPayload cp = (ConsensusPayload)askingForInitialRecovery.Inventory;
            RecoveryRequest rrm = (RecoveryRequest)cp.ConsensusMessage;
            rrm.Timestamp.Should().Be(328665601001);

            Console.WriteLine("Waiting for backupChange View... ");
            // LocalNode.SendDirectly nextMsgCV = new LocalNode.SendDirectly { Inventory = mockContext.Object.MakeChangeView(ChangeViewReason.Timeout) };
            // USE Predicate<T> TODO

            var backupOnAskingChangeView = subscriber.ExpectMsg<LocalNode.SendDirectly>();
            cp = (ConsensusPayload)backupOnAskingChangeView.Inventory;
            ChangeView cvm = (ChangeView)cp.ConsensusMessage;
            cvm.Timestamp.Should().Be(328665601001);
            cvm.ViewNumber.Should().Be(0);
            cvm.Reason.Should().Be(ChangeViewReason.Timeout);
            // Disabling flag ViewChanging
            mockContext.Object.ChangeViewPayloads[mockContext.Object.MyIndex] = null;

            Console.WriteLine("Forcing Failed nodes for recovery request... ");
            mockContext.Object.CountFailed.Should().Be(0);
            mockContext.Object.LastSeenMessage = new int[] { -1, -1, -1, -1, -1, -1, -1 };
            mockContext.Object.CountFailed.Should().Be(7);

            Console.WriteLine("\nWaiting for recovery due to failed nodes... ");
            var backupOnRecoveryDueToFailedNodes = subscriber.ExpectMsg<LocalNode.SendDirectly>();
            cp = (ConsensusPayload)backupOnRecoveryDueToFailedNodes.Inventory;
            rrm = (RecoveryRequest)cp.ConsensusMessage;
            rrm.Timestamp.Should().Be(328665601001);

            //Console.WriteLine("OnTimer Of Backup should expire...");
            //var backupOnTimer = subscriber.ExpectMsg<ConsensusService.Timer>();

            //Console.WriteLine("Telling PrepRequest... ");
            // TimeStamps can be manipuated
            // timeMock.SetupGet(tp => tp.UtcNow).Returns(() => timeValues[1]);
            // But we will manipulate changeview for now
            // mockContext.Object.ViewNumber = 0;

            Console.WriteLine("will tell PrepRequest!");
            mockContext.Object.PrevHeader.Timestamp = 328665601000;
            mockContext.Object.PrevHeader.NextConsensus.Should().Be(UInt160.Parse("0x0656f4bee614d132409c587097522bf789ab15e4"));
            var prepReq = mockContext.Object.MakePrepareRequest();
            var ppToSend = (PrepareRequest)prepReq.ConsensusMessage;

            // Forcing hashes to 0 because mempool is currently shared
            ppToSend.TransactionHashes = new UInt256[0];
            ppToSend.TransactionHashes.Length.Should().Be(0);

            actorConsensus.Tell(prepReq);
            Console.WriteLine("Waiting for something related to the PrepRequest...\nNothing happens...Recovery will come due to failed nodes");
            var backupOnRecoveryDueToFailedNodesII = subscriber.ExpectMsg<LocalNode.SendDirectly>();
            cp = (ConsensusPayload)backupOnRecoveryDueToFailedNodesII.Inventory;
            rrm = (RecoveryRequest)cp.ConsensusMessage;

            Console.WriteLine("\nFailed because it is not primary and it created the prereq...Time to adjust");
            prepReq.ValidatorIndex = 1; //simulating primary as prepreq creator (signature is skip, no problem)
            // cleaning old try with Self ValidatorIndex
            mockContext.Object.PreparationPayloads[mockContext.Object.MyIndex] = null;
            actorConsensus.Tell(prepReq);
            var OnPrepResponse = subscriber.ExpectMsg<LocalNode.SendDirectly>();
            cp = (ConsensusPayload)OnPrepResponse.Inventory;
            PrepareResponse prm = (PrepareResponse)cp.ConsensusMessage;
            prm.PreparationHash.Should().Be(prepReq.Hash);

            // Simulating CN 2
            actorConsensus.Tell(getPreparationPayloadModifiedAndSignedCopy(cp, 2));

            actorConsensus.Tell(getPreparationPayloadModifiedAndSignedCopy(cp, 4));

            actorConsensus.Tell(getPreparationPayloadModifiedAndSignedCopy(cp, 3));

            var onCommitPayload = subscriber.ExpectMsg<LocalNode.SendDirectly>();
            cp = (ConsensusPayload)onCommitPayload.Inventory;
            Commit cm = (Commit)cp.ConsensusMessage;
            // "invocation":"40661c47bec665f3e5b3659dfddd7a33102494509c46e15b210c4655234f7cb4e2916d37b147b15381486968a2491074a8cc94c20811abfa391b072527044de2eb", "verification":"2103b20fabb1421b2c16c1318529d1549058dcf66bc46dd148ad1b84e484204195cc68747476aa"
            //cp.Witness.InvocationScript.ToHexString().Should().Be("40661c47bec665f3e5b3659dfddd7a33102494509c46e15b210c4655234f7cb4e2916d37b147b15381486968a2491074a8cc94c20811abfa391b072527044de2eb");
            //cp.Witness.VerificationScript.ToString().Should().Be("2103b20fabb1421b2c16c1318529d1549058dcf66bc46dd148ad1b84e484204195cc68747476aa");

            Console.WriteLine($"(UT-Consensus) Wallet is: {mockWallet.Object.GetAccount(UInt160.Zero).GetKey().PublicKey}");
            // Changes on every execution
            //mockWallet.Object.GetAccount(UInt160.Zero).GetKey().PublicKey.Should().Be(ECPoint.Parse("03464bf27fe4c4eedada30738e43591f199fea29fb170045f5638ff3e9ba5c842c", Neo.Cryptography.ECC.ECCurve.Secp256r1));

            var kp1 = UT_Crypto.generateKey(32);

            KeyPair[] kp_array = new KeyPair[5]
                {
                    UT_Crypto.generateKey(32), // not used, kept for index consistency, didactically
                    UT_Crypto.generateKey(32),
                    UT_Crypto.generateKey(32),
                    UT_Crypto.generateKey(32),
                    UT_Crypto.generateKey(32)
                };

            // Original Contract
            Contract originalContract = Contract.CreateMultiSigContract(mockContext.Object.M, mockContext.Object.Validators);
            // Console.WriteLine($"\n Contract is: {contract.ScriptHash}");
            originalContract.ScriptHash.Should().Be(UInt160.Parse("0x0656f4bee614d132409c587097522bf789ab15e4"));
            mockContext.Object.Block.NextConsensus.Should().Be(UInt160.Parse("0x0656f4bee614d132409c587097522bf789ab15e4"));

            Console.WriteLine($"\nBlockHash: {mockContext.Object.Block.Hash}");
            Console.WriteLine($"\nBlock NextConsensus: {mockContext.Object.Block.NextConsensus}");

            mockContext.Object.Validators = new ECPoint[7]
                {
                    mockContext.Object.Validators[0],
                    kp_array[1].PublicKey,
                    kp_array[2].PublicKey,
                    kp_array[3].PublicKey,
                    kp_array[4].PublicKey,
                    mockContext.Object.Validators[5],
                    mockContext.Object.Validators[6]
                };
            Console.WriteLine($"Generated keypairs PKey:");
            for (int i = 0; i < mockContext.Object.Validators.Length; i++)
                Console.WriteLine($"{mockContext.Object.Validators[i]}");

            // Original Contract
            var updatedContract = Contract.CreateMultiSigContract(mockContext.Object.M, mockContext.Object.Validators);
            // Can not assert, because KeyPairs are randoms
            Console.WriteLine($"\nContract updated: {updatedContract.ScriptHash}");

            // Forcing next consensus
            mockContext.Object.Block.NextConsensus = updatedContract.ScriptHash;
            mockContext.Object.PrevHeader.NextConsensus = updatedContract.ScriptHash;
            var originalBlockMerkleRoot = mockContext.Object.Block.MerkleRoot;

            var blockToSign = mockContext.Object.EnsureHeader();
            Console.WriteLine($"\nBlockHash: {blockToSign.Hash}");

            Console.WriteLine("\n\n==========================");
            Console.WriteLine("\nCN2 simulation time");
            actorConsensus.Tell(getCommitPayloadModifiedAndSignedCopy(cp, 1, kp_array[1], blockToSign));

            Console.WriteLine("\nCN3 simulation time");
            actorConsensus.Tell(getCommitPayloadModifiedAndSignedCopy(cp, 2, kp_array[2], blockToSign));

            Console.WriteLine("\nCN4 simulation time");
            actorConsensus.Tell(getCommitPayloadModifiedAndSignedCopy(cp, 3, kp_array[3], blockToSign));

            // =============================================
            // Testing commit with wrong signature not valid
            // It will be invalid signature because we did not change ECPoint
            Console.WriteLine("\nCN6 simulation time. Wrong signature, KeyPair is not known");
            cp.ValidatorIndex = 5;
            actorConsensus.Tell(cp.ToArray().AsSerializable<ConsensusPayload>());

            Console.WriteLine("\nWaiting for recovery due to failed nodes... ");
            var backupOnRecoveryMessageAfterCommit = subscriber.ExpectMsg<LocalNode.SendDirectly>();
            var rmPayload = (ConsensusPayload)backupOnRecoveryMessageAfterCommit.Inventory;
            RecoveryMessage rmm = (RecoveryMessage)rmPayload.ConsensusMessage;
            // =============================================

            Console.WriteLine("\nCN5 simulation time");
            actorConsensus.Tell(getCommitPayloadModifiedAndSignedCopy(cp, 4, kp_array[4], blockToSign));

            var onBlockRelay = subscriber.ExpectMsg<LocalNode.Relay>();
            var utBlock = (Block)onBlockRelay.Inventory;

            //var lastPayloadOfChangeView = subscriber.ExpectMsg<Neo.Consensus.ConsensusService.Timer>();

            Console.WriteLine($"\nAsserting block NextConsensus..{utBlock.NextConsensus}");
            utBlock.NextConsensus.Should().Be(updatedContract.ScriptHash);

            // ============================================================================
            //                      finalize ConsensusService actor
            // ============================================================================
            //Thread.Sleep(4000);
            Console.WriteLine("Finalizing consensus service actor and returning states.");

            // Returning values to context beucase tests are not isolated
            mockContext.Object.Block.NextConsensus = originalContract.ScriptHash;
            mockContext.Object.PrevHeader.NextConsensus = originalContract.ScriptHash;
            mockContext.Object.Block.MerkleRoot = originalBlockMerkleRoot;

            mockContext.Object.Reset(0);
            Sys.Stop(actorConsensus);
            TimeProvider.ResetToDefault();
            // Ensure thread is clear
            Assert.AreEqual(1, 1);
        }

        public ConsensusPayload getCommitPayloadModifiedAndSignedCopy(ConsensusPayload cpToCopy, ushort vI, KeyPair kp, Block blockToSign)
        {
            var cpCommitTemp = cpToCopy.ToArray().AsSerializable<ConsensusPayload>();
            cpCommitTemp.ValidatorIndex = vI;
            cpCommitTemp.ConsensusMessage = cpToCopy.ConsensusMessage.ToArray().AsSerializable<Commit>();
            ((Commit)cpCommitTemp.ConsensusMessage).Signature = blockToSign.Sign(kp);
            Console.WriteLine($"getCommitPayloadModifiedAndSignedCopy: {((Commit)cpCommitTemp.ConsensusMessage).Signature.ToHexString()}");
            return cpCommitTemp;
        }

        public ConsensusPayload getPreparationPayloadModifiedAndSignedCopy(ConsensusPayload cpToCopy, ushort vI)
        {
            var cpPreparationTemp = cpToCopy.ToArray().AsSerializable<ConsensusPayload>();
            cpPreparationTemp.ValidatorIndex = vI;
            return cpPreparationTemp;
        }

        [TestMethod]
        public void TestSerializeAndDeserializeConsensusContext()
        {
            var consensusContext = new ConsensusContext(null, null)
            {
                Block = new Block
                {
                    PrevHash = Blockchain.GenesisBlock.Hash,
                    Index = 1,
                    Timestamp = 4244941711,
                    NextConsensus = UInt160.Parse("5555AAAA5555AAAA5555AAAA5555AAAA5555AAAA"),
                    ConsensusData = new ConsensusData
                    {
                        PrimaryIndex = 6
                    }
                },
                ViewNumber = 2,
                Validators = new ECPoint[7]
                {
                    ECPoint.Parse("02486fd15702c4490a26703112a5cc1d0923fd697a33406bd5a1c00e0013b09a70", Neo.Cryptography.ECC.ECCurve.Secp256r1),
                    ECPoint.Parse("024c7b7fb6c310fccf1ba33b082519d82964ea93868d676662d4a59ad548df0e7d", Neo.Cryptography.ECC.ECCurve.Secp256r1),
                    ECPoint.Parse("02aaec38470f6aad0042c6e877cfd8087d2676b0f516fddd362801b9bd3936399e", Neo.Cryptography.ECC.ECCurve.Secp256r1),
                    ECPoint.Parse("02ca0e27697b9c248f6f16e085fd0061e26f44da85b58ee835c110caa5ec3ba554", Neo.Cryptography.ECC.ECCurve.Secp256r1),
                    ECPoint.Parse("02df48f60e8f3e01c48ff40b9b7f1310d7a8b2a193188befe1c2e3df740e895093", Neo.Cryptography.ECC.ECCurve.Secp256r1),
                    ECPoint.Parse("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c", Neo.Cryptography.ECC.ECCurve.Secp256r1),
                    ECPoint.Parse("03b8d9d5771d8f513aa0869b9cc8d50986403b78c6da36890638c3d46a5adce04a", Neo.Cryptography.ECC.ECCurve.Secp256r1)
                },
                MyIndex = -1
            };
            var testTx1 = TestUtils.CreateRandomHashTransaction();
            var testTx2 = TestUtils.CreateRandomHashTransaction();

            int txCountToInlcude = 256;
            consensusContext.TransactionHashes = new UInt256[txCountToInlcude];

            Transaction[] txs = new Transaction[txCountToInlcude];
            for (int i = 0; i < txCountToInlcude; i++)
            {
                txs[i] = TestUtils.CreateRandomHashTransaction();
                consensusContext.TransactionHashes[i] = txs[i].Hash;
            }
            // consensusContext.TransactionHashes = new UInt256[2] {testTx1.Hash, testTx2.Hash};
            consensusContext.Transactions = txs.ToDictionary(p => p.Hash);

            consensusContext.PreparationPayloads = new ConsensusPayload[consensusContext.Validators.Length];
            var prepareRequestMessage = new PrepareRequest
            {
                TransactionHashes = consensusContext.TransactionHashes,
                Timestamp = 23
            };
            consensusContext.PreparationPayloads[6] = MakeSignedPayload(consensusContext, prepareRequestMessage, 6, new[] { (byte)'3', (byte)'!' });
            consensusContext.PreparationPayloads[0] = MakeSignedPayload(consensusContext, new PrepareResponse { PreparationHash = consensusContext.PreparationPayloads[6].Hash }, 0, new[] { (byte)'t', (byte)'e' });
            consensusContext.PreparationPayloads[1] = MakeSignedPayload(consensusContext, new PrepareResponse { PreparationHash = consensusContext.PreparationPayloads[6].Hash }, 1, new[] { (byte)'s', (byte)'t' });
            consensusContext.PreparationPayloads[2] = null;
            consensusContext.PreparationPayloads[3] = MakeSignedPayload(consensusContext, new PrepareResponse { PreparationHash = consensusContext.PreparationPayloads[6].Hash }, 3, new[] { (byte)'1', (byte)'2' });
            consensusContext.PreparationPayloads[4] = null;
            consensusContext.PreparationPayloads[5] = null;

            consensusContext.CommitPayloads = new ConsensusPayload[consensusContext.Validators.Length];
            using (SHA256 sha256 = SHA256.Create())
            {
                consensusContext.CommitPayloads[3] = MakeSignedPayload(consensusContext, new Commit { Signature = sha256.ComputeHash(testTx1.Hash.ToArray()) }, 3, new[] { (byte)'3', (byte)'4' });
                consensusContext.CommitPayloads[6] = MakeSignedPayload(consensusContext, new Commit { Signature = sha256.ComputeHash(testTx2.Hash.ToArray()) }, 3, new[] { (byte)'6', (byte)'7' });
            }

            consensusContext.Block.Timestamp = TimeProvider.Current.UtcNow.ToTimestampMS();

            consensusContext.ChangeViewPayloads = new ConsensusPayload[consensusContext.Validators.Length];
            consensusContext.ChangeViewPayloads[0] = MakeSignedPayload(consensusContext, new ChangeView { ViewNumber = 1, Timestamp = 6 }, 0, new[] { (byte)'A' });
            consensusContext.ChangeViewPayloads[1] = MakeSignedPayload(consensusContext, new ChangeView { ViewNumber = 1, Timestamp = 5 }, 1, new[] { (byte)'B' });
            consensusContext.ChangeViewPayloads[2] = null;
            consensusContext.ChangeViewPayloads[3] = MakeSignedPayload(consensusContext, new ChangeView { ViewNumber = 1, Timestamp = uint.MaxValue }, 3, new[] { (byte)'C' });
            consensusContext.ChangeViewPayloads[4] = null;
            consensusContext.ChangeViewPayloads[5] = null;
            consensusContext.ChangeViewPayloads[6] = MakeSignedPayload(consensusContext, new ChangeView { ViewNumber = 1, Timestamp = 1 }, 6, new[] { (byte)'D' });

            consensusContext.LastChangeViewPayloads = new ConsensusPayload[consensusContext.Validators.Length];

            var copiedContext = TestUtils.CopyMsgBySerialization(consensusContext, new ConsensusContext(null, null));

            copiedContext.Block.PrevHash.Should().Be(consensusContext.Block.PrevHash);
            copiedContext.Block.Index.Should().Be(consensusContext.Block.Index);
            copiedContext.ViewNumber.Should().Be(consensusContext.ViewNumber);
            copiedContext.Validators.Should().BeEquivalentTo(consensusContext.Validators);
            copiedContext.MyIndex.Should().Be(consensusContext.MyIndex);
            copiedContext.Block.ConsensusData.PrimaryIndex.Should().Be(consensusContext.Block.ConsensusData.PrimaryIndex);
            copiedContext.Block.Timestamp.Should().Be(consensusContext.Block.Timestamp);
            copiedContext.Block.NextConsensus.Should().Be(consensusContext.Block.NextConsensus);
            copiedContext.TransactionHashes.Should().BeEquivalentTo(consensusContext.TransactionHashes);
            copiedContext.Transactions.Should().BeEquivalentTo(consensusContext.Transactions);
            copiedContext.Transactions.Values.Should().BeEquivalentTo(consensusContext.Transactions.Values);
            copiedContext.PreparationPayloads.Should().BeEquivalentTo(consensusContext.PreparationPayloads);
            copiedContext.CommitPayloads.Should().BeEquivalentTo(consensusContext.CommitPayloads);
            copiedContext.ChangeViewPayloads.Should().BeEquivalentTo(consensusContext.ChangeViewPayloads);
        }

        [TestMethod]
        public void TestSerializeAndDeserializeRecoveryMessageWithChangeViewsAndNoPrepareRequest()
        {
            var msg = new RecoveryMessage
            {
                ChangeViewMessages = new Dictionary<int, RecoveryMessage.ChangeViewPayloadCompact>()
                {
                    {
                        0,
                        new RecoveryMessage.ChangeViewPayloadCompact
                        {
                            ValidatorIndex = 0,
                            OriginalViewNumber = 9,
                            Timestamp = 6,
                            InvocationScript = new[] { (byte)'A' }
                        }
                    },
                    {
                        1,
                        new RecoveryMessage.ChangeViewPayloadCompact
                        {
                            ValidatorIndex = 1,
                            OriginalViewNumber = 7,
                            Timestamp = 5,
                            InvocationScript = new[] { (byte)'B' }
                        }
                    },
                    {
                        3,
                        new RecoveryMessage.ChangeViewPayloadCompact
                        {
                            ValidatorIndex = 3,
                            OriginalViewNumber = 5,
                            Timestamp = 3,
                            InvocationScript = new[] { (byte)'C' }
                        }
                    },
                    {
                        6,
                        new RecoveryMessage.ChangeViewPayloadCompact
                        {
                            ValidatorIndex = 6,
                            OriginalViewNumber = 2,
                            Timestamp = 1,
                            InvocationScript = new[] { (byte)'D' }
                        }
                    }
                },
                PreparationHash = new UInt256(Crypto.Default.Hash256(new[] { (byte)'a' })),
                PreparationMessages = new Dictionary<int, RecoveryMessage.PreparationPayloadCompact>()
                {
                    {
                        0,
                        new RecoveryMessage.PreparationPayloadCompact
                        {
                            ValidatorIndex = 0,
                            InvocationScript = new[] { (byte)'t', (byte)'e' }
                        }
                    },
                    {
                        3,
                        new RecoveryMessage.PreparationPayloadCompact
                        {
                            ValidatorIndex = 3,
                            InvocationScript = new[] { (byte)'1', (byte)'2' }
                        }
                    },
                    {
                        6,
                        new RecoveryMessage.PreparationPayloadCompact
                        {
                            ValidatorIndex = 6,
                            InvocationScript = new[] { (byte)'3', (byte)'!' }
                        }
                    }
                },
                CommitMessages = new Dictionary<int, RecoveryMessage.CommitPayloadCompact>()
            };

            // msg.TransactionHashes = null;
            // msg.Nonce = 0;
            // msg.NextConsensus = null;
            // msg.MinerTransaction = (MinerTransaction) null;
            msg.PrepareRequestMessage.Should().Be(null);

            var copiedMsg = TestUtils.CopyMsgBySerialization(msg, new RecoveryMessage()); ;

            copiedMsg.ChangeViewMessages.Should().BeEquivalentTo(msg.ChangeViewMessages);
            copiedMsg.PreparationHash.Should().Be(msg.PreparationHash);
            copiedMsg.PreparationMessages.Should().BeEquivalentTo(msg.PreparationMessages);
            copiedMsg.CommitMessages.Count.Should().Be(0);
        }

        [TestMethod]
        public void TestSerializeAndDeserializeRecoveryMessageWithChangeViewsAndPrepareRequest()
        {
            Transaction[] txs = new Transaction[5];
            for (int i = 0; i < txs.Length; i++)
                txs[i] = TestUtils.CreateRandomHashTransaction();
            var msg = new RecoveryMessage
            {
                ChangeViewMessages = new Dictionary<int, RecoveryMessage.ChangeViewPayloadCompact>()
                {
                    {
                        0,
                        new RecoveryMessage.ChangeViewPayloadCompact
                        {
                            ValidatorIndex = 0,
                            OriginalViewNumber = 9,
                            Timestamp = 6,
                            InvocationScript = new[] { (byte)'A' }
                        }
                    },
                    {
                        1,
                        new RecoveryMessage.ChangeViewPayloadCompact
                        {
                            ValidatorIndex = 1,
                            OriginalViewNumber = 7,
                            Timestamp = 5,
                            InvocationScript = new[] { (byte)'B' }
                        }
                    },
                    {
                        3,
                        new RecoveryMessage.ChangeViewPayloadCompact
                        {
                            ValidatorIndex = 3,
                            OriginalViewNumber = 5,
                            Timestamp = 3,
                            InvocationScript = new[] { (byte)'C' }
                        }
                    },
                    {
                        6,
                        new RecoveryMessage.ChangeViewPayloadCompact
                        {
                            ValidatorIndex = 6,
                            OriginalViewNumber = 2,
                            Timestamp = 1,
                            InvocationScript = new[] { (byte)'D' }
                        }
                    }
                },
                PrepareRequestMessage = new PrepareRequest
                {
                    TransactionHashes = txs.Select(p => p.Hash).ToArray()
                },
                PreparationHash = new UInt256(Crypto.Default.Hash256(new[] { (byte)'a' })),
                PreparationMessages = new Dictionary<int, RecoveryMessage.PreparationPayloadCompact>()
                {
                    {
                        0,
                        new RecoveryMessage.PreparationPayloadCompact
                        {
                            ValidatorIndex = 0,
                            InvocationScript = new[] { (byte)'t', (byte)'e' }
                        }
                    },
                    {
                        1,
                        new RecoveryMessage.PreparationPayloadCompact
                        {
                            ValidatorIndex = 1,
                            InvocationScript = new[] { (byte)'s', (byte)'t' }
                        }
                    },
                    {
                        3,
                        new RecoveryMessage.PreparationPayloadCompact
                        {
                            ValidatorIndex = 3,
                            InvocationScript = new[] { (byte)'1', (byte)'2' }
                        }
                    }
                },
                CommitMessages = new Dictionary<int, RecoveryMessage.CommitPayloadCompact>()
            };

            var copiedMsg = TestUtils.CopyMsgBySerialization(msg, new RecoveryMessage()); ;

            copiedMsg.ChangeViewMessages.Should().BeEquivalentTo(msg.ChangeViewMessages);
            copiedMsg.PrepareRequestMessage.Should().BeEquivalentTo(msg.PrepareRequestMessage);
            copiedMsg.PreparationHash.Should().Be(null);
            copiedMsg.PreparationMessages.Should().BeEquivalentTo(msg.PreparationMessages);
            copiedMsg.CommitMessages.Count.Should().Be(0);
        }

        [TestMethod]
        public void TestSerializeAndDeserializeRecoveryMessageWithoutChangeViewsWithoutCommits()
        {
            Transaction[] txs = new Transaction[5];
            for (int i = 0; i < txs.Length; i++)
                txs[i] = TestUtils.CreateRandomHashTransaction();
            var msg = new RecoveryMessage
            {
                ChangeViewMessages = new Dictionary<int, RecoveryMessage.ChangeViewPayloadCompact>(),
                PrepareRequestMessage = new PrepareRequest
                {
                    TransactionHashes = txs.Select(p => p.Hash).ToArray()
                },
                PreparationMessages = new Dictionary<int, RecoveryMessage.PreparationPayloadCompact>()
                {
                    {
                        0,
                        new RecoveryMessage.PreparationPayloadCompact
                        {
                            ValidatorIndex = 0,
                            InvocationScript = new[] { (byte)'t', (byte)'e' }
                        }
                    },
                    {
                        1,
                        new RecoveryMessage.PreparationPayloadCompact
                        {
                            ValidatorIndex = 1,
                            InvocationScript = new[] { (byte)'s', (byte)'t' }
                        }
                    },
                    {
                        3,
                        new RecoveryMessage.PreparationPayloadCompact
                        {
                            ValidatorIndex = 3,
                            InvocationScript = new[] { (byte)'1', (byte)'2' }
                        }
                    },
                    {
                        6,
                        new RecoveryMessage.PreparationPayloadCompact
                        {
                            ValidatorIndex = 6,
                            InvocationScript = new[] { (byte)'3', (byte)'!' }
                        }
                    }
                },
                CommitMessages = new Dictionary<int, RecoveryMessage.CommitPayloadCompact>()
            };

            var copiedMsg = TestUtils.CopyMsgBySerialization(msg, new RecoveryMessage()); ;

            copiedMsg.ChangeViewMessages.Count.Should().Be(0);
            copiedMsg.PrepareRequestMessage.Should().BeEquivalentTo(msg.PrepareRequestMessage);
            copiedMsg.PreparationHash.Should().Be(null);
            copiedMsg.PreparationMessages.Should().BeEquivalentTo(msg.PreparationMessages);
            copiedMsg.CommitMessages.Count.Should().Be(0);
        }

        [TestMethod]
        public void TestSerializeAndDeserializeRecoveryMessageWithoutChangeViewsWithCommits()
        {
            Transaction[] txs = new Transaction[5];
            for (int i = 0; i < txs.Length; i++)
                txs[i] = TestUtils.CreateRandomHashTransaction();
            var msg = new RecoveryMessage
            {
                ChangeViewMessages = new Dictionary<int, RecoveryMessage.ChangeViewPayloadCompact>(),
                PrepareRequestMessage = new PrepareRequest
                {
                    TransactionHashes = txs.Select(p => p.Hash).ToArray()
                },
                PreparationMessages = new Dictionary<int, RecoveryMessage.PreparationPayloadCompact>()
                {
                    {
                        0,
                        new RecoveryMessage.PreparationPayloadCompact
                        {
                            ValidatorIndex = 0,
                            InvocationScript = new[] { (byte)'t', (byte)'e' }
                        }
                    },
                    {
                        1,
                        new RecoveryMessage.PreparationPayloadCompact
                        {
                            ValidatorIndex = 1,
                            InvocationScript = new[] { (byte)'s', (byte)'t' }
                        }
                    },
                    {
                        3,
                        new RecoveryMessage.PreparationPayloadCompact
                        {
                            ValidatorIndex = 3,
                            InvocationScript = new[] { (byte)'1', (byte)'2' }
                        }
                    },
                    {
                        6,
                        new RecoveryMessage.PreparationPayloadCompact
                        {
                            ValidatorIndex = 6,
                            InvocationScript = new[] { (byte)'3', (byte)'!' }
                        }
                    }
                },
                CommitMessages = new Dictionary<int, RecoveryMessage.CommitPayloadCompact>
                {
                    {
                        1,
                        new RecoveryMessage.CommitPayloadCompact
                        {
                            ValidatorIndex = 1,
                            Signature = new byte[64] { (byte)'1', (byte)'2', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                            InvocationScript = new[] { (byte)'1', (byte)'2' }
                        }
                    },
                    {
                        6,
                        new RecoveryMessage.CommitPayloadCompact
                        {
                            ValidatorIndex = 6,
                            Signature = new byte[64] { (byte)'3', (byte)'D', (byte)'R', (byte)'I', (byte)'N', (byte)'K', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                            InvocationScript = new[] { (byte)'6', (byte)'7' }
                        }
                    }
                }
            };

            var copiedMsg = TestUtils.CopyMsgBySerialization(msg, new RecoveryMessage()); ;

            copiedMsg.ChangeViewMessages.Count.Should().Be(0);
            copiedMsg.PrepareRequestMessage.Should().BeEquivalentTo(msg.PrepareRequestMessage);
            copiedMsg.PreparationHash.Should().Be(null);
            copiedMsg.PreparationMessages.Should().BeEquivalentTo(msg.PreparationMessages);
            copiedMsg.CommitMessages.Should().BeEquivalentTo(msg.CommitMessages);
        }

        private static ConsensusPayload MakeSignedPayload(ConsensusContext context, ConsensusMessage message, ushort validatorIndex, byte[] witnessInvocationScript)
        {
            return new ConsensusPayload
            {
                Version = context.Block.Version,
                PrevHash = context.Block.PrevHash,
                BlockIndex = context.Block.Index,
                ValidatorIndex = validatorIndex,
                ConsensusMessage = message,
                Witness = new Witness
                {
                    InvocationScript = witnessInvocationScript,
                    VerificationScript = Contract.CreateSignatureRedeemScript(context.Validators[validatorIndex])
                }
            };
        }
    }
}
