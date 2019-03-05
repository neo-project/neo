using System.IO;

namespace Neo.Consensus
{
    internal class ChangeView : ConsensusMessage
    {
        public byte NewViewNumber;
        /// <summary>
        /// Timestamp of when the ChangeView message was created. This allows receiving nodes to ensure
        // they only respond once to a specific ChangeView request (it thus prevents replay of the ChangeView
        // message from repeatedly broadcasting RecoveryMessages).
        /// </summary>
        public uint Timestamp;

        public override int Size => base.Size
            + sizeof(byte)  //NewViewNumber
            + sizeof(uint); //Timestamp

        public ChangeView()
            : base(ConsensusMessageType.ChangeView)
        {
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            NewViewNumber = reader.ReadByte();
            Timestamp = reader.ReadUInt32();
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(NewViewNumber);
            writer.Write(Timestamp);
        }
    }
}
