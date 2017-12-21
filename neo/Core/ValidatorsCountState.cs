using Neo.IO;
using System.IO;

namespace Neo.Core
{
    public class ValidatorsCountState : StateBase, ICloneable<ValidatorsCountState>
    {
        public Fixed8[] Votes;

        public override int Size => base.Size + Votes.GetVarSize();

        public ValidatorsCountState()
        {
            this.Votes = new Fixed8[Blockchain.MaxValidators];
        }

        ValidatorsCountState ICloneable<ValidatorsCountState>.Clone()
        {
            return new ValidatorsCountState
            {
                Votes = (Fixed8[])Votes.Clone()
            };
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Votes = reader.ReadSerializableArray<Fixed8>();
        }

        void ICloneable<ValidatorsCountState>.FromReplica(ValidatorsCountState replica)
        {
            Votes = replica.Votes;
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(Votes);
        }
    }
}
