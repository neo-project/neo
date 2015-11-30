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
        //TODO: 备用矿工未来要有5-7个
        public static readonly ECPoint[] StandbyMiners =
        {
            ECPoint.DecodePoint("0327da12b5c40200e9f65569476bbff2218da4f32548ff43b6387ec1416a231ee8".HexToBytes(), ECCurve.Secp256r1),
            ECPoint.DecodePoint("026ce35b29147ad09e4afe4ec4a7319095f08198fa8babbe3c56e970b143528d22".HexToBytes(), ECCurve.Secp256r1),
            ECPoint.DecodePoint("0209e7fd41dfb5c2f8dc72eb30358ac100ea8c72da18847befe06eade68cebfcb9".HexToBytes(), ECCurve.Secp256r1),
            ECPoint.DecodePoint("039dafd8571a641058ccc832c5e2111ea39b09c0bde36050914384f7a48bce9bf9".HexToBytes(), ECCurve.Secp256r1),
            ECPoint.DecodePoint("038dddc06ce687677a53d54f096d2591ba2302068cf123c1f2d75c2dddc5425579".HexToBytes(), ECCurve.Secp256r1),
        };
        public static readonly Block GenesisBlock = "000000000000000000000000000000000000000000000000000000000000000000000000a2d8641d2262bee562fe36392e8afb91a6e6a7c95b0c0adca2ee31315e33aeed3a005456000000001dac2b7c00000000d5b21f2e11a9795a22a482f342f53634b5a8cf3afd450140c3c280069cc6ccc8c9ce2b4a5c8c4529cdb696aca1e463b6c2d03e1d76b057843df39046ef741d588ce092a881daa471ff667b6b1466fa951c7b96a057d0e09240cce222ddf0d11ae11b746be65af35f2bcfa382e400af2c247440978cb4c97107d7f9c7d4e59a6b8b2261dca20dff95e480c021314dfaa1e211969f9a0b264ed64099b3d7a9e6ba11aac2999d55b4a8f9d2b15eeefbeffa7b71e5930db3d7373485ccd95d5c73d03c4205399aa673894c156fc039b5e9199c170e6d665476cd549b408c87ee57ca99d702f98221db88c530444d740ace574c66fa28bb797978c5b58173cab064f1ac00e02e1485f86c179787b7a53ab6655590cab9787bf77c838d47406acf9fafcbd2f68c4cc8f7e74bb404aadba37509747c1f05c78762d517b3acc5007016644448e48ef7ec195a9e1e10bc17732166ba4fb7ca73ec41983cc04278ad53210209e7fd41dfb5c2f8dc72eb30358ac100ea8c72da18847befe06eade68cebfcb9210327da12b5c40200e9f65569476bbff2218da4f32548ff43b6387ec1416a231ee821026ce35b29147ad09e4afe4ec4a7319095f08198fa8babbe3c56e970b143528d2221038dddc06ce687677a53d54f096d2591ba2302068cf123c1f2d75c2dddc542557921039dafd8571a641058ccc832c5e2111ea39b09c0bde36050914384f7a48bce9bf955ae0200000000000000004000455b7b276c616e67273a277a682d434e272c276e616d65273a27e5b08fe89a81e882a1277d2c7b276c616e67273a27656e272c276e616d65273a27416e745368617265277d5d0000c16ff2862300d5b21f2e11a9795a22a482f342f53634b5a8cf3ad5b21f2e11a9795a22a482f342f53634b5a8cf3a000001fd450140b6214ee8aad8f93ae79523679b5d654233a5de3f351876629ea6a3fbf67668c9685fab9b7e600807752b48cf898f257c4dd1bdb6092a12f4c0a2bc79c9f1339a403383ef6a23e76a9531d0830966bf879e6a7ec8c9698346e2e9016d01b8b99b3e626fdf4b63dbfaf1313a244d7108665707f35593266a77bfbe535d9ce6e7666d40a96e099bb886cad6440e0288f3d801133b612cbea5709a7ab1aa27f692e356928e2f4c7b431344fcbab1319d7bdbb4913c22efff976720ccd82cdf143225ce184074a7eeeb645e8cfdd566eeec560b25218c64263901fa6522869b19d5b344d79b01f1ed9b0d9d6cbebf96e4d671d142bfb7a080722bdcf39f878d4cbf38b7ca34403ac0f588fd5b76356ead4d248fe11813ad2a8653bf14f242edb444660b79beda7de40b7110abcc0d4c89d1a5c57e911fd2337ef0ed8b1ec1090eb4062080f19dad53210209e7fd41dfb5c2f8dc72eb30358ac100ea8c72da18847befe06eade68cebfcb9210327da12b5c40200e9f65569476bbff2218da4f32548ff43b6387ec1416a231ee821026ce35b29147ad09e4afe4ec4a7319095f08198fa8babbe3c56e970b143528d2221038dddc06ce687677a53d54f096d2591ba2302068cf123c1f2d75c2dddc542557921039dafd8571a641058ccc832c5e2111ea39b09c0bde36050914384f7a48bce9bf955ae".HexToBytes().AsSerializable<Block>();
        public static readonly RegisterTransaction AntShare = (RegisterTransaction)GenesisBlock.Transactions[1];
        public static readonly RegisterTransaction AntCoin = new RegisterTransaction
        {
            AssetType = AssetType.AntCoin,
            Name = "[{'lang':'zh-CN','name':'小蚁币'},{'lang':'en','name':'AntCoin'}]",
            Amount = Fixed8.FromDecimal(100000000),
            Issuer = new UInt160(),
            Admin = new UInt160(),
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
            return Contract.CreateMultiSigRedeemScript(miners.Length / 2 + 1, miners).ToScriptHash();
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
