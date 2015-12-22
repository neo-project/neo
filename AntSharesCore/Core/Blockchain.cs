using AntShares.Core.Scripts;
using AntShares.Cryptography.ECC;
using AntShares.IO;
using AntShares.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AntShares.Core
{
    public abstract class Blockchain : IDisposable
    {
        public event EventHandler<Block> PersistCompleted;

        public const int SecondsPerBlock = 15;
        private const int BlocksPerYear = 365 * 24 * 60 * 60 / SecondsPerBlock;
        private const double R_Init = 0.5;
        private const double R_Final = 0.3;
        public static readonly decimal GenerationFactor = 1 - (decimal)Math.Pow(R_Final / R_Init, 1.0 / BlocksPerYear);
        public static readonly TimeSpan TimePerBlock = TimeSpan.FromSeconds(SecondsPerBlock);
        public static readonly ECPoint[] StandbyMiners =
        {
            ECPoint.DecodePoint("0327da12b5c40200e9f65569476bbff2218da4f32548ff43b6387ec1416a231ee8".HexToBytes(), ECCurve.Secp256r1),
            ECPoint.DecodePoint("026ce35b29147ad09e4afe4ec4a7319095f08198fa8babbe3c56e970b143528d22".HexToBytes(), ECCurve.Secp256r1),
            ECPoint.DecodePoint("0209e7fd41dfb5c2f8dc72eb30358ac100ea8c72da18847befe06eade68cebfcb9".HexToBytes(), ECCurve.Secp256r1),
            ECPoint.DecodePoint("039dafd8571a641058ccc832c5e2111ea39b09c0bde36050914384f7a48bce9bf9".HexToBytes(), ECCurve.Secp256r1),
            ECPoint.DecodePoint("038dddc06ce687677a53d54f096d2591ba2302068cf123c1f2d75c2dddc5425579".HexToBytes(), ECCurve.Secp256r1),
        };
        public static readonly Block GenesisBlock = "0000000000000000000000000000000000000000000000000000000000000000000000003e237594e6e2b0cdd92aea5beecab6662662820597f1424a935c4dfc362bfafc81fc7856000000001dac2b7c00000000d5b21f2e11a9795a22a482f342f53634b5a8cf3afd450140af61f3ede868743262d6efe56dec7fcf6f9c75f42e4289e9165d26a08b870fc65f8a3b2b72f52fed206b9f215f1b993ebb3fce0a27b70d8de9782edb900b12094027bec17f618362a3beb8da43850d192880ab7e77c6d0c7bd12a81669f8afc38fca431adc9c4603ae2a70390ab0945b3484fa6c405742b409be9eb952ada981e8408d4584f68a66962e5a6b4454847bab196776432331a8ee2e538de117f7b4d8371dbcf177581b9e02c7db07253fd653fb249b60597a737d565d6b1209805bbfce40f9a17482f956e0567378f444f2f5256b4c15d4a425302ff96da3530d4e07c47eb08d32ebdd104db01bfb09e4c34292a94d26cdc8b6efd3a86f6fbceff44bfb59404dacce9dda32ec14f17448d6422f95dab8876d75371004183909819090021c58d71c070c66d82591a2a21a40d5cd2af12121b33403280735de1bc6aaf234dd71ad53210209e7fd41dfb5c2f8dc72eb30358ac100ea8c72da18847befe06eade68cebfcb9210327da12b5c40200e9f65569476bbff2218da4f32548ff43b6387ec1416a231ee821026ce35b29147ad09e4afe4ec4a7319095f08198fa8babbe3c56e970b143528d2221038dddc06ce687677a53d54f096d2591ba2302068cf123c1f2d75c2dddc542557921039dafd8571a641058ccc832c5e2111ea39b09c0bde36050914384f7a48bce9bf955ae020000000000000000004000565b7b276c616e67273a277a682d434e272c276e616d65273a27e5b08fe89a81e882a128e6b58be8af9529277d2c7b276c616e67273a27656e272c276e616d65273a27416e74536861726528546573744e657429277d5d0000c16ff28623000327da12b5c40200e9f65569476bbff2218da4f32548ff43b6387ec1416a231ee8d5b21f2e11a9795a22a482f342f53634b5a8cf3a000000024140925b0d1dc68ceb21337886439f2cd40009746fe3332b9149d0214ae7c9d3c9d2056d4adb8d5d14d7ff07447b3c71032508b36b81b7014921c819a042dbfbb60a23210327da12b5c40200e9f65569476bbff2218da4f32548ff43b6387ec1416a231ee8acfd450140c85d5de0f90a7ae9e268ef2b092a63d39c9022c7fce44714a7c9171d58304712cbe90cca5718c210ccf6e99cc3a0a1ac27ea52a1f3b18c1d60b9c4842c88153c4049a8674b3637b7b72e2950b75b8333d0294be7d46fe44309e1cc9d47ffa1e55c3b2d9968267316b1425a54369e55c2103915f064a2d479cd34bf665f2ef0acba40c397b172d90401dd38e965489e36dcdf1129cc28c387dfc2d19d2a7d1c9c35e8344fda9bafc7b80be652a892f506ae98013111d2e332a6afe1bf2a98bb92aee34023e4565d362b9d8708c5a661c9b5f3973d4deaa24be18122a97c68286aed76523d79f13afc1ffd75b369afe799c6516ed25d3862c2d6dc9693f9bc360563410f40819abbd4c52c98360f0b54b67e62e1726a3a5704cf4d09ce0a270226ac22d4397158785d5c5b27b05a041a7b52cd993722afff807b9ed227799c06fe6d321e51ad53210209e7fd41dfb5c2f8dc72eb30358ac100ea8c72da18847befe06eade68cebfcb9210327da12b5c40200e9f65569476bbff2218da4f32548ff43b6387ec1416a231ee821026ce35b29147ad09e4afe4ec4a7319095f08198fa8babbe3c56e970b143528d2221038dddc06ce687677a53d54f096d2591ba2302068cf123c1f2d75c2dddc542557921039dafd8571a641058ccc832c5e2111ea39b09c0bde36050914384f7a48bce9bf955ae".HexToBytes().AsSerializable<Block>();
        public static readonly RegisterTransaction AntShare = (RegisterTransaction)GenesisBlock.Transactions[1];
        public static readonly RegisterTransaction AntCoin = new RegisterTransaction
        {
            AssetType = AssetType.AntCoin,
#if TESTNET
            Name = "[{'lang':'zh-CN','name':'小蚁币(测试)'},{'lang':'en','name':'AntCoin(TestNet)'}]",
#else
            Name = "[{'lang':'zh-CN','name':'小蚁币'},{'lang':'en','name':'AntCoin'}]",
#endif
            Amount = Fixed8.FromDecimal(100000000),
            Issuer = ECCurve.Secp256r1.Infinity,
            Admin = new UInt160(),
            Attributes = new TransactionAttribute[0],
            Inputs = new TransactionInput[0],
            Outputs = new TransactionOutput[0],
            Scripts = new Script[0]
        };
        protected static readonly Dictionary<UInt256, Transaction> MemoryPool = new Dictionary<UInt256, Transaction>();

        public abstract BlockchainAbility Ability { get; }
        public abstract UInt256 CurrentBlockHash { get; }
        public virtual UInt256 CurrentHeaderHash => CurrentBlockHash;
        public static Blockchain Default { get; private set; } = null;
        public virtual uint HeaderHeight => Height;
        public abstract uint Height { get; }
        public abstract bool IsReadOnly { get; }

        protected internal abstract bool AddBlock(Block block);

        protected internal abstract void AddHeaders(IEnumerable<Block> headers);

        internal bool AddTransaction(Transaction tx)
        {
            lock (MemoryPool)
            {
                if (ContainsTransaction(tx.Hash)) return false;
                if (IsDoubleSpend(tx)) return false;
                if (!tx.Verify()) return false;
                MemoryPool.Add(tx.Hash, tx);
                return true;
            }
        }

        public virtual bool ContainsAsset(UInt256 hash)
        {
            return hash == AntCoin.Hash || hash == AntShare.Hash;
        }

        public virtual bool ContainsBlock(UInt256 hash)
        {
            return hash == GenesisBlock.Hash;
        }

        public virtual bool ContainsTransaction(UInt256 hash)
        {
            return hash == AntCoin.Hash || GenesisBlock.Transactions.Any(p => p.Hash == hash) || MemoryPool.ContainsKey(hash);
        }

        public bool ContainsUnspent(TransactionInput input)
        {
            return ContainsUnspent(input.PrevHash, input.PrevIndex);
        }

        public virtual bool ContainsUnspent(UInt256 hash, ushort index)
        {
            Transaction tx;
            if (!MemoryPool.TryGetValue(hash, out tx))
                return false;
            return index < tx.Outputs.Length;
        }

        public abstract void Dispose();

        public abstract IEnumerable<RegisterTransaction> GetAssets();

        public virtual Block GetBlock(uint height)
        {
            if (height == 0) return GenesisBlock;
            return null;
        }

        public virtual Block GetBlock(UInt256 hash)
        {
            if (hash == GenesisBlock.Hash)
                return GenesisBlock;
            return null;
        }

        public IEnumerable<EnrollmentTransaction> GetEnrollments()
        {
            return GetEnrollments(Enumerable.Empty<Transaction>());
        }

        public abstract IEnumerable<EnrollmentTransaction> GetEnrollments(IEnumerable<Transaction> others);

        public virtual Block GetHeader(UInt256 hash)
        {
            return GetBlock(hash)?.Header;
        }

        public abstract UInt256[] GetLeafHeaderHashes();

        public IEnumerable<Transaction> GetMemoryPool()
        {
            return MemoryPool.Values;
        }

        public static UInt160 GetMinerAddress(ECPoint[] miners)
        {
            return MultiSigContract.CreateMultiSigRedeemScript(miners.Length / 2 + 1, miners).ToScriptHash();
        }

        private List<ECPoint> _miners = new List<ECPoint>();
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
                foreach (UInt256 hash in vote.Enrollments)
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

        public abstract Block GetNextBlock(UInt256 hash);

        public abstract UInt256 GetNextBlockHash(UInt256 hash);

        public abstract Fixed8 GetQuantityIssued(UInt256 asset_id);

        public virtual Transaction GetTransaction(UInt256 hash)
        {
            if (hash == AntCoin.Hash)
                return AntCoin;
            Transaction tx;
            if (MemoryPool.TryGetValue(hash, out tx))
                return tx;
            return GenesisBlock.Transactions.FirstOrDefault(p => p.Hash == hash);
        }

        public virtual TransactionOutput GetUnspent(UInt256 hash, ushort index)
        {
            Transaction tx;
            if (!MemoryPool.TryGetValue(hash, out tx) || index >= tx.Outputs.Length)
                return null;
            return tx.Outputs[index];
        }

        public abstract IEnumerable<TransactionOutput> GetUnspentAntShares();

        public IEnumerable<Vote> GetVotes()
        {
            return GetVotes(Enumerable.Empty<Transaction>());
        }

        public abstract IEnumerable<Vote> GetVotes(IEnumerable<Transaction> others);

        public abstract bool IsDoubleSpend(Transaction tx);

        protected void OnPersistCompleted(Block block)
        {
            lock (_miners)
            {
                _miners.Clear();
            }
            lock (MemoryPool)
            {
                foreach (Transaction tx in block.Transactions)
                {
                    MemoryPool.Remove(tx.Hash);
                }
            }
            if (PersistCompleted != null) PersistCompleted(this, block);
        }

        public static Blockchain RegisterBlockchain(Blockchain blockchain)
        {
            if (blockchain == null) throw new ArgumentNullException();
            if (Default != null) Default.Dispose();
            Default = blockchain;
            return blockchain;
        }
    }
}
