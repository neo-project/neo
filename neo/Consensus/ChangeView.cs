using System;
using System.IO;

namespace Neo.Consensus
{
    internal class ChangeView : ConsensusMessage
    {
        public byte NewViewNumber;

        public ChangeView()
            : base(ConsensusMessageType.ChangeView)
        {
        }

        public static ChangeView Make(byte MyExpectedView)
        {
            return new ChangeView
            {
                NewViewNumber = MyExpectedView//ExpectedView[MyIndex]
            };
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            NewViewNumber = reader.ReadByte();
            if (NewViewNumber == 0) throw new FormatException();
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(NewViewNumber);
        }
    }
}
