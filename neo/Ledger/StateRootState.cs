using Neo.IO;
using Neo.IO.Json;
using Neo.Network.P2P.Payloads;
using System.IO;

namespace Neo.Ledger
{
    public enum StateRootVerifyFlag : byte
    {
        Unverified = 0x00,
        Verified = 0x01,
        Invalid = 0x03,
    }

    public class StateRootState : StateBase, ICloneable<StateRootState>
    {
        public StateRootVerifyFlag Flag;
        public StateRoot StateRoot;

        public override int Size => base.Size + sizeof(StateRootVerifyFlag) + StateRoot.Size;

        StateRootState ICloneable<StateRootState>.Clone()
        {
            return new StateRootState
            {
                Flag = Flag,
                StateRoot = StateRoot,
            };
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Flag = (StateRootVerifyFlag)reader.ReadByte();
            StateRoot = reader.ReadSerializable<StateRoot>();
        }

        void ICloneable<StateRootState>.FromReplica(StateRootState replica)
        {
            Flag = replica.Flag;
            StateRoot = replica.StateRoot;
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write((byte)Flag);
            writer.Write(StateRoot);
        }

        public override JObject ToJson()
        {
            JObject json = new JObject();
            json["flag"] = Flag;
            json["stateroot"] = StateRoot.ToJson();
            return json;
        }
    }
}
