using Neo.IO;
using System.IO;
using System.Linq;

namespace Neo.Core
{
    public class UnspentCoinState : StateBase
    {
        public CoinState[] Items;

        public override int Size => base.Size + Items.GetVarSize();

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Items = reader.ReadVarBytes().Select(p => (CoinState)p).ToArray();
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.WriteVarBytes(Items.Cast<byte>().ToArray());
        }
    }
}
