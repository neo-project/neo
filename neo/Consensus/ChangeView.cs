using System.IO;

namespace Neo.Consensus
{
    public class ChangeView : ConsensusMessage
    {
        public enum ChangeViewReason : byte
        {
            Timeout = 0x0,
            WrongExpectedView = 0x1,
            TxNotFound = 0x2,
            TxRejectedByPolicy = 0x3,
            TxInvalid = 0x4,
        }

        /// <summary>
        /// NewViewNumber is always set to the current ViewNumber asking changeview + 1
        /// </summary>
        public byte NewViewNumber => (byte)(ViewNumber + 1);

        /// <summary>
        /// Reason
        /// </summary>
        public ChangeViewReason Reason;

        /// <summary>
        /// Timestamp of when the ChangeView message was created. This allows receiving nodes to ensure
        /// they only respond once to a specific ChangeView request (it thus prevents replay of the ChangeView
        /// message from repeatedly broadcasting RecoveryMessages).
        /// </summary>
        public uint Timestamp;

        public override int Size => base.Size +
            sizeof(uint) +              // Timestamp
            sizeof(ChangeViewReason);   // Reason

        public ChangeView() : base(ConsensusMessageType.ChangeView) { }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Timestamp = reader.ReadUInt32();
            Reason = (ChangeViewReason)reader.ReadByte();
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(Timestamp);
            writer.Write((byte)Reason);
        }
    }
}
