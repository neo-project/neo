using System.IO;

namespace Neo.Consensus
{
    public class ChangeView : ConsensusMessage
    {
        public byte NewViewNumber;
        /// <summary>
        /// Timestamp of when the ChangeView message was created. This allows receiving nodes to ensure
        /// they only respond once to a specific ChangeView request (it thus prevents replay of the ChangeView
        /// message from repeatedly broadcasting RecoveryMessages).
        /// </summary>
        public uint Timestamp;
        /// <summary>
        /// Flag whether the node is locked/committed to change view from it's current view.
        /// If not set, this indicates this is a request to change view, but not a commitment, and therefore it may
        /// still accept further preparations and commit to generate a block in the current view.
        /// If set, this node is locked to change view, and will not accept further preparations in the current view.
        /// </summary>
        public bool Locked;

        public override int Size => base.Size
            + sizeof(byte)  //NewViewNumber
            + sizeof(uint)  //Timestamp
            + sizeof(bool); //Committed

        public ChangeView()
            : base(ConsensusMessageType.ChangeView)
        {
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            NewViewNumber = reader.ReadByte();
            Timestamp = reader.ReadUInt32();
            Locked = reader.ReadBoolean();
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(NewViewNumber);
            writer.Write(Timestamp);
            writer.Write(Locked);
        }
    }
}
