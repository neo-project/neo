using AntShares.Cryptography.ECC;
using AntShares.IO;
using System.IO;

namespace AntShares.Core
{
    public class ValidatorState : StateBase, ICloneable<ValidatorState>
    {
        public ECPoint PublicKey;

        public override int Size => base.Size + PublicKey.Size;

        ValidatorState ICloneable<ValidatorState>.Clone()
        {
            return new ValidatorState
            {
                PublicKey = PublicKey
            };
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            PublicKey = ECPoint.DeserializeFrom(reader, ECCurve.Secp256r1);
        }

        void ICloneable<ValidatorState>.FromReplica(ValidatorState replica)
        {
            PublicKey = replica.PublicKey;
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(PublicKey);
        }
    }
}
