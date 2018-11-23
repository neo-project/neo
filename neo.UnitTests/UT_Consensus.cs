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
          //IActorRef actor = Sys.ActorOf<ConsensusService>();//ActorOfAsTestActorRef<ConsensusService>();
          TestActorRef<ConsensusService> actor = ActorOfAsTestActorRef<ConsensusService>();
          //ConsensusService cs = actor.UnderlyingActor;//UnderlyingActor;
              //actor.Tell("test");
          actor.Tell(new ConsensusService.Start());

          //ActorSystem.ActorOf(ConsensusService.Props(this, wallet));
          Console.WriteLine("comecou consensus!");
          //  actor.Tell(new ConsensusService.Stop());
          Thread.Sleep(500);
          actor.Tell(new ConsensusService.Stop());
          Thread.Sleep(500);
          Sys.Stop(actor);
          Console.WriteLine("Fora do stop!");

          //var answer = ExpectMsg<MessageReceived>();
          //Assert.AreEqual(2, answer.Counter);
          Assert.AreEqual(2, 1);
      }
  }
}
