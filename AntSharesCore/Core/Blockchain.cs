using AntShares.Core.Scripts;
using AntShares.Cryptography;
using AntShares.Cryptography.ECC;
using AntShares.IO;
using AntShares.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AntShares.Core
{
    /// <summary>
    /// 实现区块链功能的基类
    /// </summary>
    public abstract class Blockchain : IDisposable
    {
        /// <summary>
        /// 当区块被写入到硬盘后触发
        /// </summary>
        public static event EventHandler<Block> PersistCompleted;

        /// <summary>
        /// 产生每个区块的时间间隔，已秒为单位
        /// </summary>
        public const uint SecondsPerBlock = 15;
        /// <summary>
        /// 小蚁币产量递减的时间间隔，以区块数量为单位
        /// </summary>
        public const uint DecrementInterval = 2000000;
        /// <summary>
        /// 每个区块产生的小蚁币的数量
        /// </summary>
        public static readonly uint[] MintingAmount = { 8, 7, 6, 5, 4, 3, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
        /// <summary>
        /// 产生每个区块的时间间隔
        /// </summary>
        public static readonly TimeSpan TimePerBlock = TimeSpan.FromSeconds(SecondsPerBlock);
        /// <summary>
        /// 后备记账人列表
        /// </summary>
        public static readonly ECPoint[] StandbyMiners =
        {
#if TESTNET
            ECPoint.DecodePoint("0327da12b5c40200e9f65569476bbff2218da4f32548ff43b6387ec1416a231ee8".HexToBytes(), ECCurve.Secp256r1),
            ECPoint.DecodePoint("026ce35b29147ad09e4afe4ec4a7319095f08198fa8babbe3c56e970b143528d22".HexToBytes(), ECCurve.Secp256r1),
            ECPoint.DecodePoint("0209e7fd41dfb5c2f8dc72eb30358ac100ea8c72da18847befe06eade68cebfcb9".HexToBytes(), ECCurve.Secp256r1),
            ECPoint.DecodePoint("039dafd8571a641058ccc832c5e2111ea39b09c0bde36050914384f7a48bce9bf9".HexToBytes(), ECCurve.Secp256r1),
            ECPoint.DecodePoint("038dddc06ce687677a53d54f096d2591ba2302068cf123c1f2d75c2dddc5425579".HexToBytes(), ECCurve.Secp256r1),
            ECPoint.DecodePoint("02d02b1873a0863cd042cc717da31cea0d7cf9db32b74d4c72c01b0011503e2e22".HexToBytes(), ECCurve.Secp256r1),
            ECPoint.DecodePoint("034ff5ceeac41acf22cd5ed2da17a6df4dd8358fcb2bfb1a43208ad0feaab2746b".HexToBytes(), ECCurve.Secp256r1),
#else
            ECPoint.DecodePoint("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c".HexToBytes(), ECCurve.Secp256r1),
            ECPoint.DecodePoint("02df48f60e8f3e01c48ff40b9b7f1310d7a8b2a193188befe1c2e3df740e895093".HexToBytes(), ECCurve.Secp256r1),
            ECPoint.DecodePoint("03b8d9d5771d8f513aa0869b9cc8d50986403b78c6da36890638c3d46a5adce04a".HexToBytes(), ECCurve.Secp256r1),
            ECPoint.DecodePoint("02ca0e27697b9c248f6f16e085fd0061e26f44da85b58ee835c110caa5ec3ba554".HexToBytes(), ECCurve.Secp256r1),
            ECPoint.DecodePoint("024c7b7fb6c310fccf1ba33b082519d82964ea93868d676662d4a59ad548df0e7d".HexToBytes(), ECCurve.Secp256r1),
            ECPoint.DecodePoint("02aaec38470f6aad0042c6e877cfd8087d2676b0f516fddd362801b9bd3936399e".HexToBytes(), ECCurve.Secp256r1),
            ECPoint.DecodePoint("02486fd15702c4490a26703112a5cc1d0923fd697a33406bd5a1c00e0013b09a70".HexToBytes(), ECCurve.Secp256r1),
#endif
        };

        /// <summary>
        /// 小蚁股
        /// </summary>
        public static readonly RegisterTransaction AntShare = new RegisterTransaction
        {
            AssetType = AssetType.AntShare,
#if TESTNET
            Name = "[{\"lang\":\"zh-CN\",\"name\":\"小蚁股(测试)\"},{\"lang\":\"en\",\"name\":\"AntShare(TestNet)\"}]",
#else
            Name = "[{\"lang\":\"zh-CN\",\"name\":\"小蚁股\"},{\"lang\":\"en\",\"name\":\"AntShare\"}]",
#endif
            Amount = Fixed8.FromDecimal(100000000),
            Precision = 0,
            Issuer = ECCurve.Secp256r1.Infinity,
            Admin = (new[] { (byte)ScriptOp.OP_TRUE }).ToScriptHash(),
            Attributes = new TransactionAttribute[0],
            Inputs = new TransactionInput[0],
            Outputs = new TransactionOutput[0]
        };

        /// <summary>
        /// 小蚁币
        /// </summary>
        public static readonly RegisterTransaction AntCoin = new RegisterTransaction
        {
            AssetType = AssetType.AntCoin,
#if TESTNET
            Name = "[{\"lang\":\"zh-CN\",\"name\":\"小蚁币(测试)\"},{\"lang\":\"en\",\"name\":\"AntCoin(TestNet)\"}]",
#else
            Name = "[{\"lang\":\"zh-CN\",\"name\":\"小蚁币\"},{\"lang\":\"en\",\"name\":\"AntCoin\"}]",
#endif
            Amount = Fixed8.FromDecimal(MintingAmount.Sum(p => p * DecrementInterval)),
            Precision = 8,
            Issuer = ECCurve.Secp256r1.Infinity,
            Admin = (new[] { (byte)ScriptOp.OP_FALSE }).ToScriptHash(),
            Attributes = new TransactionAttribute[0],
            Inputs = new TransactionInput[0],
            Outputs = new TransactionOutput[0]
        };

        /// <summary>
        /// 创世区块
        /// </summary>
        public static readonly Block GenesisBlock = new Block
        {
            PrevBlock = UInt256.Zero,
            Timestamp = (new DateTime(2016, 7, 15, 15, 8, 21, DateTimeKind.Utc)).ToTimestamp(),
            Height = 0,
            Nonce = 2083236893, //向比特币致敬
            NextMiner = GetMinerAddress(StandbyMiners),
            Script = new Script
            {
                StackScript = new byte[0],
                RedeemScript = new[] { (byte)ScriptOp.OP_TRUE }
            },
            Transactions = new Transaction[]
            {
                new MinerTransaction
                {
                    Nonce = 2083236893,
                    Attributes = new TransactionAttribute[0],
                    Inputs = new TransactionInput[0],
                    Outputs = new TransactionOutput[0],
                    Scripts = new Script[0]
                },
                AntShare,
                AntCoin,
                new IssueTransaction
                {
                    Attributes = new TransactionAttribute[0],
                    Inputs = new TransactionInput[0],
                    Outputs = new[]
                    {
                        new TransactionOutput
                        {
                            AssetId = AntShare.Hash,
                            Value = AntShare.Amount,
                            ScriptHash = Contract.CreateMultiSigRedeemScript(StandbyMiners.Length / 2 + 1, StandbyMiners).ToScriptHash()
                        }
                    },
                    Scripts = new[]
                    {
                        new Script
                        {
                            StackScript = new byte[0],
                            RedeemScript = new[] { (byte)ScriptOp.OP_TRUE }
                        }
                    }
                }
            }
        };

        /// <summary>
        /// 区块链所提供的功能
        /// </summary>
        public abstract BlockchainAbility Ability { get; }
        /// <summary>
        /// 当前最新区块散列值
        /// </summary>
        public abstract UInt256 CurrentBlockHash { get; }
        /// <summary>
        /// 当前最新区块头的散列值
        /// </summary>
        public virtual UInt256 CurrentHeaderHash => CurrentBlockHash;
        /// <summary>
        /// 默认的区块链实例
        /// </summary>
        public static Blockchain Default { get; private set; } = null;
        /// <summary>
        /// 区块头高度
        /// </summary>
        public virtual uint HeaderHeight => Height;
        /// <summary>
        /// 区块高度
        /// </summary>
        public abstract uint Height { get; }
        /// <summary>
        /// 表示当前的区块链实现是否为只读的
        /// </summary>
        public abstract bool IsReadOnly { get; }

        static Blockchain()
        {
            AntShare.Scripts = new[] { new Script { RedeemScript = Contract.CreateSignatureRedeemScript(AntShare.Issuer) } };
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.Push(ECCurve.Secp256r1.G.EncodePoint(true).Skip(1).Concat(AntShare.GetHashForSigning()).ToArray());
                AntShare.Scripts[0].StackScript = sb.ToArray();
            }
            AntCoin.Scripts = new[] { new Script { RedeemScript = Contract.CreateSignatureRedeemScript(AntCoin.Issuer) } };
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.Push(ECCurve.Secp256r1.G.EncodePoint(true).Skip(1).Concat(AntCoin.GetHashForSigning()).ToArray());
                AntCoin.Scripts[0].StackScript = sb.ToArray();
            }
            GenesisBlock.RebuildMerkleRoot();
        }

        /// <summary>
        /// 将指定的区块添加到区块链中
        /// </summary>
        /// <param name="block">要添加的区块</param>
        /// <returns>返回是否添加成功</returns>
        protected internal abstract bool AddBlock(Block block);

        /// <summary>
        /// 将指定的区块头添加到区块头链中
        /// </summary>
        /// <param name="headers">要添加的区块头列表</param>
        protected internal abstract void AddHeaders(IEnumerable<Header> headers);

        /// <summary>
        /// 判断区块链中是否包含指定的区块
        /// </summary>
        /// <param name="hash">区块编号</param>
        /// <returns>如果包含指定区块则返回true</returns>
        public virtual bool ContainsBlock(UInt256 hash)
        {
            return hash == GenesisBlock.Hash;
        }

        /// <summary>
        /// 判断区块链中是否包含指定的交易
        /// </summary>
        /// <param name="hash">交易编号</param>
        /// <returns>如果包含指定交易则返回true</returns>
        public virtual bool ContainsTransaction(UInt256 hash)
        {
            return GenesisBlock.Transactions.Any(p => p.Hash == hash);
        }

        public bool ContainsUnspent(TransactionInput input)
        {
            return ContainsUnspent(input.PrevHash, input.PrevIndex);
        }

        public abstract bool ContainsUnspent(UInt256 hash, ushort index);

        public abstract void Dispose();

        /// <summary>
        /// 根据指定的高度，返回对应的区块信息
        /// </summary>
        /// <param name="height">区块高度</param>
        /// <returns>返回对应的区块信息</returns>
        public Block GetBlock(uint height)
        {
            return GetBlock(GetBlockHash(height));
        }

        /// <summary>
        /// 根据指定的散列值，返回对应的区块信息
        /// </summary>
        /// <param name="hash">散列值</param>
        /// <returns>返回对应的区块信息</returns>
        public virtual Block GetBlock(UInt256 hash)
        {
            if (hash == GenesisBlock.Hash)
                return GenesisBlock;
            return null;
        }

        /// <summary>
        /// 根据指定的高度，返回对应区块的散列值
        /// </summary>
        /// <param name="height">区块高度</param>
        /// <returns>返回对应区块的散列值</returns>
        public virtual UInt256 GetBlockHash(uint height)
        {
            if (height == 0) return GenesisBlock.Hash;
            return null;
        }

        public virtual byte[] GetContract(UInt160 hash)
        {
            return null;
        }

        public IEnumerable<EnrollmentTransaction> GetEnrollments()
        {
            return GetEnrollments(Enumerable.Empty<Transaction>());
        }

        public abstract IEnumerable<EnrollmentTransaction> GetEnrollments(IEnumerable<Transaction> others);

        /// <summary>
        /// 根据指定的高度，返回对应的区块头信息
        /// </summary>
        /// <param name="height">区块高度</param>
        /// <returns>返回对应的区块头信息</returns>
        public virtual Header GetHeader(uint height)
        {
            return GetHeader(GetBlockHash(height));
        }

        /// <summary>
        /// 根据指定的散列值，返回对应的区块头信息
        /// </summary>
        /// <param name="hash">散列值</param>
        /// <returns>返回对应的区块头信息</returns>
        public virtual Header GetHeader(UInt256 hash)
        {
            return GetBlock(hash)?.Header;
        }

        public abstract UInt256[] GetLeafHeaderHashes();

        /// <summary>
        /// 获取记账人的合约地址
        /// </summary>
        /// <param name="miners">记账人的公钥列表</param>
        /// <returns>返回记账人的合约地址</returns>
        public static UInt160 GetMinerAddress(ECPoint[] miners)
        {
            return Contract.CreateMultiSigRedeemScript(miners.Length - (miners.Length - 1) / 3, miners).ToScriptHash();
        }

        private List<ECPoint> _miners = new List<ECPoint>();
        /// <summary>
        /// 获取下一个区块的记账人列表
        /// </summary>
        /// <returns>返回一组公钥，表示下一个区块的记账人列表</returns>
        public ECPoint[] GetMiners()
        {
            lock (_miners)
            {
                if (_miners.Count == 0)
                {
                    _miners.AddRange(GetMiners(Enumerable.Empty<Transaction>()));
                }
                return _miners.ToArray();
            }
        }

        public virtual IEnumerable<ECPoint> GetMiners(IEnumerable<Transaction> others)
        {
            if (!Ability.HasFlag(BlockchainAbility.TransactionIndexes) || !Ability.HasFlag(BlockchainAbility.UnspentIndexes))
                throw new NotSupportedException();
            //TODO: 此处排序可能将耗费大量内存，考虑是否采用其它机制
            Vote[] votes = GetVotes(others).OrderBy(p => p.Enrollments.Length).ToArray();
            int miner_count = (int)votes.WeightedFilter(0.25, 0.75, p => p.Count.GetData(), (p, w) => new
            {
                MinerCount = p.Enrollments.Length,
                Weight = w
            }).WeightedAverage(p => p.MinerCount, p => p.Weight);
            miner_count = Math.Max(miner_count, StandbyMiners.Length);
            Dictionary<ECPoint, Fixed8> miners = new Dictionary<ECPoint, Fixed8>();
            Dictionary<UInt256, ECPoint> enrollments = GetEnrollments(others).ToDictionary(p => p.Hash, p => p.PublicKey);
            foreach (var vote in votes)
            {
                foreach (UInt256 hash in vote.Enrollments.Take(miner_count))
                {
                    if (!enrollments.ContainsKey(hash)) continue;
                    ECPoint pubkey = enrollments[hash];
                    if (!miners.ContainsKey(pubkey))
                    {
                        miners.Add(pubkey, Fixed8.Zero);
                    }
                    miners[pubkey] += vote.Count;
                }
            }
            return miners.OrderByDescending(p => p.Value).ThenBy(p => p.Key).Select(p => p.Key).Concat(StandbyMiners).Take(miner_count);
        }

        /// <summary>
        /// 根据指定的散列值，返回下一个区块的信息
        /// </summary>
        /// <param name="hash">散列值</param>
        /// <returns>返回下一个区块的信息>
        public abstract Block GetNextBlock(UInt256 hash);

        /// <summary>
        /// 根据指定的散列值，返回下一个区块的散列值
        /// </summary>
        /// <param name="hash">散列值</param>
        /// <returns>返回下一个区块的散列值</returns>
        public abstract UInt256 GetNextBlockHash(UInt256 hash);

        /// <summary>
        /// 根据指定的资产编号，返回对应资产的发行量
        /// </summary>
        /// <param name="asset_id">资产编号</param>
        /// <returns>返回对应资产的当前已经发行的数量</returns>
        public abstract Fixed8 GetQuantityIssued(UInt256 asset_id);

        /// <summary>
        /// 根据指定的区块高度，返回对应区块及之前所有区块中包含的系统费用的总量
        /// </summary>
        /// <param name="height">区块高度</param>
        /// <returns>返回对应的系统费用的总量</returns>
        public virtual long GetSysFeeAmount(uint height)
        {
            return GetSysFeeAmount(GetBlockHash(height));
        }

        /// <summary>
        /// 根据指定的区块散列值，返回对应区块及之前所有区块中包含的系统费用的总量
        /// </summary>
        /// <param name="hash">散列值</param>
        /// <returns>返回系统费用的总量</returns>
        public abstract long GetSysFeeAmount(UInt256 hash);

        /// <summary>
        /// 根据指定的散列值，返回对应的交易信息
        /// </summary>
        /// <param name="hash">散列值</param>
        /// <returns>返回对应的交易信息</returns>
        public Transaction GetTransaction(UInt256 hash)
        {
            int height;
            return GetTransaction(hash, out height);
        }

        /// <summary>
        /// 根据指定的散列值，返回对应的交易信息与该交易所在区块的高度
        /// </summary>
        /// <param name="hash">交易散列值</param>
        /// <param name="height">返回该交易所在区块的高度</param>
        /// <returns>返回对应的交易信息</returns>
        public virtual Transaction GetTransaction(UInt256 hash, out int height)
        {
            Transaction tx = GenesisBlock.Transactions.FirstOrDefault(p => p.Hash == hash);
            if (tx != null)
            {
                height = 0;
                return tx;
            }
            height = -1;
            return null;
        }

        public abstract Dictionary<ushort, Claimable> GetUnclaimed(UInt256 hash);

        /// <summary>
        /// 根据指定的散列值和索引，获取对应的未花费的资产
        /// </summary>
        /// <param name="hash">交易散列值</param>
        /// <param name="index">输出的索引</param>
        /// <returns>返回一个交易输出，表示一个未花费的资产</returns>
        public abstract TransactionOutput GetUnspent(UInt256 hash, ushort index);

        /// <summary>
        /// 获取选票信息
        /// </summary>
        /// <returns>返回一个选票列表，包含当前区块链中所有有效的选票</returns>
        public IEnumerable<Vote> GetVotes()
        {
            return GetVotes(Enumerable.Empty<Transaction>());
        }

        public abstract IEnumerable<Vote> GetVotes(IEnumerable<Transaction> others);

        /// <summary>
        /// 判断交易是否双花
        /// </summary>
        /// <param name="tx">交易</param>
        /// <returns>返回交易是否双花</returns>
        public abstract bool IsDoubleSpend(Transaction tx);

        /// <summary>
        /// 当区块被写入到硬盘后调用
        /// </summary>
        /// <param name="block">区块</param>
        protected void OnPersistCompleted(Block block)
        {
            lock (_miners)
            {
                _miners.Clear();
            }
            if (PersistCompleted != null) PersistCompleted(this, block);
        }

        /// <summary>
        /// 注册默认的区块链实例
        /// </summary>
        /// <param name="blockchain">区块链实例</param>
        /// <returns>返回注册后的区块链实例</returns>
        public static Blockchain RegisterBlockchain(Blockchain blockchain)
        {
            if (blockchain == null) throw new ArgumentNullException();
            if (Default != null) Default.Dispose();
            Default = blockchain;
            return blockchain;
        }
    }
}
