using Akka.TestKit;
using Akka.TestKit.Xunit2;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Consensus;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using Neo.Cryptography;
using Neo.Persistence;
using Remotion.Linq.Clauses;
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

            ConsensusMessage mprep = prep;
            byte[] prepData = mprep.ToArray();

            ConsensusPayload prepPayload = new ConsensusPayload
            {
                Version = 0,
                PrevHash = mockConsensusContext.Object.PrevHash,
                BlockIndex = mockConsensusContext.Object.BlockIndex,
                ValidatorIndex = (ushort)mockConsensusContext.Object.MyIndex,
                Timestamp = mockConsensusContext.Object.Timestamp,
                Data = prepData
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
            for (int i = 0; i < txCountToInlcude; i++)
            {
                txs[i] = TestUtils.CreateRandomHashInvocationMockTransaction().Object;
                consensusContext.TransactionHashes[i] = txs[i].Hash;
            }
            // consensusContext.TransactionHashes = new UInt256[2] {testTx1.Hash, testTx2.Hash};
            consensusContext.Transactions = txs.ToDictionary(p => p.Hash);

            consensusContext.Preparations = new [] {null, null, null, consensusContext.PrevHash, null, null, null };
            consensusContext.PreparationWitnessInvocationScripts = new byte[consensusContext.Validators.Length][];
            consensusContext.PreparationWitnessInvocationScripts[0] = new [] {(byte)'t', (byte)'e'};
            consensusContext.PreparationWitnessInvocationScripts[1] = new [] {(byte)'s', (byte)'t'};
            consensusContext.PreparationWitnessInvocationScripts[2] = null;
            consensusContext.PreparationWitnessInvocationScripts[3] = new [] {(byte)'1', (byte)'2'};
            consensusContext.PreparationWitnessInvocationScripts[4] = null;
            consensusContext.PreparationWitnessInvocationScripts[5] = null;
            consensusContext.PreparationWitnessInvocationScripts[6] = new [] {(byte)'3', (byte)'!'};
            consensusContext.PreparationTimestamps = new uint[7];
            consensusContext.PreparationTimestamps[0] = 0;
            consensusContext.PreparationTimestamps[1] = 1;
            consensusContext.PreparationTimestamps[2] = 2;
            consensusContext.PreparationTimestamps[3] = uint.MaxValue;
            consensusContext.PreparationTimestamps[4] = 4;
            consensusContext.PreparationTimestamps[5] = 5;
            consensusContext.PreparationTimestamps[6] = 6;


            consensusContext.Commits = new byte[consensusContext.Validators.Length][];
            using (SHA256 sha256 = SHA256.Create())
            {
                consensusContext.Commits[3] = sha256.ComputeHash(testTx1.Hash.ToArray());
                consensusContext.Commits[6] = sha256.ComputeHash(testTx2.Hash.ToArray());
            }

            consensusContext.ExpectedView = new byte[consensusContext.Validators.Length];
            consensusContext.ExpectedView[0] = 2;
            consensusContext.ExpectedView[1] = 2;
            consensusContext.ExpectedView[2] = 1;
            consensusContext.ExpectedView[3] = 2;
            consensusContext.ExpectedView[4] = 1;
            consensusContext.ExpectedView[5] = 1;
            consensusContext.ExpectedView[6] = 2;

            consensusContext.ChangeViewWitnessInvocationScripts = new byte[consensusContext.Validators.Length][];
            consensusContext.ChangeViewWitnessInvocationScripts[0] = new [] {(byte) 'A'};
            consensusContext.ChangeViewWitnessInvocationScripts[1] = new [] {(byte) 'B'};
            consensusContext.ChangeViewWitnessInvocationScripts[2] = null;
            consensusContext.ChangeViewWitnessInvocationScripts[3] = new [] {(byte) 'C'};
            consensusContext.ChangeViewWitnessInvocationScripts[4] = null;
            consensusContext.ChangeViewWitnessInvocationScripts[5] = null;
            consensusContext.ChangeViewWitnessInvocationScripts[6] = new [] {(byte) 'D'};
            consensusContext.ChangeViewTimestamps = new uint[7];
            consensusContext.ChangeViewTimestamps[0] = 6;
            consensusContext.ChangeViewTimestamps[1] = 5;
            consensusContext.ChangeViewTimestamps[2] = 4;
            consensusContext.ChangeViewTimestamps[3] = 3;
            consensusContext.ChangeViewTimestamps[4] = uint.MaxValue;
            consensusContext.ChangeViewTimestamps[5] = 2;
            consensusContext.ChangeViewTimestamps[6] = 1;
            consensusContext.OriginalChangeViewNumbers = new byte[7];
            consensusContext.OriginalChangeViewNumbers[0] = 5;
            consensusContext.OriginalChangeViewNumbers[1] = 5;
            consensusContext.OriginalChangeViewNumbers[2] = 6;
            consensusContext.OriginalChangeViewNumbers[3] = 6;
            consensusContext.OriginalChangeViewNumbers[4] = 5;
            consensusContext.OriginalChangeViewNumbers[5] = 0;
            consensusContext.OriginalChangeViewNumbers[6] = 5;


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
            copiedContext.PreparationWitnessInvocationScripts.ShouldAllBeEquivalentTo(consensusContext.PreparationWitnessInvocationScripts);
            copiedContext.PreparationTimestamps.ShouldAllBeEquivalentTo(consensusContext.PreparationTimestamps);
            copiedContext.Preparations.ShouldAllBeEquivalentTo(consensusContext.Preparations);
            copiedContext.Commits.ShouldAllBeEquivalentTo(consensusContext.Commits);
            copiedContext.ExpectedView.ShouldAllBeEquivalentTo(consensusContext.ExpectedView);
            copiedContext.ChangeViewWitnessInvocationScripts.ShouldAllBeEquivalentTo(consensusContext.ChangeViewWitnessInvocationScripts);
            copiedContext.ChangeViewTimestamps.ShouldAllBeEquivalentTo(consensusContext.ChangeViewTimestamps);
            copiedContext.OriginalChangeViewNumbers.ShouldAllBeEquivalentTo(consensusContext.OriginalChangeViewNumbers);
        }

        [TestMethod]
        public void TestSerializeAndDeserializeRecoveryMessageWithChangeViewsAndNoPrepareRequest()
        {
            var msg = new RecoveryMessage();
            msg.ChangeViewWitnessInvocationScripts = new byte[7][];
            msg.ChangeViewWitnessInvocationScripts[0] = new [] {(byte) 'A'};
            msg.ChangeViewWitnessInvocationScripts[1] = new [] {(byte) 'B'};
            msg.ChangeViewWitnessInvocationScripts[2] = null;
            msg.ChangeViewWitnessInvocationScripts[3] = new [] {(byte) 'C'};
            msg.ChangeViewWitnessInvocationScripts[4] = null;
            msg.ChangeViewWitnessInvocationScripts[5] = null;
            msg.ChangeViewWitnessInvocationScripts[6] = new [] {(byte) 'D'};
            msg.ChangeViewTimestamps = new uint[7];
            msg.ChangeViewTimestamps[0] = 6;
            msg.ChangeViewTimestamps[1] = 5;
            msg.ChangeViewTimestamps[2] = 4;
            msg.ChangeViewTimestamps[3] = 3;
            msg.ChangeViewTimestamps[4] = uint.MaxValue;
            msg.ChangeViewTimestamps[5] = 2;
            msg.ChangeViewTimestamps[6] = 1;
            msg.OriginalChangeViewNumbers = new byte[7];
            msg.OriginalChangeViewNumbers[0] = 9;
            msg.OriginalChangeViewNumbers[1] = 7;
            msg.OriginalChangeViewNumbers[2] = 6;
            msg.OriginalChangeViewNumbers[3] = 5;
            msg.OriginalChangeViewNumbers[4] = 4;
            msg.OriginalChangeViewNumbers[5] = 3;
            msg.OriginalChangeViewNumbers[6] = 2;

            // msg.TransactionHashes = null;
            // msg.Nonce = 0;
            // msg.NextConsensus = null;
            // msg.MinerTransaction = (MinerTransaction) null;
            msg.TransactionHashes.ShouldAllBeEquivalentTo(null);
            msg.Nonce.Should().Be(0);
            msg.NextConsensus.Should().Be(null);
            msg.MinerTransaction.Should().Be(null);
            msg.PreparationHash = new UInt256(Crypto.Default.Hash256(new [] {(byte) 'a'}));
            msg.PrepareWitnessInvocationScripts = new byte[7][];
            msg.PrepareWitnessInvocationScripts[0] = new [] {(byte)'t', (byte)'e'};
            msg.PrepareWitnessInvocationScripts[1] = null;
            msg.PrepareWitnessInvocationScripts[2] = null;
            msg.PrepareWitnessInvocationScripts[3] = new [] {(byte)'1', (byte)'2'};
            msg.PrepareWitnessInvocationScripts[4] = null;
            msg.PrepareWitnessInvocationScripts[5] = null;
            msg.PrepareWitnessInvocationScripts[6] = new [] {(byte)'3', (byte)'!'};
            msg.PrepareTimestamps = new uint[7];
            msg.PrepareTimestamps[0] = 0;
            msg.PrepareTimestamps[1] = 0;
            msg.PrepareTimestamps[2] = 2;
            msg.PrepareTimestamps[3] = uint.MaxValue;
            msg.PrepareTimestamps[4] = 4;
            msg.PrepareTimestamps[5] = 5;
            msg.PrepareTimestamps[6] = 6;

            var copiedMsg = TestUtils.CopyMsgBySerialization(msg, new RecoveryMessage());;

            copiedMsg.ChangeViewWitnessInvocationScripts.ShouldAllBeEquivalentTo(msg.ChangeViewWitnessInvocationScripts);
            copiedMsg.ChangeViewTimestamps.ShouldAllBeEquivalentTo(msg.ChangeViewTimestamps);
            copiedMsg.OriginalChangeViewNumbers.ShouldAllBeEquivalentTo(msg.OriginalChangeViewNumbers);
            copiedMsg.TransactionHashes.ShouldAllBeEquivalentTo(null);
            copiedMsg.Nonce.Should().Be(0);
            copiedMsg.NextConsensus.Should().Be(null);
            copiedMsg.MinerTransaction.Should().Be(null);
            copiedMsg.PreparationHash.Should().Be(msg.PreparationHash);
            copiedMsg.PrepareWitnessInvocationScripts.ShouldAllBeEquivalentTo(msg.PrepareWitnessInvocationScripts);
            copiedMsg.PrepareTimestamps.ShouldAllBeEquivalentTo(msg.PrepareTimestamps);
            (copiedMsg.CommitSignatures == null).Should().Be(true);
        }

        [TestMethod]
        public void TestSerializeAndDeserializeRecoveryMessageWithChangeViewsAndPrepareRequest()
        {
            var msg = new RecoveryMessage();
            msg.ChangeViewWitnessInvocationScripts = new byte[7][];
            msg.ChangeViewWitnessInvocationScripts[0] = new [] {(byte) 'A'};
            msg.ChangeViewWitnessInvocationScripts[1] = new [] {(byte) 'B'};
            msg.ChangeViewWitnessInvocationScripts[2] = null;
            msg.ChangeViewWitnessInvocationScripts[3] = new [] {(byte) 'C'};
            msg.ChangeViewWitnessInvocationScripts[4] = null;
            msg.ChangeViewWitnessInvocationScripts[5] = null;
            msg.ChangeViewWitnessInvocationScripts[6] = new [] {(byte) 'D'};
            msg.ChangeViewTimestamps = new uint[7];
            msg.ChangeViewTimestamps[0] = 6;
            msg.ChangeViewTimestamps[1] = 5;
            msg.ChangeViewTimestamps[2] = 4;
            msg.ChangeViewTimestamps[3] = 3;
            msg.ChangeViewTimestamps[4] = uint.MaxValue;
            msg.ChangeViewTimestamps[5] = 2;
            msg.ChangeViewTimestamps[6] = 1;
            msg.OriginalChangeViewNumbers = new byte[7];
            msg.OriginalChangeViewNumbers[0] = 9;
            msg.OriginalChangeViewNumbers[1] = 7;
            msg.OriginalChangeViewNumbers[2] = 6;
            msg.OriginalChangeViewNumbers[3] = 5;
            msg.OriginalChangeViewNumbers[4] = 4;
            msg.OriginalChangeViewNumbers[5] = 3;
            msg.OriginalChangeViewNumbers[6] = 2;
            int txCountToInlcude = 5;
            msg.TransactionHashes = new UInt256[txCountToInlcude];
            Transaction[] txs = new Transaction[txCountToInlcude];
            txs[0] = TestUtils.CreateRandomMockMinerTransaction().Object;
            msg.TransactionHashes[0] = txs[0].Hash;
            for (int i = 1; i < txCountToInlcude; i++)
            {
                txs[i] = TestUtils.CreateRandomHashInvocationMockTransaction().Object;
                msg.TransactionHashes[i] = txs[i].Hash;
            }
            msg.Nonce = UInt64.MaxValue;
            msg.NextConsensus = UInt160.Parse("5555AAAA5555AAAA5555AAAA5555AAAA5555AAAA");
            msg.MinerTransaction = (MinerTransaction) txs[0];
            msg.PrepareWitnessInvocationScripts = new byte[7][];
            msg.PrepareWitnessInvocationScripts[0] = new [] {(byte)'t', (byte)'e'};
            msg.PrepareWitnessInvocationScripts[1] = new [] {(byte)'s', (byte)'t'};
            msg.PrepareWitnessInvocationScripts[2] = null;
            msg.PrepareWitnessInvocationScripts[3] = new [] {(byte)'1', (byte)'2'};
            msg.PrepareWitnessInvocationScripts[4] = null;
            msg.PrepareWitnessInvocationScripts[5] = null;
            msg.PrepareWitnessInvocationScripts[6] = null;
            msg.PrepareTimestamps = new uint[7];
            msg.PrepareTimestamps[0] = 0;
            msg.PrepareTimestamps[1] = 1;
            msg.PrepareTimestamps[2] = 2;
            msg.PrepareTimestamps[3] = uint.MaxValue;
            msg.PrepareTimestamps[4] = 4;
            msg.PrepareTimestamps[5] = 5;
            msg.PrepareTimestamps[6] = 0;

            var copiedMsg = TestUtils.CopyMsgBySerialization(msg, new RecoveryMessage());;

            copiedMsg.ChangeViewWitnessInvocationScripts.ShouldAllBeEquivalentTo(msg.ChangeViewWitnessInvocationScripts);
            copiedMsg.ChangeViewTimestamps.ShouldAllBeEquivalentTo(msg.ChangeViewTimestamps);
            copiedMsg.TransactionHashes.ShouldAllBeEquivalentTo(msg.TransactionHashes);
            copiedMsg.Nonce.Should().Be(msg.Nonce);
            copiedMsg.NextConsensus.Should().Be(msg.NextConsensus);
            copiedMsg.MinerTransaction.Should().Be(msg.MinerTransaction);
            copiedMsg.PreparationHash.Should().Be(null);
            copiedMsg.PrepareWitnessInvocationScripts.ShouldAllBeEquivalentTo(msg.PrepareWitnessInvocationScripts);
            copiedMsg.PrepareTimestamps.ShouldAllBeEquivalentTo(msg.PrepareTimestamps);
            (copiedMsg.CommitSignatures == null).Should().Be(true);
        }

        [TestMethod]
        public void TestSerializeAndDeserializeRecoveryMessageWithoutChangeViewsWithoutCommits()
        {
            var msg = new RecoveryMessage();
            int txCountToInlcude = 5;
            msg.TransactionHashes = new UInt256[txCountToInlcude];
            Transaction[] txs = new Transaction[txCountToInlcude];
            txs[0] = TestUtils.CreateRandomMockMinerTransaction().Object;
            msg.TransactionHashes[0] = txs[0].Hash;
            for (int i = 1; i < txCountToInlcude; i++)
            {
                txs[i] = TestUtils.CreateRandomHashInvocationMockTransaction().Object;
                msg.TransactionHashes[i] = txs[i].Hash;
            }
            msg.Nonce = UInt64.MaxValue;
            msg.NextConsensus = UInt160.Parse("5555AAAA5555AAAA5555AAAA5555AAAA5555AAAA");
            msg.MinerTransaction = (MinerTransaction) txs[0];
            msg.PrepareWitnessInvocationScripts = new byte[7][];
            msg.PrepareWitnessInvocationScripts[0] = new [] {(byte)'t', (byte)'e'};
            msg.PrepareWitnessInvocationScripts[1] = new [] {(byte)'s', (byte)'t'};
            msg.PrepareWitnessInvocationScripts[2] = null;
            msg.PrepareWitnessInvocationScripts[3] = new [] {(byte)'1', (byte)'2'};
            msg.PrepareWitnessInvocationScripts[4] = null;
            msg.PrepareWitnessInvocationScripts[5] = null;
            msg.PrepareWitnessInvocationScripts[6] = new [] {(byte)'3', (byte)'!'};
            msg.PrepareTimestamps = new uint[7];
            msg.PrepareTimestamps[0] = 0;
            msg.PrepareTimestamps[1] = 1;
            msg.PrepareTimestamps[2] = 2;
            msg.PrepareTimestamps[3] = uint.MaxValue;
            msg.PrepareTimestamps[4] = 4;
            msg.PrepareTimestamps[5] = 5;
            msg.PrepareTimestamps[6] = 6;

            var copiedMsg = TestUtils.CopyMsgBySerialization(msg, new RecoveryMessage());;

            copiedMsg.ChangeViewWitnessInvocationScripts.ShouldAllBeEquivalentTo(null);
            copiedMsg.ChangeViewTimestamps.ShouldAllBeEquivalentTo(null);
            copiedMsg.TransactionHashes.ShouldAllBeEquivalentTo(msg.TransactionHashes);
            copiedMsg.Nonce.Should().Be(msg.Nonce);
            copiedMsg.NextConsensus.Should().Be(msg.NextConsensus);
            copiedMsg.MinerTransaction.Should().Be(msg.MinerTransaction);
            copiedMsg.PreparationHash.Should().Be(null);
            copiedMsg.PrepareWitnessInvocationScripts.ShouldAllBeEquivalentTo(msg.PrepareWitnessInvocationScripts);
            copiedMsg.PrepareTimestamps.ShouldAllBeEquivalentTo(msg.PrepareTimestamps);
            (copiedMsg.CommitSignatures == null).Should().Be(true);
        }

        [TestMethod]
        public void TestSerializeAndDeserializeRecoveryMessageWithoutChangeViewsWithCommits()
        {
            var msg = new RecoveryMessage();
            int txCountToInlcude = 5;
            msg.TransactionHashes = new UInt256[txCountToInlcude];
            Transaction[] txs = new Transaction[txCountToInlcude];
            txs[0] = TestUtils.CreateRandomMockMinerTransaction().Object;
            msg.TransactionHashes[0] = txs[0].Hash;
            for (int i = 1; i < txCountToInlcude; i++)
            {
                txs[i] = TestUtils.CreateRandomHashInvocationMockTransaction().Object;
                msg.TransactionHashes[i] = txs[i].Hash;
            }
            msg.Nonce = UInt64.MaxValue;
            msg.NextConsensus = UInt160.Parse("5555AAAA5555AAAA5555AAAA5555AAAA5555AAAA");
            msg.MinerTransaction = (MinerTransaction) txs[0];
            msg.PrepareWitnessInvocationScripts = new byte[7][];
            msg.PrepareWitnessInvocationScripts[0] = new [] {(byte)'t', (byte)'e'};
            msg.PrepareWitnessInvocationScripts[1] = new [] {(byte)'s', (byte)'t'};
            msg.PrepareWitnessInvocationScripts[2] = null;
            msg.PrepareWitnessInvocationScripts[3] = new [] {(byte)'1', (byte)'2'};
            msg.PrepareWitnessInvocationScripts[4] = null;
            msg.PrepareWitnessInvocationScripts[5] = null;
            msg.PrepareWitnessInvocationScripts[6] = new [] {(byte)'3', (byte)'!'};
            msg.PrepareTimestamps = new uint[7];
            msg.PrepareTimestamps[0] = 0;
            msg.PrepareTimestamps[1] = 1;
            msg.PrepareTimestamps[2] = 2;
            msg.PrepareTimestamps[3] = uint.MaxValue;
            msg.PrepareTimestamps[4] = 4;
            msg.PrepareTimestamps[5] = 5;
            msg.PrepareTimestamps[6] = 6;
            msg.CommitSignatures = new byte[7][];
            msg.CommitSignatures[0] = null;
            msg.CommitSignatures[1] = new [] {(byte)'1', (byte)'2'};
            msg.CommitSignatures[6] = new [] {(byte)'3', (byte)'D', (byte)'R', (byte)'I', (byte)'N', (byte)'K'};

            var copiedMsg = TestUtils.CopyMsgBySerialization(msg, new RecoveryMessage());;

            copiedMsg.ChangeViewWitnessInvocationScripts.ShouldAllBeEquivalentTo(null);
            copiedMsg.ChangeViewTimestamps.ShouldAllBeEquivalentTo(null);
            copiedMsg.TransactionHashes.ShouldAllBeEquivalentTo(msg.TransactionHashes);
            copiedMsg.Nonce.Should().Be(msg.Nonce);
            copiedMsg.NextConsensus.Should().Be(msg.NextConsensus);
            copiedMsg.MinerTransaction.Should().Be(msg.MinerTransaction);
            copiedMsg.PreparationHash.Should().Be(null);
            copiedMsg.PrepareWitnessInvocationScripts.ShouldAllBeEquivalentTo(msg.PrepareWitnessInvocationScripts);
            copiedMsg.PrepareTimestamps.ShouldAllBeEquivalentTo(msg.PrepareTimestamps);
            copiedMsg.CommitSignatures.ShouldAllBeEquivalentTo(msg.CommitSignatures);
        }
    }
}
