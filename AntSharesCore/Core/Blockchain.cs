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
        public static event EventHandler<Block> PersistCompleted;

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
        public static readonly Block GenesisBlock = "000000000000000000000000000000000000000000000000000000000000000000000000db9d58cacbae76de3b1ff5eabb0ec099d6f0df05c1a1a6706f1782bd0a9efe8807f2f056000000001dac2b7c000000007c185b0a6ff4002b8670556429250ff73bfb100201000004001dac2b7c000000004000565b7b276c616e67273a277a682d434e272c276e616d65273a27e5b08fe89a81e882a128e6b58be8af9529277d2c7b276c616e67273a27656e272c276e616d65273a27416e74536861726528546573744e657429277d5d0000c16ff28623000327da12b5c40200e9f65569476bbff2218da4f32548ff43b6387ec1416a231ee87c185b0a6ff4002b8670556429250ff73bfb100200000002fd45014043e8effe4c2cdc8dcf3f73f3c0f3ec5c0e1d03ead26a388bde5ac4f73ef39b49f29b58aeee441893aca4f76f0b525b19c4182de0a01e57a7082779c08a43938e40d9352795311dd1ad64120bb80edead05a383db02e3439474d306dc12a30c20ed35eee81eaa9b32e514f4ac39b31316003c4e80bb1bc4d90fcbadd30f270f709a401a931ce4f2127ab35e64be4a1ad46636c142830da24032e5ec2974bb64c6da2d00e16ee637c11676e29e28b76eb15689d5e069729901749684f0df3c221d16fd40b877ca40cc979999685385465b1d57dc5364da338dc2bd267513d612e41d0a5103b63fb1f7729d80836e048c5dc2ae02107b752ecabf0c1780be8bf62285903640fbaeea240896815061f4d5081e7439e9c733da99ec31370a10df002b626796a1b9561834a0125c1f8c1a996c004448d5fc379e197d5ea5af3faa527932a3a61cad54210209e7fd41dfb5c2f8dc72eb30358ac100ea8c72da18847befe06eade68cebfcb9210327da12b5c40200e9f65569476bbff2218da4f32548ff43b6387ec1416a231ee821026ce35b29147ad09e4afe4ec4a7319095f08198fa8babbe3c56e970b143528d2221038dddc06ce687677a53d54f096d2591ba2302068cf123c1f2d75c2dddc542557921039dafd8571a641058ccc832c5e2111ea39b09c0bde36050914384f7a48bce9bf955ae41406fcf1c3927700a4928a39ae12439a3bc81c2bd02ac967bc677ffc25431c30f8a7557edd26c63b42607ccf55376199e12f9163fbb6e8b2bfc595804f2a3bf726823210327da12b5c40200e9f65569476bbff2218da4f32548ff43b6387ec1416a231ee8ac4001555b7b276c616e67273a277a682d434e272c276e616d65273a27e5b08fe89a81e5b88128e6b58be8af9529277d2c7b276c616e67273a27656e272c276e616d65273a27416e74436f696e28546573744e657429277d5d0000c16ff28623000000000000000000000000000000000000000000000000000001125bb95c0000013d2aac31e7dd5842cb1d68390c9664edafc5928263af7dbd0c6644a3c2453e2a0000c16ff28623007c185b0a6ff4002b8670556429250ff73bfb100201fd4501403626c28ba176f8e93167c82d635d17443c713aa3bfa9929d5a26e26126aa9bca72ea3a405e0eae069e554d8a6f42504f08832c64f948f500a8bdc3acdb958ef7408bf3731b28717c60d42f1bc02e87a113ad07c6c7fd6f3716c509046e69ba722fd76117a7c104c07ec766ef8feedf6939966a400c99bf3de5bbc4785b4550f89a40543f52389afccb8d9753278b26342dd438ae9c010fbb28b54118a7085aba58c6597d65499d5336cb8a294017dbb313f6c29c51eac4c09c5d05c74e4c291566d540182efab8b5ddcd839a4f348b63d2efb26a4379a94ed30ba1d072ee1c1cd70eba593ab5c0bfa4aefa579d78afae50411f8c5a04aef946f5c1ba8b4260e4d9326d404ba8283bf4f105874690345ec8523f5a183bd8935b37795a3c2754c1b23da46300a284ef780e6f7ec58faad510a03961c04ebf32542de1755fcf6280f75198d1ad54210209e7fd41dfb5c2f8dc72eb30358ac100ea8c72da18847befe06eade68cebfcb9210327da12b5c40200e9f65569476bbff2218da4f32548ff43b6387ec1416a231ee821026ce35b29147ad09e4afe4ec4a7319095f08198fa8babbe3c56e970b143528d2221038dddc06ce687677a53d54f096d2591ba2302068cf123c1f2d75c2dddc542557921039dafd8571a641058ccc832c5e2111ea39b09c0bde36050914384f7a48bce9bf955ae".HexToBytes().AsSerializable<Block>();
        public static readonly RegisterTransaction AntShare = GenesisBlock.Transactions.OfType<RegisterTransaction>().First(p => p.AssetType == AssetType.AntShare);
        public static readonly RegisterTransaction AntCoin = GenesisBlock.Transactions.OfType<RegisterTransaction>().First(p => p.AssetType == AssetType.AntCoin);

        public abstract BlockchainAbility Ability { get; }
        public abstract UInt256 CurrentBlockHash { get; }
        public virtual UInt256 CurrentHeaderHash => CurrentBlockHash;
        public static Blockchain Default { get; private set; } = null;
        public virtual uint HeaderHeight => Height;
        public abstract uint Height { get; }
        public abstract bool IsReadOnly { get; }

        protected internal abstract bool AddBlock(Block block);

        protected internal abstract void AddHeaders(IEnumerable<Block> headers);

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
            return GenesisBlock.Transactions.Any(p => p.Hash == hash);
        }

        public bool ContainsUnspent(TransactionInput input)
        {
            return ContainsUnspent(input.PrevHash, input.PrevIndex);
        }

        public abstract bool ContainsUnspent(UInt256 hash, ushort index);

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

        public static UInt160 GetMinerAddress(ECPoint[] miners)
        {
            return MultiSigContract.CreateMultiSigRedeemScript(miners.Length - (miners.Length - 1) / 3, miners).ToScriptHash();
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

        public abstract TransactionOutput GetUnspent(UInt256 hash, ushort index);

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
