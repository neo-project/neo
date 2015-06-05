using AntShares.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AntShares.Core
{
    public class RegisterTransaction : Transaction
    {
        public RegisterType RegisterType;
        public string RegisterName;
        public Int64 Amount;
        public UInt160 Issuer;
        public UInt160 Admin;

        public RegisterTransaction()
            : base(TransactionType.RegisterTransaction)
        {
        }

        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            this.RegisterType = (RegisterType)reader.ReadByte();
            this.RegisterName = reader.ReadVarString();
            this.Amount = reader.ReadInt64();
            this.Issuer = reader.ReadSerializable<UInt160>();
            this.Admin = reader.ReadSerializable<UInt160>();
        }

        public override UInt160[] GetScriptHashesForVerifying()
        {
            IEnumerable<UInt160> hashes = base.GetScriptHashesForVerifying().Union(new UInt160[] { Issuer, Admin });
            if (RegisterType.HasFlag(RegisterType.Share))
            {
                hashes = hashes.Union(Outputs.Select(p => p.ScriptHash));
            }
            return hashes.OrderBy(p => p).ToArray();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write((byte)RegisterType);
            writer.WriteVarString(RegisterName);
            writer.Write(Amount);
            writer.Write(Issuer);
            writer.Write(Admin);
        }
    }
}
