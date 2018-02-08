using Neo.IO;
using System.IO;
using System.Linq;

namespace Neo.Core
{
    public class UnspentCoinState : StateBase, ICloneable<UnspentCoinState>
    {
        public CoinState[] Items;

        public override int Size => base.Size + Items.GetVarSize();

        UnspentCoinState ICloneable<UnspentCoinState>.Clone()
        {
            return new UnspentCoinState
            {
                Items = (CoinState[])Items.Clone()
            };
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Items = reader.ReadVarBytes().Select(p => (CoinState)p).ToArray();
        }

        void ICloneable<UnspentCoinState>.FromReplica(UnspentCoinState replica)
        {
            Items = replica.Items;
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.WriteVarBytes(Items.Cast<byte>().ToArray());
        }
    }
}
