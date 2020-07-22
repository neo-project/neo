using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class OracleRequestAttribute : TransactionAttribute
    {
        public override TransactionAttributeType Type => TransactionAttributeType.OracleRequest;
        public override bool AllowMultiple => false;

        protected override void DeserializeWithoutType(BinaryReader reader) { }
        protected override void SerializeWithoutType(BinaryWriter writer) { }
    }
}
