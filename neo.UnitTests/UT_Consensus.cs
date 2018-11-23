using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Json;
using Neo.Network.P2P.Payloads;

namespace Neo.UnitTests
{

  public class BlueActor : ReceiveActor
  {
      private const string ActorName = "BlueActor";
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
              Sender.Tell(new MessageReceived(_counter));
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

  [TestClass]
  public class BlueActorTests : TestKit
  {
      [TestCleanup]
      public void Cleanup()
      {
          Shutdown();
      }

      [TestMethod]
      public void BlueActor_Respond_With_Counter()
      {
          var actor = ActorOfAsTestActorRef<BlueActor>();
          actor.Tell("test");
          var answer = ExpectMsg<MessageReceived>();
          Assert.AreEqual(1, answer.Counter);
      }
  }
}
