using Neo.Core;
using Neo.Cryptography.ECC;
using Neo.IO.Caching;
using System;
using System.Collections.Generic;

namespace Neo.UnitTests
{
    public class TestBlockchain : Blockchain
    {
        private UInt256 _assetId;

        public TestBlockchain(UInt256 assetId)
        {
            _assetId = assetId;
        }

        public override UInt256 CurrentBlockHash => throw new NotImplementedException();

        public override UInt256 CurrentHeaderHash => throw new NotImplementedException();

        public override uint HeaderHeight => throw new NotImplementedException();

        public override uint Height => throw new NotImplementedException();

        public override bool AddBlock(Block block)
        {
            throw new NotImplementedException();
        }

        public override bool ContainsBlock(UInt256 hash)
        {
            return true; // for verify in UT_Block
        }

        public override bool ContainsTransaction(UInt256 hash)
        {
            throw new NotImplementedException();
        }

        public override bool ContainsUnspent(UInt256 hash, ushort index)
        {
            throw new NotImplementedException();
        }

        public override DataCache<TKey, TValue> CreateCache<TKey, TValue>()
        {
            throw new NotImplementedException();
        }

        public override void Dispose()
        {
            // do nothing
        }

        public override AccountState GetAccountState(UInt160 script_hash)
        {
            throw new NotImplementedException();
        }

        public override AssetState GetAssetState(UInt256 asset_id)
        {
            if (asset_id == UInt256.Zero) return null;
            UInt160 val = new UInt160(TestUtils.GetByteArray(20, asset_id.ToArray()[0]));
            return new AssetState() { Issuer = val };
        }

        public override Block GetBlock(UInt256 hash)
        {
            throw new NotImplementedException();
        }

        public override UInt256 GetBlockHash(uint height)
        {
            throw new NotImplementedException();
        }

        public override ContractState GetContract(UInt160 hash)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<ValidatorState> GetEnrollments()
        {
            ECPoint ecp = TestUtils.StandbyValidators[0];
            return new ValidatorState[] { new ValidatorState() { PublicKey = ecp } };
        }

        public override Header GetHeader(uint height)
        {
            throw new NotImplementedException();
        }

        public override Header GetHeader(UInt256 hash)
        {
            throw new NotImplementedException();
        }

        public override Block GetNextBlock(UInt256 hash)
        {
            throw new NotImplementedException();
        }

        public override UInt256 GetNextBlockHash(UInt256 hash)
        {
            throw new NotImplementedException();
        }

        public override StorageItem GetStorageItem(StorageKey key)
        {
            throw new NotImplementedException();
        }

        public override long GetSysFeeAmount(UInt256 hash)
        {
            throw new NotImplementedException();
        }

        public override Transaction GetTransaction(UInt256 hash, out int height)
        {
            height = 0;
            // take part of the trans hash and use that for the scripthash of the testtransaction
            return new TestTransaction(_assetId, TransactionType.ClaimTransaction, new UInt160(TestUtils.GetByteArray(20,hash.ToArray()[0])));
        }

        public override Dictionary<ushort, SpentCoin> GetUnclaimed(UInt256 hash)
        {
            throw new NotImplementedException();
        }

        public override TransactionOutput GetUnspent(UInt256 hash, ushort index)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<VoteState> GetVotes(IEnumerable<Transaction> others)
        {
            VoteState vs = new VoteState() { Count = Fixed8.FromDecimal(1), PublicKeys = TestUtils.StandbyValidators};            
            return new VoteState[]
            {
                vs
            };
        }

        public override bool IsDoubleSpend(Transaction tx)
        {
            throw new NotImplementedException();
        }

        protected override void AddHeaders(IEnumerable<Header> headers)
        {
            throw new NotImplementedException();
        }
    }
}
