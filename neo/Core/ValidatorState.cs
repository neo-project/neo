using Neo.Cryptography.ECC;
using Neo.IO;
using System;
using System.IO;

namespace Neo.Core
{
    public class ValidatorState : StateBase, ICloneable<ValidatorState>, IEquatable<ValidatorState>
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

        public bool Equals(ValidatorState other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (ReferenceEquals(null, other)) return false;
            return PublicKey.Equals(other.PublicKey);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            if (ReferenceEquals(null, obj)) return false;
            return Equals(obj as ValidatorState);
        }

        void ICloneable<ValidatorState>.FromReplica(ValidatorState replica)
        {
            PublicKey = replica.PublicKey;
        }

        public override int GetHashCode()
        {
            return PublicKey.GetHashCode();
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(PublicKey);
        }
    }
}
