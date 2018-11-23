using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Json;
using Neo.Network.P2P.Payloads;
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

  /*
  public class MessageReceived2
  {
      public int Counter { get; private set; }

      public MessageReceived2(int counter)
      {
          Counter = counter;
      }
  }

  public class BlueActor2 : ReceiveActor
  {
      private const string ActorName = "BlueActor2";
      private const ConsoleColor MessageColor = ConsoleColor.Blue;

      private int _counter = 0;

      protected override void PreStart()
      {
          base.PreStart();
          Become(HandleString);
      }

      private void HandleString()
      {
          Receive<string>(s =>
          {
              PrintMessage(s);
              _counter++;
              Sender.Tell(new MessageReceived2(_counter));
          });
      }

      private void PrintMessage(string message)
      {
          Console.ForegroundColor = MessageColor;
          Console.WriteLine(
              "{0} on thread #{1}: {2}",
              ActorName,
              Thread.CurrentThread.ManagedThreadId,
              message);
      }
  }
  */

  public class TestMessageReceived
  {
      public int Counter { get; private set; }

      public TestMessageReceived(int counter)
      {
          Counter = counter;
      }
  }

  public class TestReceiveActor : ReceiveActor
  {
      private const string ActorName = "TestReceiveActor";
      private int _counter = 0;

      protected override void PreStart()
      {
          base.PreStart();
          Become(HandleString);
      }

      private void HandleString()
      {
          Receive<string>(s =>
          {
              //PrintMessage(s);
              _counter++;
              Sender.Tell(new TestMessageReceived(_counter));
          });
      }

      private void PrintMessage(string message)
      {
          Console.WriteLine("TestMessageReceived PrintMessage");
          /*
          Console.ForegroundColor = MessageColor;
          Console.WriteLine(
              "{0} on thread #{1}: {2}",
              ActorName,
              Thread.CurrentThread.ManagedThreadId,
              message);
          */
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
          // NeoSystem dependencies
          TestActorRef<TestReceiveActor> actorLocalNode = ActorOfAsTestActorRef<TestReceiveActor>();
          TestActorRef<TestReceiveActor> actorTaskManager = ActorOfAsTestActorRef<TestReceiveActor>();
          var mockConsensusContext = new Mock<IConsensusContext>();
          //var mockConsensusContext = new Mock<ConsensusContext>();

          // context.Reset(): do nothing
          //mockConsensusContext.Setup(mr => mr.Update(It.IsAny<int>(), It.IsAny<string>()))
          mockConsensusContext.Setup(mr => mr.Reset()).Verifiable(); // void
          mockConsensusContext.SetupGet(mr => mr.MyIndex).Returns(2); // MyIndex == 2
          mockConsensusContext.SetupGet(mr => mr.BlockIndex).Returns(2);
          mockConsensusContext.SetupGet(mr => mr.PrimaryIndex).Returns(2);
          mockConsensusContext.Setup(mr => mr.GetPrimaryIndex(It.IsAny<byte>())).Returns(2);
          mockConsensusContext.SetupGet(mr => mr.State).Returns(ConsensusState.Initial);
          mockConsensusContext.Setup(mr => mr.GetUtcNow()).Returns(new DateTime(1968, 06, 01, 0, 0, 4, DateTimeKind.Utc));


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

          ECPoint[] points = new ECPoint[4];
          //Validators

          // check basic ConsensusContext
          mockConsensusContext.Object.MyIndex.Should().Be(2);
          mockConsensusContext.Object.GetUtcNow().ToTimestamp().Should().Be(4244941700); //1968-06-01 00:00:04


          //mockConsensusContext.Setup(m => m.CreateProduct(It.IsAny<Product>())).Returns(true);


          //TestActorRef<ConsensusService> actor2 = ActorOfAsTestActorRef<ConsensusService>();
          TestActorRef<ConsensusService> actor = ActorOfAsTestActorRef<ConsensusService>(Akka.Actor.Props.Create(() => new ConsensusService(actorLocalNode, actorTaskManager, mockConsensusContext.Object)));

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

          actor.Tell(new ConsensusService.Start());

          //ActorSystem.ActorOf(ConsensusService.Props(this, wallet));
          Console.WriteLine("comecou consensus!");
          //  actor.Tell(new ConsensusService.Stop());
          Thread.Sleep(500);
          //actor.Tell(new ConsensusService.Stop());
          //Thread.Sleep(500);
          Sys.Stop(actor);
          //Console.WriteLine("Fora do stop!");

          //var answer = ExpectMsg<MessageReceived>();
          //Assert.AreEqual(2, answer.Counter);
          Assert.AreEqual(2, 1);
      }
  }
}
