using Akka.Actor;

namespace Neo.Network.P2P
{
    class EmptyActor : UntypedActor
    {
        protected override void OnReceive(object message)
        {
        }

        internal static Props Props()
        {
            return Akka.Actor.Props.Create(() => new EmptyActor());
        }
    }
}
