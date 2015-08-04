using AntShares.Cryptography;
using AntShares.IO;
using AntShares.IO.Caching;
using AntShares.Network;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AntShares.Core
{
    public class Blockchain : IDisposable
    {
        //TODO: 备用矿工未来要有5-7个
        public static readonly ECCPublicKey[] StandbyMiners =
        {
            "02c4a2fd44a0d80d84ea3258eaf7c3c2c9f5d22369dbbe5dafdcf4ead89f7fbdd0".HexToBytes().ToPublicKey()
        };
        public static readonly Block GenesisBlock = "00000000000000000000000000000000000000000000000000000000000000000000000029ba35a638459492f5a4361ce3592da35b06478934e6e3e8f0aa15770f63fee91abaaf55000000001dac2b7ceea34400951bc0e31a530ce8a8a63485c62711476740d3f679e56fedccf5f950b3b8bee180c902fecf74141c2527d1ab3b21519253777b2e75c5307809c32b4b161e10ee271baede571822a2d261d280deed65070d2525512102c4a2fd44a0d80d84ea3258eaf7c3c2c9f5d22369dbbe5dafdcf4ead89f7fbdd051ae0200000000000000004000455b7b276c616e67273a277a682d434e272c276e616d65273a27e5b08fe89a81e882a1277d2c7b276c616e67273a27656e272c276e616d65273a27416e745368617265277d5d0000c16ff2862300eea34400951bc0e31a530ce8a8a63485c6271147eea34400951bc0e31a530ce8a8a63485c6271147000001674028952df9ca0a4e9198ade8fd8d75c3a383702203a4f2ea8e0e0785006f2525d748d8cd713fa2e53e3f57f9c4939153829846a5288cedaada81db8b7068ab428925512102c4a2fd44a0d80d84ea3258eaf7c3c2c9f5d22369dbbe5dafdcf4ead89f7fbdd051ae".HexToBytes().AsSerializable<Block>();
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
            Scripts = new byte[0][]
        };

        //TODO: 是否应该根据内存大小来优化缓存容量？
        private static BlockCache cache = new BlockCache(5760);

        public virtual BlockchainAbility Ability => BlockchainAbility.None;

        public static Blockchain Default { get; private set; } = new Blockchain();

        public virtual bool IsReadOnly => true;

        protected Blockchain()
        {
            LocalNode.NewBlock += LocalNode_NewBlock;
        }

        public virtual bool ContainsBlock(UInt256 hash)
        {
            return hash == GenesisBlock.Hash || cache.Contains(hash);
        }

        public virtual bool ContainsTransaction(UInt256 hash)
        {
            return hash == AntCoin.Hash || GenesisBlock.Transactions.Any(p => p.Hash == hash);
        }

        public bool ContainsUnspent(TransactionInput input)
        {
            return ContainsUnspent(input.PrevTxId, input.PrevIndex);
        }

        public virtual bool ContainsUnspent(UInt256 hash, ushort index)
        {
            throw new NotSupportedException();
        }

        public virtual void Dispose()
        {
            LocalNode.NewBlock -= LocalNode_NewBlock;
        }

        public virtual IEnumerable<RegisterTransaction> GetAssets()
        {
            throw new NotSupportedException();
        }

        public virtual Block GetBlock(UInt256 hash)
        {
            if (hash == GenesisBlock.Hash)
                return GenesisBlock;
            lock (cache.SyncRoot)
            {
                if (cache.Contains(hash))
                    return cache[hash];
            }
            return null;
        }

        public virtual IEnumerable<EnrollmentTransaction> GetEnrollments()
        {
            throw new NotSupportedException();
        }

        public static int GetMinSignatureCount(int miner_count)
        {
            return miner_count / 2 + 1;
        }

        public virtual Block GetNextBlock(UInt256 hash)
        {
            return null;
        }

        public virtual Fixed8 GetQuantityIssued(UInt256 asset_id)
        {
            throw new NotSupportedException();
        }

        public virtual Transaction GetTransaction(UInt256 hash)
        {
            if (hash == AntCoin.Hash)
                return AntCoin;
            return GenesisBlock.Transactions.FirstOrDefault(p => p.Hash == hash);
        }

        public virtual TransactionOutput GetUnspent(UInt256 hash, ushort index)
        {
            throw new NotSupportedException();
        }

        public virtual IEnumerable<TransactionOutput> GetUnspentAntShares()
        {
            throw new NotSupportedException();
        }

        public virtual IEnumerable<Vote> GetVotes()
        {
            throw new NotSupportedException();
        }

        public virtual bool IsDoubleSpend(Transaction tx)
        {
            throw new NotSupportedException();
        }

        private void LocalNode_NewBlock(object sender, Block block)
        {
            OnBlock(block);
        }

        protected virtual void OnBlock(Block block)
        {
            cache.Add(block);
        }

        public static void RegisterBlockchain(Blockchain blockchain)
        {
            if (blockchain == null) throw new ArgumentNullException();
            if (Default != null)
                Default.Dispose();
            Default = blockchain;
        }
    }
}
