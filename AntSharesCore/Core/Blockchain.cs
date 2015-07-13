using AntShares.IO;
using System;
using System.Collections.Generic;

namespace AntShares.Core
{
    public abstract class Blockchain
    {
        //备用矿工未来要有5-7个
        public static readonly byte[][] StandbyMiners =
        {
            "02c4a2fd44a0d80d84ea3258eaf7c3c2c9f5d22369dbbe5dafdcf4ead89f7fbdd0".HexToBytes()
        };
        public static readonly Block GenesisBlock = "00000000000000000000000000000000000000000000000000000000000000000000000050411a9b63fdee20a4a45bd872085f21d5be335c0f6547f58f0581aea8fb131d0b74a355000000001dac2b7ceea34400951bc0e31a530ce8a8a63485c627114767409e96ff652de30fdf80fae9eebd0dda71cadfbd17657fba1a3ad3e4b68eb6c2ac1c706965df164adae670aeb2ade27b01058f7940faa669dc968abfc85d4ace5c25512102c4a2fd44a0d80d84ea3258eaf7c3c2c9f5d22369dbbe5dafdcf4ead89f7fbdd051ae0200000000000000004000455b7b276c616e67273a277a682d434e272c276e616d65273a27e5b08fe89a81e882a1277d2c7b276c616e67273a27656e272c276e616d65273a27416e745368617265277d5d00e1f50500000000eea34400951bc0e31a530ce8a8a63485c6271147eea34400951bc0e31a530ce8a8a63485c627114700000167401b1c1d7beedd74101bd3c8d831d6956e79de55136d085d6103f6685cbe4c36f9db87dfd83017756fdc2c9feda99c2871e8dc787e5b56d47c0fd9d3c53cab755525512102c4a2fd44a0d80d84ea3258eaf7c3c2c9f5d22369dbbe5dafdcf4ead89f7fbdd051ae".HexToBytes().AsSerializable<Block>();
        public static readonly RegisterTransaction AntShare = (RegisterTransaction)GenesisBlock.Transactions[1];
        public static readonly RegisterTransaction AntCoin = new RegisterTransaction
        {
            RegisterType = RegisterType.AntCoin,
            RegisterName = "[{'lang':'zh-CN','name':'小蚁币'},{'lang':'en','name':'AntCoin'}]",
            Amount = (Int64)(100000000m).ToSatoshi(),
            Issuer = new UInt160(),
            Admin = new UInt160(),
            Inputs = new TransactionInput[0],
            Outputs = new TransactionOutput[0],
            Scripts = new byte[0][]
        };

        public virtual IEnumerable<RegisterTransaction> GetAssets()
        {
            return new RegisterTransaction[] { AntShare, AntCoin };
        }

        public abstract long GetQuantityIssued(UInt256 asset_type);

        protected abstract void OnBlock(Block block);
    }
}
