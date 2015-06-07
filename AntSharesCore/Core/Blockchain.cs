using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntShares.Core
{
    public abstract class Blockchain
    {
        public static readonly UInt160 AntSharesIssuer = "AWAm7VzveC4qPFxnF55nLPjeD9DAEDQZZB".ToScriptHash();

        public static readonly RegisterTransaction AntShare = new RegisterTransaction
        {
            RegisterType = RegisterType.System,
            RegisterName = "<names><name lang=\"zh-CHS\" value=\"小蚁股\"/><name lang=\"en\" value=\"AntShare\"/></names>",
            Amount = 100000000,
            Issuer = AntSharesIssuer,
            Admin = AntSharesIssuer, //暂时如此，正式发布前需要改成适当的管理员
            Inputs = new TransactionInput[0],
            Outputs = new TransactionOutput[0] //初始分配尚未决定
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
            //1. AntShare资产需要签名
            //2. 需要登记AntCoin资产
            //3. 构造创世区块
        }
    }
}
