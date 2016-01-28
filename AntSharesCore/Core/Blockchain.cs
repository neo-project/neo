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
        public const int MintingInterval = 10;
        private const int BlocksPerYear = 365 * 24 * 60 * 60 / SecondsPerBlock;
        private const double R_Init = 0.5;
        private const double R_Final = 0.3;
        public static readonly decimal GenerationFactor = 1 - (decimal)Math.Pow(R_Final / R_Init, (double)MintingInterval / BlocksPerYear);
        public static readonly TimeSpan TimePerBlock = TimeSpan.FromSeconds(SecondsPerBlock);
        public static readonly ECPoint[] StandbyMiners =
        {
            ECPoint.DecodePoint("0327da12b5c40200e9f65569476bbff2218da4f32548ff43b6387ec1416a231ee8".HexToBytes(), ECCurve.Secp256r1),
            ECPoint.DecodePoint("026ce35b29147ad09e4afe4ec4a7319095f08198fa8babbe3c56e970b143528d22".HexToBytes(), ECCurve.Secp256r1),
            ECPoint.DecodePoint("0209e7fd41dfb5c2f8dc72eb30358ac100ea8c72da18847befe06eade68cebfcb9".HexToBytes(), ECCurve.Secp256r1),
            ECPoint.DecodePoint("039dafd8571a641058ccc832c5e2111ea39b09c0bde36050914384f7a48bce9bf9".HexToBytes(), ECCurve.Secp256r1),
            ECPoint.DecodePoint("038dddc06ce687677a53d54f096d2591ba2302068cf123c1f2d75c2dddc5425579".HexToBytes(), ECCurve.Secp256r1),
        };
        public static readonly Block GenesisBlock = "0000000000000000000000000000000000000000000000000000000000000000000000003af466056e266f45fd4f929e5f6c55367b9047fb8bdd581cc77bb1dfcc40f43aa1f2a956000000001dac2b7c00000000d5b21f2e11a9795a22a482f342f53634b5a8cf3a01fd450140afa15d18d9f31dba3db565a69ebee34a09453d110fe3349257579d1c9359ff59a0e14e948141bbc5e2431dd171047f92d7ecd964b6cd4f3e5dd5854a0cd590d240e02f74fb6d8dc6283f00a3d51f1c3e252288b474da0f2b081370417740b950f07e45856b77f89524e830750364baa6cf90f4b8775da9ff720b87b75aa5989ca8405a6bbfbdcbbbf3cf47b668e1a781df9f745b5b2e9379aaf7d40c1f9fb8714d2d5dad2f72e5db2cc7646490e1545897d4820d2b54ae2c20b2b141c4cac4eaa6c6409d8d1ade01619614ade4e2e07f6a4094e5141e5a4d9995fc358d8101502edcda306b97707579800d39a16a047e67b758e57e5c384b39274815bf7bc75f98fe8d402182a064dc6831f2988834bc51c3d5db0300d19c0554923885d9ef8f38dbac4981afbbeb2d9b346d3557f10f6c988faf69f7d238aa4e4c6570e2a5629ef8143dad53210209e7fd41dfb5c2f8dc72eb30358ac100ea8c72da18847befe06eade68cebfcb9210327da12b5c40200e9f65569476bbff2218da4f32548ff43b6387ec1416a231ee821026ce35b29147ad09e4afe4ec4a7319095f08198fa8babbe3c56e970b143528d2221038dddc06ce687677a53d54f096d2591ba2302068cf123c1f2d75c2dddc542557921039dafd8571a641058ccc832c5e2111ea39b09c0bde36050914384f7a48bce9bf955ae020000000000000000004000565b7b276c616e67273a277a682d434e272c276e616d65273a27e5b08fe89a81e882a128e6b58be8af9529277d2c7b276c616e67273a27656e272c276e616d65273a27416e74536861726528546573744e657429277d5d0000c16ff28623000327da12b5c40200e9f65569476bbff2218da4f32548ff43b6387ec1416a231ee8d5b21f2e11a9795a22a482f342f53634b5a8cf3a00000002414034b5265dc400befd91f0441d8aa2ed1cad6d1fc4426c5bf72b3f9fe140ff7bd4bc386b34ac66c442ef1a216d36313fa1917a7a2f649dd6330e2403de0ca0fe7623210327da12b5c40200e9f65569476bbff2218da4f32548ff43b6387ec1416a231ee8acfd4501400e02302d5711999b8cc93e5f93b556cd7b4c4018422182201a29548c2924e1a62a65f188cdf51dc3a6b72e7357b57f101a420e09b40d6c6f2673e74065d1b036407feb69a038d0d138e055835abe10415f9d1a6155dbd8b077d87e73bcc78713f76ebc7750aa8bab484ec87f929a7b865362e0a6ceb5da9c24e0567667ea3912864059d38d920be5d774a359270ecbed7817a2ad48b3deb29fd4588563f532bf085a4c2b0372a3bc4fdc6187403e1642f61005b585df1834d863daafae3379a386a040b0ef07e761e00ede7047339690dc4632ae5d20cda49030f1141a839d9284df86ac181bc2340183d0dcc961e646a44d07732dac6a49da0dd613556e74f0edcd164090073f99a88b39ba27970ebaa858ff8ef05eed6c546f7ea6860a433efc6384c22c4afcc2ca6aa2f2bd62af9a32c0c7ae0ab9b5cbe28d1bb7469c1a1d63a9de72ad53210209e7fd41dfb5c2f8dc72eb30358ac100ea8c72da18847befe06eade68cebfcb9210327da12b5c40200e9f65569476bbff2218da4f32548ff43b6387ec1416a231ee821026ce35b29147ad09e4afe4ec4a7319095f08198fa8babbe3c56e970b143528d2221038dddc06ce687677a53d54f096d2591ba2302068cf123c1f2d75c2dddc542557921039dafd8571a641058ccc832c5e2111ea39b09c0bde36050914384f7a48bce9bf955ae".HexToBytes().AsSerializable<Block>();
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

        public Block GetBlock(uint height)
        {
            return GetBlock(GetBlockHash(height));
        }

        public virtual Block GetBlock(UInt256 hash)
        {
            if (hash == GenesisBlock.Hash)
                return GenesisBlock;
            return null;
        }

        public virtual UInt256 GetBlockHash(uint height)
        {
            if (height == 0) return GenesisBlock.Hash;
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
