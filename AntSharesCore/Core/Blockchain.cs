using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AntShares.IO;

namespace AntShares.Core
{
    public abstract class Blockchain
    {
        public static readonly RegisterTransaction AntShare = "4000465b7b276c616e67273a277a682d434853272c276e616d65273a27e5b08fe89a81e882a1277d2c7b276c616e67273a27656e272c276e616d65273a27416e745368617265277d5d00e1f50500000000eea34400951bc0e31a530ce8a8a63485c6271147eea34400951bc0e31a530ce8a8a63485c627114700000167403c3a86e7f87387e52bad9422a17c494c2c8350776ebfe303018546e27bc32f9a29ae8fbe8a695d2e5ba6a853dc5c9725a4475e943bfe53b1ea56141e3c9372f025512102c4a2fd44a0d80d84ea3258eaf7c3c2c9f5d22369dbbe5dafdcf4ead89f7fbdd051ae".HexToBytes().AsSerializable<RegisterTransaction>();

        public static readonly RegisterTransaction AntCoin = new RegisterTransaction
        {
            RegisterType = RegisterType.AntCoin,
            RegisterName = "[{'lang':'zh-CHS','name':'小蚁币'},{'lang':'en','name':'AntCoin'}]",
            Amount = (Int64)(100000000m).ToSatoshi(),
            Issuer = new UInt160(),
            Admin = new UInt160(),
            Inputs = new TransactionInput[0],
            Outputs = new TransactionOutput[0]
        };

        public static readonly Block GenesisBlock = new Block
        {
            PrevBlock = new UInt256(),
            Timestamp = DateTime.Now.ToTimestamp(), //测试环境，让每次生成的创世区块都不一样，所以时间选择当前系统时间
            Nonce = 2083236893, //向比特币致敬
            Miner = "AWAm7VzveC4qPFxnF55nLPjeD9DAEDQZZB".ToScriptHash(), //应该根据本区块中小蚁股的投票数来计算，但是现在小蚁股的初始分配还未决定，所以先手工指定记账人
            Transactions = new Transaction[]
            { 
                new GenerationTransaction
                {
                    Nonce = 2083236893,
                    Inputs = new TransactionInput[0],
                    Outputs = new TransactionOutput[]
                    {
                        new TransactionOutput
                        {

                        }
                    }
                }
            }
        };

        static Blockchain()
        {
            //1. AntShare的初始分配
            //2. 构造挖矿奖励
            //3. 构造创世区块
        }
    }
}
