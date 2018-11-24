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
  public class TestReceiveActor : ReceiveActor
  {

      public TestReceiveActor() {
          Receive<LocalNode.SendDirectly>(greet =>
          {
              Console.WriteLine($"Received one message: {greet}");
              // when message arrives, we publish it on the event stream
              // and send response back to sender
              Context.System.EventStream.Publish("Prepare Request has arrived!");
              //Sender.Tell(new GreetBack(Self.Path.Name));
          });
      }
  }




  [TestClass]
  public class ConsensusTests : TestKit
  {
      [TestCleanup]
      public void Cleanup()
      {
          Shutdown();
      }

      [TestMethod]
      public void ConsensusService_Respond_After_OnStart()
      {
          TestProbe subscriber = CreateTestProbe();
          //TestActorRef<TestReceiveActor> receiverActor = ActorOfAsTestActorRef<TestReceiveActor>(); // (subscriber)

          //Sys.EventStream.Subscribe(subscriber.Ref, typeof (LocalNode.SendDirectly));

          // NeoSystem dependencies
          //TestActorRef<TestReceiveActor> actorLocalNode = ActorOfAsTestActorRef<TestReceiveActor>(subscriber);
          //TestActorRef<TestReceiveActor2> actorTaskManager = ActorOfAsTestActorRef<TestReceiveActor2>(subscriber);
          //subscriber.Subscribe(receiverActor);

          //Console.WriteLine($"Actors: localnode {actorLocalNode} taskManager {actorTaskManager}");

          var mockConsensusContext = new Mock<IConsensusContext>();
          //var mockConsensusContext = new Mock<ConsensusContext>();

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

          UInt256 val256 = UInt256.Zero;
          UInt256 merkRootVal;
          UInt160 val160;
          uint timestampVal, indexVal;
          ulong consensusDataVal;
          Witness scriptVal;
          Header header = new Header();
          TestUtils.SetupHeaderWithValues(header, val256, out merkRootVal, out val160, out timestampVal, out indexVal, out consensusDataVal, out scriptVal);
          header.Size.Should().Be(109);
          timestampVal.Should().Be(4244941696); //1968-06-01 00:00:00

          Console.WriteLine($"header {header} hash {header.Hash} timstamp {timestampVal} now {mockConsensusContext.Object.GetUtcNow().ToTimestamp()}");

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




          //ECPoint[] points = new ECPoint[4];
          //Validators

          // check basic ConsensusContext
          mockConsensusContext.Object.MyIndex.Should().Be(2);
          //mockConsensusContext.Object.block_received_time.ToTimestamp().Should().Be(4244941698); //1968-06-01 00:00:02
          mockConsensusContext.Object.GetUtcNow().ToTimestamp().Should().Be(4244941711); //1968-06-01 00:00:15


          mockConsensusContext.Setup(mr => mr.LocalNodeSendDirectly(It.IsAny<ConsensusPayload>()))
                                .Callback((ConsensusPayload _Inventory) => { Console.WriteLine("Sending!!");
                                      //actorLocalNode.Tell(new LocalNode.SendDirectly { Inventory = _Inventory }, TestActor); }
                                      //receiverActor.Tell(new LocalNode.SendDirectly { Inventory = _Inventory });
                                      subscriber.Tell(new LocalNode.SendDirectly { Inventory = _Inventory });
                                                                          }
                                         );
          //actorLocalNode, actorTaskManager, public void LocalNodeSendDirectly(ConsensusPayload _Inventory)

          //mockConsensusContext.Setup(m => m.CreateProduct(It.IsAny<Product>())).Returns(true);


          //TestActorRef<ConsensusService> actor2 = ActorOfAsTestActorRef<ConsensusService>();
          TestActorRef<ConsensusService> actor = ActorOfAsTestActorRef<ConsensusService>(Akka.Actor.Props.Create(() => new ConsensusService(mockConsensusContext.Object)));

          //TestActorRef<ConsensusService> actorRef = //TestActorRef<ConsensusService>(Akka.Actor.Props.Create(() => new ConsensusService(actorLocalNode, actorTaskManager, mockConsensusContext.Object)));

          /*
          Console.WriteLine("vai testar setup!");
          actor.Tell(new ConsensusService.Setup {
            _localNode = actorLocalNode,
            _taskManager = actorTaskManager,
            _context = mockConsensusContext.Object
            });
          Thread.Sleep(500);
          */


          actor.Tell(new ConsensusService.Start());//, TestActor);

          //ActorSystem.ActorOf(ConsensusService.Props(this, wallet));
          Console.WriteLine("comecou consensus!");
          //  actor.Tell(new ConsensusService.Stop());
          //Thread.Sleep(900);
          Console.WriteLine("OnTimer should expire!");

          //Neo.Network.P2P.LocalNode.SendDirectly

          Console.WriteLine("Waiting for subscriber message!");

          //subscriber.ExpectMsg<string>("Prepare Request has arrived!");
          //ExpectMsg<string>("Prepare Request has arrived!");

          var answer = subscriber.ExpectMsg<LocalNode.SendDirectly>();
          Console.WriteLine($"MESSAGE 1: {answer}");
          var answer2 = subscriber.ExpectMsg<LocalNode.SendDirectly>(); // expects to fail!

          //var msg = ExpectMsg<string>();
          //var msg = Sys.ExpectMsg<string>();
          //Console.WriteLine($"MESSAGE 1: {msg}");


          //var answer = ExpectMsg<LocalNode.SendDirectly>();

          //Thread.Sleep(4000);
          //actor.Tell(new ConsensusService.Stop());
          //Thread.Sleep(500);
          Sys.Stop(actor);
          //Console.WriteLine("Fora do stop!");

          //var answer = ExpectMsg<MessageReceived>();
          //Assert.AreEqual(2, answer.Counter);
          Assert.AreEqual(1, 1);
      }
  }
}
