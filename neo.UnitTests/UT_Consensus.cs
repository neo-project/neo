using Akka.TestKit;
using Akka.TestKit.Xunit2;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Consensus;
using Neo.Cryptography;
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
            var mockWallet = new Mock<Wallet>();
            mockWallet.Setup(p => p.GetAccount(It.IsAny<UInt160>())).Returns<UInt160>(p => new TestWalletAccount(p));
            ConsensusContext context = new ConsensusContext(mockWallet.Object, TestBlockchain.GetStore());

            int timeIndex = 0;
            var timeValues = new[] {
              //new DateTime(1968, 06, 01, 0, 0, 15, DateTimeKind.Utc), // For tests here
              new DateTime(1980, 06, 01, 0, 0, 1, 001, DateTimeKind.Utc),  // For receiving block
              new DateTime(1980, 06, 01, 0, 0, (int) Blockchain.MillisecondsPerBlock / 1000, 100, DateTimeKind.Utc), // For Initialize
              new DateTime(1980, 06, 01, 0, 0, 15, 001, DateTimeKind.Utc), // unused
              new DateTime(1980, 06, 01, 0, 0, 15, 001, DateTimeKind.Utc)  // unused
            };
            //TimeProvider.Current.UtcNow.ToTimestamp().Should().Be(4244941711); //1968-06-01 00:00:15

            Console.WriteLine($"time 0: {timeValues[0].ToString()} 1: {timeValues[1].ToString()} 2: {timeValues[2].ToString()} 3: {timeValues[3].ToString()}");

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
            TestUtils.SetupHeaderWithValues(header, UInt256.Zero, out UInt256 merkRootVal, out UInt160 val160, out ulong timestampVal, out uint indexVal, out Witness scriptVal);
            header.Size.Should().Be(105);

            Console.WriteLine($"header {header} hash {header.Hash} timestamp {timestampVal}");

            timestampVal.Should().Be(328665601001);    // GMT: Sunday, June 1, 1980 12:00:01.001 AM
                                                  // check basic ConsensusContext
                                                  //mockConsensusContext.Object.block_received_time.ToTimestamp().Should().Be(4244941697); //1968-06-01 00:00:01

            // ============================================================================
            //                      creating ConsensusService actor
            // ============================================================================

            TestActorRef<ConsensusService> actorConsensus = ActorOfAsTestActorRef<ConsensusService>(
                                     Akka.Actor.Props.Create(() => (ConsensusService)Activator.CreateInstance(typeof(ConsensusService), BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { subscriber, subscriber, context }, null))
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

            Console.WriteLine("OnTimer should expire!");
            Console.WriteLine("Waiting for subscriber message!");
            // Timer should expire in one second (block_received_time at :01, initialized at :02)

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
                    ECPoint.Parse("02486fd15702c4490a26703112a5cc1d0923fd697a33406bd5a1c00e0013b09a70", Cryptography.ECC.ECCurve.Secp256r1),
                    ECPoint.Parse("024c7b7fb6c310fccf1ba33b082519d82964ea93868d676662d4a59ad548df0e7d", Cryptography.ECC.ECCurve.Secp256r1),
                    ECPoint.Parse("02aaec38470f6aad0042c6e877cfd8087d2676b0f516fddd362801b9bd3936399e", Cryptography.ECC.ECCurve.Secp256r1),
                    ECPoint.Parse("02ca0e27697b9c248f6f16e085fd0061e26f44da85b58ee835c110caa5ec3ba554", Cryptography.ECC.ECCurve.Secp256r1),
                    ECPoint.Parse("02df48f60e8f3e01c48ff40b9b7f1310d7a8b2a193188befe1c2e3df740e895093", Cryptography.ECC.ECCurve.Secp256r1),
                    ECPoint.Parse("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c", Cryptography.ECC.ECCurve.Secp256r1),
                    ECPoint.Parse("03b8d9d5771d8f513aa0869b9cc8d50986403b78c6da36890638c3d46a5adce04a", Cryptography.ECC.ECCurve.Secp256r1)
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
            copiedContext.Validators.ShouldAllBeEquivalentTo(consensusContext.Validators);
            copiedContext.MyIndex.Should().Be(consensusContext.MyIndex);
            copiedContext.Block.ConsensusData.PrimaryIndex.Should().Be(consensusContext.Block.ConsensusData.PrimaryIndex);
            copiedContext.Block.Timestamp.Should().Be(consensusContext.Block.Timestamp);
            copiedContext.Block.NextConsensus.Should().Be(consensusContext.Block.NextConsensus);
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
            copiedMsg.PrepareRequestMessage.ShouldBeEquivalentTo(msg.PrepareRequestMessage);
            copiedMsg.PreparationHash.Should().Be(null);
            copiedMsg.PreparationMessages.ShouldAllBeEquivalentTo(msg.PreparationMessages);
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
            copiedMsg.PrepareRequestMessage.ShouldBeEquivalentTo(msg.PrepareRequestMessage);
            copiedMsg.PreparationHash.Should().Be(null);
            copiedMsg.PreparationMessages.ShouldAllBeEquivalentTo(msg.PreparationMessages);
            copiedMsg.CommitMessages.ShouldAllBeEquivalentTo(msg.CommitMessages);
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
