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

        public const uint SecondsPerBlock = 15;
        public const uint DecrementInterval = 2000000;
        public static readonly uint[] MintingAmount = { 8, 7, 6, 5, 4, 3, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
        public static readonly TimeSpan TimePerBlock = TimeSpan.FromSeconds(SecondsPerBlock);
        public static readonly ECPoint[] StandbyMiners =
        {
            ECPoint.DecodePoint("0327da12b5c40200e9f65569476bbff2218da4f32548ff43b6387ec1416a231ee8".HexToBytes(), ECCurve.Secp256r1),
            ECPoint.DecodePoint("026ce35b29147ad09e4afe4ec4a7319095f08198fa8babbe3c56e970b143528d22".HexToBytes(), ECCurve.Secp256r1),
            ECPoint.DecodePoint("0209e7fd41dfb5c2f8dc72eb30358ac100ea8c72da18847befe06eade68cebfcb9".HexToBytes(), ECCurve.Secp256r1),
            ECPoint.DecodePoint("039dafd8571a641058ccc832c5e2111ea39b09c0bde36050914384f7a48bce9bf9".HexToBytes(), ECCurve.Secp256r1),
            ECPoint.DecodePoint("038dddc06ce687677a53d54f096d2591ba2302068cf123c1f2d75c2dddc5425579".HexToBytes(), ECCurve.Secp256r1),
        };
        public static readonly Block GenesisBlock = "000000000000000000000000000000000000000000000000000000000000000000000000217777185b10cb4cc6e19b0ac5db4a9eec63b4528dd09cce486d6013a9c15b704e3ae156000000001dac2b7c00000000d5b21f2e11a9795a22a482f342f53634b5a8cf3a01000004001dac2b7c000000004000565b7b276c616e67273a277a682d434e272c276e616d65273a27e5b08fe89a81e882a128e6b58be8af9529277d2c7b276c616e67273a27656e272c276e616d65273a27416e74536861726528546573744e657429277d5d0000c16ff28623000327da12b5c40200e9f65569476bbff2218da4f32548ff43b6387ec1416a231ee8d5b21f2e11a9795a22a482f342f53634b5a8cf3a0000000241400599e1e41e9565fa3bad0848ed7c5c0d119c5080aea40d852a9b1198f02693e2c1ab5dcf8da6cae61c051bb76fe5dc30c6e1808538aa051a47e58eba656748ed23210327da12b5c40200e9f65569476bbff2218da4f32548ff43b6387ec1416a231ee8acfd450140518518dca753893395adc54f0d357281aadad1774cd8719fd134285884e91ec5bf5e91f3f5ca0ae66b2c9fc962baad7f5fff04990aaba469ad65a54bb5fb2fe540dde2d9226122cbbe708fea0346fdadeb38bcca3fc28dee378e43760893bc0207c5f5a31db99803eb66de008e0a4f1ae01435f42bbcb7a53b7ced77e613b7b9ee406e12f308480292c1ae256f0f4fa1ace755905ba5a8e777e4ebf665c3b277d48f6e54fc76317726bdd38f72bda5f9e31a6c93630e2f53d5f6a53591b27a8727e940d7de21598c18745e89020be413bdd12de18b5adfc0e91c7a9394956b3cbe70a659f6b09afdffc2b667c8bb1aa5e18f309373258d4f0b409405f665718a3ec2fc40bd2faeb173f882c5750c756b36a78dd16ba1946f04a0e9da52b97cbee709ea9a693ac1edda084bcfa6a82618bff5f544da992ef2702af0d55d5566837dbed8a0ad53210209e7fd41dfb5c2f8dc72eb30358ac100ea8c72da18847befe06eade68cebfcb9210327da12b5c40200e9f65569476bbff2218da4f32548ff43b6387ec1416a231ee821026ce35b29147ad09e4afe4ec4a7319095f08198fa8babbe3c56e970b143528d2221038dddc06ce687677a53d54f096d2591ba2302068cf123c1f2d75c2dddc542557921039dafd8571a641058ccc832c5e2111ea39b09c0bde36050914384f7a48bce9bf955ae4001555b7b276c616e67273a277a682d434e272c276e616d65273a27e5b08fe89a81e5b88128e6b58be8af9529277d2c7b276c616e67273a27656e272c276e616d65273a27416e74436f696e28546573744e657429277d5d0000c16ff28623000000000000000000000000000000000000000000000000000001159d243e000001c621a062ebda4636d889afaf7c88980094a90ae2921a6d73f4f7ef8033501b350000c16ff2862300d5b21f2e11a9795a22a482f342f53634b5a8cf3a01fd45014054e4dbfe747b5541b87b30818d0d87142c78630d3c30e87959609b84ef01fa22f8d829a770934d9e0ba53be61c558bc364a51c66ea8f51cea28e2ad2c3d3bffa40b216bf72b94da649e9d9524aedb962e635649dcf7e6cfb9055a9681d303dc4273d7496b6f0f0d3c1c7ad6c01ef4a5e3c4a792a7e7605c7103f355d3fc71273ee403ff2a3d64c5036ca9edbeafb57f71a21fa29699f69df3e3718e7abc95fd699e655492c2e634027c209666bcec1316e03d85852013e2e7a19bfddc13635022d4b406998df5d8ae667d0143242a174fea97720a99d4dbbbc037c254e54cbccabc0dc559e52781e21c67bc4836b70da17e48b487bd796cb9204194a672c1d3b0340bf400746403c53f0f83988b72436beb6d517f0ba594859d9caa1bb0c170515b6923e0adb72e50ae53ef7c44ee29961bb6c849bebabf6a18920f7b7aad42be53c7415ad53210209e7fd41dfb5c2f8dc72eb30358ac100ea8c72da18847befe06eade68cebfcb9210327da12b5c40200e9f65569476bbff2218da4f32548ff43b6387ec1416a231ee821026ce35b29147ad09e4afe4ec4a7319095f08198fa8babbe3c56e970b143528d2221038dddc06ce687677a53d54f096d2591ba2302068cf123c1f2d75c2dddc542557921039dafd8571a641058ccc832c5e2111ea39b09c0bde36050914384f7a48bce9bf955ae".HexToBytes().AsSerializable<Block>();
        public static readonly RegisterTransaction AntShare = GenesisBlock.Transactions.OfType<RegisterTransaction>().First(p => p.AssetType == AssetType.AntShare);
        public static readonly RegisterTransaction AntCoin = GenesisBlock.Transactions.OfType<RegisterTransaction>().First(p => p.AssetType == AssetType.AntCoin);
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
            return hash == AntShare.Hash || hash == AntCoin.Hash;
        }

        public virtual bool ContainsBlock(UInt256 hash)
        {
            return hash == GenesisBlock.Hash;
        }

        public virtual bool ContainsTransaction(UInt256 hash)
        {
            return GenesisBlock.Transactions.Any(p => p.Hash == hash) || MemoryPool.ContainsKey(hash);
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

        public virtual Block GetHeader(uint height)
        {
            return GetHeader(GetBlockHash(height));
        }

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

        public virtual long GetSysFeeAmount(uint height)
        {
            return GetSysFeeAmount(GetBlockHash(height));
        }

        public abstract long GetSysFeeAmount(UInt256 hash);

        public Transaction GetTransaction(UInt256 hash)
        {
            int height;
            return GetTransaction(hash, out height);
        }

        public virtual Transaction GetTransaction(UInt256 hash, out int height)
        {
            Transaction tx;
            if (MemoryPool.TryGetValue(hash, out tx))
            {
                height = -1;
                return tx;
            }
            tx = GenesisBlock.Transactions.FirstOrDefault(p => p.Hash == hash);
            if (tx != null)
            {
                height = 0;
                return tx;
            }
            height = -1;
            return null;
        }

        public abstract Dictionary<ushort, Claimable> GetUnclaimed(UInt256 hash);

        public virtual TransactionOutput GetUnspent(UInt256 hash, ushort index)
        {
            Transaction tx;
            if (!MemoryPool.TryGetValue(hash, out tx) || index >= tx.Outputs.Length)
                return null;
            return tx.Outputs[index];
        }

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
