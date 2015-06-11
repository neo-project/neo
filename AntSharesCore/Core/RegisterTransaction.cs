using AntShares.IO;
using System;
using System.IO;
using System.Linq;

namespace AntShares.Core
{
    public class RegisterTransaction : Transaction
    {
        public RegisterType RegisterType;
        public string RegisterName;
        /// <summary>
        /// 发行总量，共有3中模式：
        /// 1. 限量模式：当Amount为正数时，表示当前资产的最大总量为Amount，且不可修改（股权在未来可能会支持扩股或增发，会考虑需要公司签名或一定比例的股东签名认可）。
        /// 2. 信贷模式：当Amount等于0时，表示当前资产的发行方式为信贷方式。通常网关会采用这种模式，当用户充值时，产生正负两枚货币，负值表示负债，不可转让；当用户提现时，正负合并归零。无论何时，该资产在链上的总和为0。
        /// 3. 不限量模式：当Amount等于-1时，表示当前资产可以由创建者无限量发行。这种模式的自由度最大，但是公信力最低，不建议使用。
        /// 在使用过程中，根据资产类型的不同，能够使用的总量模式也不同，具体规则如下：
        /// 1. 对于股权，只能使用限量模式；
        /// 2. 对于货币，只能使用信贷模式；
        /// 3. 对于点券，可以使用任意模式；
        /// </summary>
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
            if (Amount < -1)
                throw new FormatException();
            this.Issuer = reader.ReadSerializable<UInt160>();
            this.Admin = reader.ReadSerializable<UInt160>();
        }

        public override UInt160[] GetScriptHashesForVerifying()
        {
            return base.GetScriptHashesForVerifying().Union(new UInt160[] { Issuer, Admin }).OrderBy(p => p).ToArray();
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
