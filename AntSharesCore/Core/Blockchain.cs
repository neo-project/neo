using AntShares.IO;
using System;

namespace AntShares.Core
{
    public abstract class Blockchain
    {
        //备用矿工未来要有5-7个
        public static readonly byte[][] StandbyMiners =
        {
            "02c4a2fd44a0d80d84ea3258eaf7c3c2c9f5d22369dbbe5dafdcf4ead89f7fbdd0".HexToBytes()
        };
        public static readonly RegisterTransaction AntShare = "4000465b7b276c616e67273a277a682d434853272c276e616d65273a27e5b08fe89a81e882a1277d2c7b276c616e67273a27656e272c276e616d65273a27416e745368617265277d5d00e1f50500000000eea34400951bc0e31a530ce8a8a63485c6271147eea34400951bc0e31a530ce8a8a63485c627114700000167403c3a86e7f87387e52bad9422a17c494c2c8350776ebfe303018546e27bc32f9a29ae8fbe8a695d2e5ba6a853dc5c9725a4475e943bfe53b1ea56141e3c9372f025512102c4a2fd44a0d80d84ea3258eaf7c3c2c9f5d22369dbbe5dafdcf4ead89f7fbdd051ae".HexToBytes().AsSerializable<RegisterTransaction>();
        public static readonly RegisterTransaction AntCoin = new RegisterTransaction
        {
            RegisterType = RegisterType.AntCoin,
            RegisterName = "[{'lang':'zh-CHS','name':'小蚁币'},{'lang':'en','name':'AntCoin'}]",
            Amount = (Int64)(100000000m).ToSatoshi(),
            Issuer = new UInt160(),
            Admin = new UInt160(),
            Inputs = new TransactionInput[0],
            Outputs = new TransactionOutput[0],
            Scripts = new byte[0][]
        };
        public static readonly Block GenesisBlock = "000000000000000000000000000000000000000000000000000000000000000000000000c7f085b45c34dbbb8b7adcd5627bd822b8fdf7da78934b5f23edb685b5ed3824ff438155000000001dac2b7ceea34400951bc0e31a530ce8a8a63485c627114767403b9f4b679d04c9552fdff0a87fa0e6a3d6d92281a00b0c910aa098598b7767f9df91162b2bf204e89542afcf4e081f09662c40c8d8bbbd01608582cceb422fc325512102c4a2fd44a0d80d84ea3258eaf7c3c2c9f5d22369dbbe5dafdcf4ead89f7fbdd051ae0200000000000000004000465b7b276c616e67273a277a682d434853272c276e616d65273a27e5b08fe89a81e882a1277d2c7b276c616e67273a27656e272c276e616d65273a27416e745368617265277d5d00e1f50500000000eea34400951bc0e31a530ce8a8a63485c6271147eea34400951bc0e31a530ce8a8a63485c627114700000167403c3a86e7f87387e52bad9422a17c494c2c8350776ebfe303018546e27bc32f9a29ae8fbe8a695d2e5ba6a853dc5c9725a4475e943bfe53b1ea56141e3c9372f025512102c4a2fd44a0d80d84ea3258eaf7c3c2c9f5d22369dbbe5dafdcf4ead89f7fbdd051ae".HexToBytes().AsSerializable<Block>();

        protected abstract void OnBlock(Block block);
    }
}
