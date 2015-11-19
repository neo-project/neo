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
            ECPoint.DecodePoint("02c4a2fd44a0d80d84ea3258eaf7c3c2c9f5d22369dbbe5dafdcf4ead89f7fbdd0".HexToBytes(), ECCurve.Secp256r1)
        };
        public static readonly Block GenesisBlock = "00000000000000000000000000000000000000000000000000000000000000000000000069ffd4815d08ece5435a64070dff19caefac89795236f7638e3f3290d9e5f9f0375ecc55000000001dac2b7c00000000eea34400951bc0e31a530ce8a8a63485c6271147414065e19b1bd7f90c1fb26182bce24f0c840235f647efd83647988661736500546a89bb710d832bf487a26c1053c8ccb6b6fb30be9d57ccb39bd7fb098f6e1d593025512102c4a2fd44a0d80d84ea3258eaf7c3c2c9f5d22369dbbe5dafdcf4ead89f7fbdd051ae0200000000000000004000455b7b276c616e67273a277a682d434e272c276e616d65273a27e5b08fe89a81e882a1277d2c7b276c616e67273a27656e272c276e616d65273a27416e745368617265277d5d0000c16ff2862300eea34400951bc0e31a530ce8a8a63485c6271147eea34400951bc0e31a530ce8a8a63485c62711470000014140da12d7b9a3f66d3a27a160e73ffd3fdbd712eedc3262913ec93944f874ef823d2ab972ac06616c0481afd1eb9fecc379b4030c3212641f035d8ed4095ea7476a25512102c4a2fd44a0d80d84ea3258eaf7c3c2c9f5d22369dbbe5dafdcf4ead89f7fbdd051ae".HexToBytes().AsSerializable<Block>();
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
            Scripts = { }
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
