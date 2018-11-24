using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Json;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Network.P2P;
using Akka.TestKit;
using Akka.TestKit.Xunit2;
using Akka;
using Akka.Actor;
using System;
using System.Threading;
using Neo.Consensus;
using Neo.Cryptography.ECC;
using Moq;

//Neo.Consensus
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
          mockConsensusContext.SetupProperty(mr => mr.block_received_time);

          mockConsensusContext.Object.block_received_time = new DateTime(1968, 06, 01, 0, 0, 1, DateTimeKind.Utc);
          mockConsensusContext.Setup(mr => mr.GetUtcNow()).Returns(new DateTime(1968, 06, 01, 0, 0, 15, DateTimeKind.Utc));


          // Creating proposed block
          UInt256 val256 = UInt256.Zero;
          UInt256 merkRootVal;
          UInt160 val160;
          uint timestampVal, indexVal;
          ulong consensusDataVal;
          Witness scriptVal;
          Header header = new Header();
          TestUtils.SetupHeaderWithValues(header, val256, out merkRootVal, out val160, out timestampVal, out indexVal, out consensusDataVal, out scriptVal);
          header.Size.Should().Be(109);

          Console.WriteLine($"header {header} hash {header.Hash} timstamp {timestampVal} now {mockConsensusContext.Object.GetUtcNow().ToTimestamp()}");

          timestampVal.Should().Be(4244941696); //1968-06-01 00:00:00
          // check basic ConsensusContext
          mockConsensusContext.Object.MyIndex.Should().Be(2);
          mockConsensusContext.Object.block_received_time.ToTimestamp().Should().Be(4244941697); //1968-06-01 00:00:01
          mockConsensusContext.Object.GetUtcNow().ToTimestamp().Should().Be(4244941711); //1968-06-01 00:00:15

          MinerTransaction minerTx = new MinerTransaction();
          minerTx.Attributes = new TransactionAttribute[0];
          minerTx.Inputs = new CoinReference[0];
          minerTx.Outputs = new TransactionOutput[0];
          minerTx.Witnesses = new Witness[0];
          minerTx.Nonce = 42;

          PrepareRequest prep = new PrepareRequest
          {
              Nonce = mockConsensusContext.Object.Nonce,
              NextConsensus = mockConsensusContext.Object.NextConsensus,
              TransactionHashes = new UInt256[0],
              MinerTransaction = minerTx, //(MinerTransaction)Transactions[TransactionHashes[0]],
              Signature = new byte[64]//Signatures[MyIndex]
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
                                   Akka.Actor.Props.Create(() => new ConsensusService(subscriber, subscriber, mockConsensusContext.Object))
                                                                                                 );
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

          Assert.AreEqual(1, 1);
      }
  }
}
