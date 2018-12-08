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

namespace Neo.UnitTests
{

    [TestClass]
    public class ConsensusTests : TestKit
    {
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

            mockConsensusContext.SetupGet(mr => mr.MyIndex).Returns(2); // MyIndex == 2
            mockConsensusContext.SetupGet(mr => mr.BlockIndex).Returns(2);
            mockConsensusContext.SetupGet(mr => mr.PrimaryIndex).Returns(2);
            mockConsensusContext.SetupGet(mr => mr.ViewNumber).Returns(0);
            mockConsensusContext.SetupProperty(mr => mr.Nonce);
            mockConsensusContext.SetupProperty(mr => mr.NextConsensus);
            mockConsensusContext.SetupProperty(mr => mr.PreparePayload);
            mockConsensusContext.Object.NextConsensus = UInt160.Zero;
            mockConsensusContext.SetupGet(mr => mr.FinalSignatures).Returns(new byte[4][]);
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

            var timeMock = new Mock<TimeProvider>();
            timeMock.SetupGet(tp => tp.UtcNow).Returns(() => timeValues[timeIndex])
                                              .Callback(() => timeIndex++);
            TimeProvider.Current = timeMock.Object;

            // Creating proposed block
            Header header = new Header();
            TestUtils.SetupHeaderWithValues(header, UInt256.Zero, out UInt256 merkRootVal, out UInt160 val160, out uint timestampVal, out uint indexVal, out ulong consensusDataVal, out Witness scriptVal);
            header.Size.Should().Be(109);

            Console.WriteLine($"header {header} hash {header.Hash} timstamp {timestampVal}");

            timestampVal.Should().Be(4244941696); //1968-06-01 00:00:00
            TimeProvider.Current.UtcNow.ToTimestamp().Should().Be(4244941711); //1968-06-01 00:00:15
                                                                               // check basic ConsensusContext
            mockConsensusContext.Object.MyIndex.Should().Be(2);

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
                MinerTransaction = minerTx, //(MinerTransaction)Transactions[TransactionHashes[0]],
                PrepReqSignature = new byte[64], //PrepReqSignature[MyIndex]
                FinalSignature = new byte[64] //FinalSignature[MyIndex]
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

            mockConsensusContext.Setup(mr => mr.SignBlock(It.IsAny<Block>())).Returns(new byte[64]);
            mockConsensusContext.Setup(mr => mr.MakePrepareRequest(It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(prepPayload);
            mockConsensusContext.Setup(mr => mr.MakeHeader()).Returns(new Block
            {
                Version = header.Version,
                PrevHash = header.PrevHash,
                MerkleRoot = header.MerkleRoot,
                Timestamp = header.Timestamp,
                Index = header.Index + 1,
                ConsensusData = prep.Nonce,
                NextConsensus = header.NextConsensus
            });

            // ============================================================================
            //                      creating ConsensusService actor
            // ============================================================================

            TestActorRef<ConsensusService> actorConsensus = ActorOfAsTestActorRef<ConsensusService>(
                                     Akka.Actor.Props.Create(() => new ConsensusService(subscriber, subscriber, mockConsensusContext.Object))
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
                    ConsensusData = prep.Nonce,
                    NextConsensus = header.NextConsensus,
                    Transactions = new Transaction[0]
                }
            });

            Console.WriteLine("OnTimer should expire!");

            Console.WriteLine("Waiting for subscriber message!");
            var answer = subscriber.ExpectMsg<LocalNode.SendDirectly>();
            Console.WriteLine($"MESSAGE 1: {answer}");

            Console.WriteLine("Ok, subscriber!");

            // ============================================================================
            //                      finalize ConsensusService actor
            // ============================================================================

            //Thread.Sleep(4000);
            Sys.Stop(actorConsensus);
            TimeProvider.ResetToDefault();

            Assert.AreEqual(1, 1);
        }
    }
}
