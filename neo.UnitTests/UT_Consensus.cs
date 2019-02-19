using Akka.TestKit;
using Akka.TestKit.Xunit2;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Consensus;
using Neo.Cryptography;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using ECPoint = Neo.Cryptography.ECC.ECPoint;

namespace Neo.UnitTests
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
        public void ConsensusService_Primary_Sends_PrepareRequest_After_OnStart()
        {
            TestProbe subscriber = CreateTestProbe();

            var mockConsensusContext = new Mock<IConsensusContext>();
            var mockStore = new Mock<Store>();

            // context.Reset(): do nothing
            //mockConsensusContext.Setup(mr => mr.Reset()).Verifiable(); // void
            mockConsensusContext.SetupGet(mr => mr.MyIndex).Returns(2); // MyIndex == 2
            mockConsensusContext.SetupGet(mr => mr.BlockIndex).Returns(2);
            mockConsensusContext.SetupGet(mr => mr.PrimaryIndex).Returns(2);
            mockConsensusContext.SetupGet(mr => mr.ViewNumber).Returns(0);
            mockConsensusContext.SetupProperty(mr => mr.Nonce);
            mockConsensusContext.SetupProperty(mr => mr.NextConsensus);
            mockConsensusContext.Object.NextConsensus = UInt160.Zero;
            mockConsensusContext.Setup(mr => mr.GetPrimaryIndex(It.IsAny<byte>())).Returns(2);
            mockConsensusContext.SetupProperty(mr => mr.State);  // allows get and set to update mock state on Initialize method
            mockConsensusContext.Object.State = ConsensusState.Initial;

            int timeIndex = 0;
            var timeValues = new[] {
              new DateTime(1968, 06, 01, 0, 0, 15, DateTimeKind.Utc), // For tests here
              new DateTime(1968, 06, 01, 0, 0, 1, DateTimeKind.Utc),  // For receiving block
              new DateTime(1968, 06, 01, 0, 0, 15, DateTimeKind.Utc), // For Initialize
              new DateTime(1968, 06, 01, 0, 0, 15, DateTimeKind.Utc), // unused
              new DateTime(1968, 06, 01, 0, 0, 15, DateTimeKind.Utc)  // unused
          };

            Console.WriteLine($"time 0: {timeValues[0].ToString()} 1: {timeValues[1].ToString()} 2: {timeValues[2].ToString()} 3: {timeValues[3].ToString()}");

            //mockConsensusContext.Object.block_received_time = new DateTime(1968, 06, 01, 0, 0, 1, DateTimeKind.Utc);
            //mockConsensusContext.Setup(mr => mr.GetUtcNow()).Returns(new DateTime(1968, 06, 01, 0, 0, 15, DateTimeKind.Utc));

            var timeMock = new Mock<TimeProvider>();
            timeMock.SetupGet(tp => tp.UtcNow).Returns(() => timeValues[timeIndex])
                                              .Callback(() => timeIndex++);
            //new DateTime(1968, 06, 01, 0, 0, 15, DateTimeKind.Utc));
            TimeProvider.Current = timeMock.Object;

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
            TestUtils.SetupHeaderWithValues(header, UInt256.Zero, out UInt256 merkRootVal, out UInt160 val160, out uint timestampVal, out uint indexVal, out ulong consensusDataVal, out Witness scriptVal);
            header.Size.Should().Be(109);

            Console.WriteLine($"header {header} hash {header.Hash} timstamp {timestampVal}");

            timestampVal.Should().Be(4244941696); //1968-06-01 00:00:00
            TimeProvider.Current.UtcNow.ToTimestamp().Should().Be(4244941711); //1968-06-01 00:00:15
                                                                               // check basic ConsensusContext
            mockConsensusContext.Object.MyIndex.Should().Be(2);
            //mockConsensusContext.Object.block_received_time.ToTimestamp().Should().Be(4244941697); //1968-06-01 00:00:01

            MinerTransaction minerTx = new MinerTransaction
            {
                Attributes = new TransactionAttribute[0],
                Inputs = new CoinReference[0],
                Outputs = new TransactionOutput[0],
                Witnesses = new Witness[0],
                Nonce = 42
            };

            PrepareRequest prep = new PrepareRequest
            {
                Nonce = mockConsensusContext.Object.Nonce,
                NextConsensus = mockConsensusContext.Object.NextConsensus,
                TransactionHashes = new UInt256[0],
                MinerTransaction = minerTx //(MinerTransaction)Transactions[TransactionHashes[0]],
            };

            ConsensusPayload prepPayload = new ConsensusPayload
            {
                Version = 0,
                PrevHash = mockConsensusContext.Object.PrevHash,
                BlockIndex = mockConsensusContext.Object.BlockIndex,
                ValidatorIndex = (ushort)mockConsensusContext.Object.MyIndex,
                ConsensusMessage = prep
            };

            mockConsensusContext.Setup(mr => mr.MakePrepareRequest()).Returns(prepPayload);

            // ============================================================================
            //                      creating ConsensusService actor
            // ============================================================================

            TestActorRef<ConsensusService> actorConsensus = ActorOfAsTestActorRef<ConsensusService>(
                                     Akka.Actor.Props.Create(() => new ConsensusService(subscriber, subscriber, mockStore.Object, mockConsensusContext.Object))
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
                    ConsensusData = header.ConsensusData,
                    NextConsensus = header.NextConsensus
                }
            });

            // OnPersist will not launch timer, we need OnStart

            Console.WriteLine("will start consensus!");
            actorConsensus.Tell(new ConsensusService.Start());

            Console.WriteLine("OnTimer should expire!");
            Console.WriteLine("Waiting for subscriber message!");

            var answer = subscriber.ExpectMsg<LocalNode.SendDirectly>();
            Console.WriteLine($"MESSAGE 1: {answer}");
            //var answer2 = subscriber.ExpectMsg<LocalNode.SendDirectly>(); // expects to fail!

            // ============================================================================
            //                      finalize ConsensusService actor
            // ============================================================================

            //Thread.Sleep(4000);
            Sys.Stop(actorConsensus);
            TimeProvider.ResetToDefault();

            Assert.AreEqual(1, 1);
        }

        [TestMethod]
        public void TestSerializeAndDeserializeConsensusContext()
        {
            var consensusContext = new ConsensusContext(null);
            consensusContext.State = ConsensusState.CommitSent;
            consensusContext.PrevHash = UInt256.Parse("3333333377777777333333337777777733333333777777773333333377777777");
            consensusContext.BlockIndex = 1337;
            consensusContext.ViewNumber = 2;
            consensusContext.Validators = new ECPoint[7]
            {
                TestUtils.StandbyValidators[0],
                ECPoint.Multiply(TestUtils.StandbyValidators[0], new BigInteger(2)),
                ECPoint.Multiply(TestUtils.StandbyValidators[0], new BigInteger(3)),
                ECPoint.Multiply(TestUtils.StandbyValidators[0], new BigInteger(4)),
                ECPoint.Multiply(TestUtils.StandbyValidators[0], new BigInteger(5)),
                ECPoint.Multiply(TestUtils.StandbyValidators[0], new BigInteger(6)),
                ECPoint.Multiply(TestUtils.StandbyValidators[0], new BigInteger(7)),
            };
            consensusContext.MyIndex = 3;
            consensusContext.PrimaryIndex = 6;
            consensusContext.Timestamp = 4244941711;
            consensusContext.Nonce = UInt64.MaxValue;
            consensusContext.NextConsensus = UInt160.Parse("5555AAAA5555AAAA5555AAAA5555AAAA5555AAAA");
            var testTx1 = TestUtils.CreateRandomHashInvocationMockTransaction().Object;
            var testTx2 = TestUtils.CreateRandomHashInvocationMockTransaction().Object;

            int txCountToInlcude = 256;
            consensusContext.TransactionHashes = new UInt256[txCountToInlcude];

            Transaction[] txs = new Transaction[txCountToInlcude];
            txs[0] = TestUtils.CreateRandomMockMinerTransaction().Object;
            consensusContext.TransactionHashes[0] = txs[0].Hash;
            for (int i = 1; i < txCountToInlcude; i++)
            {
                txs[i] = TestUtils.CreateRandomHashInvocationMockTransaction().Object;
                consensusContext.TransactionHashes[i] = txs[i].Hash;
            }
            // consensusContext.TransactionHashes = new UInt256[2] {testTx1.Hash, testTx2.Hash};
            consensusContext.Transactions = txs.ToDictionary(p => p.Hash);

            consensusContext.PreparationPayloads = new ConsensusPayload[consensusContext.Validators.Length];
            var prepareRequestMessage = new PrepareRequest
            {
                Nonce = consensusContext.Nonce,
                NextConsensus = consensusContext.NextConsensus,
                TransactionHashes = consensusContext.TransactionHashes,
                MinerTransaction = (MinerTransaction)consensusContext.Transactions[consensusContext.TransactionHashes[0]],
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

            consensusContext.Timestamp = TimeProvider.Current.UtcNow.ToTimestamp();

            consensusContext.ChangeViewPayloads = new ConsensusPayload[consensusContext.Validators.Length];
            consensusContext.ChangeViewPayloads[0] = MakeSignedPayload(consensusContext, new ChangeView { ViewNumber = 1, NewViewNumber = 2, Timestamp = 6 }, 0, new[] { (byte)'A' });
            consensusContext.ChangeViewPayloads[1] = MakeSignedPayload(consensusContext, new ChangeView { ViewNumber = 1, NewViewNumber = 2, Timestamp = 5 }, 1, new[] { (byte)'B' });
            consensusContext.ChangeViewPayloads[2] = null;
            consensusContext.ChangeViewPayloads[3] = MakeSignedPayload(consensusContext, new ChangeView { ViewNumber = 1, NewViewNumber = 2, Timestamp = uint.MaxValue }, 3, new[] { (byte)'C' });
            consensusContext.ChangeViewPayloads[4] = null;
            consensusContext.ChangeViewPayloads[5] = null;
            consensusContext.ChangeViewPayloads[6] = MakeSignedPayload(consensusContext, new ChangeView { ViewNumber = 1, NewViewNumber = 2, Timestamp = 1 }, 6, new[] { (byte)'D' });

            consensusContext.LastChangeViewPayloads = new ConsensusPayload[consensusContext.Validators.Length];

            var copiedContext = TestUtils.CopyMsgBySerialization(consensusContext, new ConsensusContext(null));

            copiedContext.State.Should().Be(consensusContext.State);
            copiedContext.PrevHash.Should().Be(consensusContext.PrevHash);
            copiedContext.BlockIndex.Should().Be(consensusContext.BlockIndex);
            copiedContext.ViewNumber.Should().Be(consensusContext.ViewNumber);
            copiedContext.Validators.ShouldAllBeEquivalentTo(consensusContext.Validators);
            copiedContext.MyIndex.Should().Be(consensusContext.MyIndex);
            copiedContext.PrimaryIndex.Should().Be(consensusContext.PrimaryIndex);
            copiedContext.Timestamp.Should().Be(consensusContext.Timestamp);
            copiedContext.Nonce.Should().Be(consensusContext.Nonce);
            copiedContext.NextConsensus.Should().Be(consensusContext.NextConsensus);
            copiedContext.TransactionHashes.ShouldAllBeEquivalentTo(consensusContext.TransactionHashes);
            copiedContext.Transactions.ShouldAllBeEquivalentTo(consensusContext.Transactions);
            copiedContext.Transactions.Values.ShouldAllBeEquivalentTo(consensusContext.Transactions.Values);
            copiedContext.PreparationPayloads.ShouldAllBeEquivalentTo(consensusContext.PreparationPayloads);
            copiedContext.CommitPayloads.ShouldAllBeEquivalentTo(consensusContext.CommitPayloads);
            copiedContext.ChangeViewPayloads.ShouldAllBeEquivalentTo(consensusContext.ChangeViewPayloads);
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

            copiedMsg.ChangeViewMessages.ShouldAllBeEquivalentTo(msg.ChangeViewMessages);
            copiedMsg.PreparationHash.Should().Be(msg.PreparationHash);
            copiedMsg.PreparationMessages.ShouldAllBeEquivalentTo(msg.PreparationMessages);
            copiedMsg.CommitMessages.Count.Should().Be(0);
        }

        [TestMethod]
        public void TestSerializeAndDeserializeRecoveryMessageWithChangeViewsAndPrepareRequest()
        {
            Transaction[] txs = new Transaction[5];
            txs[0] = TestUtils.CreateRandomMockMinerTransaction().Object;
            for (int i = 1; i < txs.Length; i++)
                txs[i] = TestUtils.CreateRandomHashInvocationMockTransaction().Object;
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
                    TransactionHashes = txs.Select(p => p.Hash).ToArray(),
                    Nonce = ulong.MaxValue,
                    NextConsensus = UInt160.Parse("5555AAAA5555AAAA5555AAAA5555AAAA5555AAAA"),
                    MinerTransaction = (MinerTransaction)txs[0]
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

            copiedMsg.ChangeViewMessages.ShouldAllBeEquivalentTo(msg.ChangeViewMessages);
            copiedMsg.PrepareRequestMessage.ShouldBeEquivalentTo(msg.PrepareRequestMessage);
            copiedMsg.PreparationHash.Should().Be(null);
            copiedMsg.PreparationMessages.ShouldAllBeEquivalentTo(msg.PreparationMessages);
            copiedMsg.CommitMessages.Count.Should().Be(0);
        }

        [TestMethod]
        public void TestSerializeAndDeserializeRecoveryMessageWithoutChangeViewsWithoutCommits()
        {
            Transaction[] txs = new Transaction[5];
            txs[0] = TestUtils.CreateRandomMockMinerTransaction().Object;
            for (int i = 1; i < txs.Length; i++)
                txs[i] = TestUtils.CreateRandomHashInvocationMockTransaction().Object;
            var msg = new RecoveryMessage
            {
                ChangeViewMessages = new Dictionary<int, RecoveryMessage.ChangeViewPayloadCompact>(),
                PrepareRequestMessage = new PrepareRequest
                {
                    TransactionHashes = txs.Select(p => p.Hash).ToArray(),
                    Nonce = ulong.MaxValue,
                    NextConsensus = UInt160.Parse("5555AAAA5555AAAA5555AAAA5555AAAA5555AAAA"),
                    MinerTransaction = (MinerTransaction)txs[0]
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
            copiedMsg.PrepareRequestMessage.ShouldBeEquivalentTo(msg.PrepareRequestMessage);
            copiedMsg.PreparationHash.Should().Be(null);
            copiedMsg.PreparationMessages.ShouldAllBeEquivalentTo(msg.PreparationMessages);
            copiedMsg.CommitMessages.Count.Should().Be(0);
        }

        [TestMethod]
        public void TestSerializeAndDeserializeRecoveryMessageWithoutChangeViewsWithCommits()
        {
            Transaction[] txs = new Transaction[5];
            txs[0] = TestUtils.CreateRandomMockMinerTransaction().Object;
            for (int i = 1; i < txs.Length; i++)
                txs[i] = TestUtils.CreateRandomHashInvocationMockTransaction().Object;
            var msg = new RecoveryMessage
            {
                ChangeViewMessages = new Dictionary<int, RecoveryMessage.ChangeViewPayloadCompact>(),
                PrepareRequestMessage = new PrepareRequest
                {
                    TransactionHashes = txs.Select(p => p.Hash).ToArray(),
                    Nonce = ulong.MaxValue,
                    NextConsensus = UInt160.Parse("5555AAAA5555AAAA5555AAAA5555AAAA5555AAAA"),
                    MinerTransaction = (MinerTransaction)txs[0]
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
            copiedMsg.PrepareRequestMessage.ShouldBeEquivalentTo(msg.PrepareRequestMessage);
            copiedMsg.PreparationHash.Should().Be(null);
            copiedMsg.PreparationMessages.ShouldAllBeEquivalentTo(msg.PreparationMessages);
            copiedMsg.CommitMessages.ShouldAllBeEquivalentTo(msg.CommitMessages);
        }

        private static ConsensusPayload MakeSignedPayload(IConsensusContext context, ConsensusMessage message, ushort validatorIndex, byte[] witnessInvocationScript)
        {
            return new ConsensusPayload
            {
                Version = ConsensusContext.Version,
                PrevHash = context.PrevHash,
                BlockIndex = context.BlockIndex,
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
