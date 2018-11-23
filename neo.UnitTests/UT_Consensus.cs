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
using Moq;

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
