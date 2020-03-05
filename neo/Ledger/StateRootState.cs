using Neo.IO;
using Neo.IO.Json;
using Neo.Network.P2P.Payloads;
using System.IO;

namespace Neo.Ledger
{
    public enum StateRootVerified : byte
    {
        Unverified = 0x00,
        Verified = 0x01,
        Invalid = 0x03,
    }

    public class StateRootState : StateBase, ICloneable<StateRootState>
    {
        public StateRootVerified Verified;
        public StateRoot StateRoot;

        public override int Size => base.Size + sizeof(bool) + StateRoot.Size;

        StateRootState ICloneable<StateRootState>.Clone()
        {
            return new StateRootState
            {
                Verified = Verified,
                StateRoot = StateRoot,
            };
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Verified = (StateRootVerified)reader.ReadByte();
            StateRoot = reader.ReadSerializable<StateRoot>();
        }

        void ICloneable<StateRootState>.FromReplica(StateRootState replica)
        {
            Verified = replica.Verified;
            StateRoot = replica.StateRoot;
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write((byte)Verified);
            writer.Write(StateRoot);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["verified"] = Verified;
            json["stateroot"] = StateRoot.ToJson();
            return json;
        }
    }
}
