using AntShares.IO;
using System.IO;
using System.Linq;

namespace AntShares.Wallets
{
    public class CustomContract : Contract
    {
        private ContractParameterType[] parameterList;

        public override ContractParameterType[] ParameterList => parameterList;

        public static CustomContract Create(UInt160 publicKeyHash, ContractParameterType[] parameterList, byte[] redeemScript)
        {
            return new CustomContract
            {
                parameterList = parameterList,
                RedeemScript = redeemScript,
                PublicKeyHash = publicKeyHash
            };
        }

        public override void Deserialize(BinaryReader reader)
        {
            parameterList = reader.ReadVarBytes().Cast<ContractParameterType>().ToArray();
            RedeemScript = reader.ReadVarBytes();
            PublicKeyHash = reader.ReadSerializable<UInt160>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.WriteVarBytes(parameterList.Cast<byte>().ToArray());
            writer.WriteVarBytes(RedeemScript);
            writer.Write(PublicKeyHash);
        }
    }
}
