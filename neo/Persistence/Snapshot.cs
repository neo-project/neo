using Neo.Cryptography.ECC;
using Neo.IO.Caching;
using Neo.IO.Wrappers;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Persistence
{
    public abstract class Snapshot : IDisposable, IPersistence
    {
        public Block PersistingBlock { get; internal set; }
        public abstract DataCache<UInt256, BlockState> Blocks { get; }
        public abstract DataCache<UInt256, TransactionState> Transactions { get; }
        public abstract DataCache<UInt160, AccountState> Accounts { get; }
        public abstract DataCache<UInt256, UnspentCoinState> UnspentCoins { get; }
        public abstract DataCache<ECPoint, ValidatorState> Validators { get; }
        public abstract DataCache<UInt256, AssetState> Assets { get; }
        public abstract DataCache<UInt160, ContractState> Contracts { get; }
        public abstract DataCache<StorageKey, StorageItem> Storages { get; }
        public abstract DataCache<UInt32Wrapper, HeaderHashList> HeaderHashList { get; }
        public abstract MetaDataCache<ValidatorsCountState> ValidatorsCount { get; }
        public abstract MetaDataCache<HashIndexState> BlockHashIndex { get; }
        public abstract MetaDataCache<HashIndexState> HeaderHashIndex { get; }

        public uint Height => BlockHashIndex.Get().Index;
        public uint HeaderHeight => HeaderHashIndex.Get().Index;
        public UInt256 CurrentBlockHash => BlockHashIndex.Get().Hash;
        public UInt256 CurrentHeaderHash => HeaderHashIndex.Get().Hash;

        public Snapshot Clone()
        {
            return new CloneSnapshot(this);
        }

        public virtual void Commit()
        {
            Accounts.DeleteWhere((k, v) => !v.IsFrozen && v.Votes.Length == 0 && v.Balances.All(p => p.Value <= Fixed8.Zero));
            UnspentCoins.DeleteWhere((k, v) => v.Items.All(p => p.HasFlag(CoinState.Spent)));
            Blocks.Commit();
            Transactions.Commit();
            Accounts.Commit();
            UnspentCoins.Commit();
            Validators.Commit();
            Assets.Commit();
            Contracts.Commit();
            Storages.Commit();
            HeaderHashList.Commit();
            ValidatorsCount.Commit();
            BlockHashIndex.Commit();
            HeaderHashIndex.Commit();
        }

        public virtual void Dispose()
        {
        }

        private ECPoint[] _validators = null;
        public ECPoint[] GetValidators()
        {
            if (_validators == null)
            {
                _validators = GetValidators(Enumerable.Empty<Transaction>()).ToArray();
            }
            return _validators;
        }

        public IEnumerable<ECPoint> GetValidators(IEnumerable<Transaction> others)
        {
            Snapshot snapshot = Clone();
            foreach (Transaction tx in others)
            {
                foreach (TransactionOutput output in tx.Outputs)
                {
                    AccountState account = snapshot.Accounts.GetAndChange(output.ScriptHash, () => new AccountState(output.ScriptHash));
                    if (account.Balances.ContainsKey(output.AssetId))
                        account.Balances[output.AssetId] += output.Value;
                    else
                        account.Balances[output.AssetId] = output.Value;
                    if (output.AssetId.Equals(Blockchain.GoverningToken.Hash) && account.Votes.Length > 0)
                    {
                        foreach (ECPoint pubkey in account.Votes)
                            snapshot.Validators.GetAndChange(pubkey, () => new ValidatorState(pubkey)).Votes += output.Value;
                        snapshot.ValidatorsCount.GetAndChange().Votes[account.Votes.Length - 1] += output.Value;
                    }
                }
                foreach (var group in tx.Inputs.GroupBy(p => p.PrevHash))
                {
                    Transaction tx_prev = snapshot.GetTransaction(group.Key);
                    foreach (CoinReference input in group)
                    {
                        TransactionOutput out_prev = tx_prev.Outputs[input.PrevIndex];
                        AccountState account = snapshot.Accounts.GetAndChange(out_prev.ScriptHash);
                        if (out_prev.AssetId.Equals(Blockchain.GoverningToken.Hash))
                        {
                            if (account.Votes.Length > 0)
                            {
                                foreach (ECPoint pubkey in account.Votes)
                                {
                                    ValidatorState validator = snapshot.Validators.GetAndChange(pubkey);
                                    validator.Votes -= out_prev.Value;
                                    if (!validator.Registered && validator.Votes.Equals(Fixed8.Zero))
                                        snapshot.Validators.Delete(pubkey);
                                }
                                snapshot.ValidatorsCount.GetAndChange().Votes[account.Votes.Length - 1] -= out_prev.Value;
                            }
                        }
                        account.Balances[out_prev.AssetId] -= out_prev.Value;
                    }
                }
                if (tx is StateTransaction tx_state)
                {
                    foreach (StateDescriptor descriptor in tx_state.Descriptors)
                        switch (descriptor.Type)
                        {
                            case StateType.Account:
                                Blockchain.ProcessAccountStateDescriptor(descriptor, snapshot);
                                break;
                            case StateType.Validator:
                                Blockchain.ProcessValidatorStateDescriptor(descriptor, snapshot);
                                break;
                        }
                }
            }
            int count = (int)snapshot.ValidatorsCount.Get().Votes.Select((p, i) => new
            {
                Count = i,
                Votes = p
            }).Where(p => p.Votes > Fixed8.Zero).ToArray().WeightedFilter(0.25, 0.75, p => p.Votes.GetData(), (p, w) => new
            {
                p.Count,
                Weight = w
            }).WeightedAverage(p => p.Count, p => p.Weight);
            count = Math.Max(count, Blockchain.StandbyValidators.Length);
            HashSet<ECPoint> sv = new HashSet<ECPoint>(Blockchain.StandbyValidators);
            ECPoint[] pubkeys = snapshot.Validators.Find().Select(p => p.Value).Where(p => (p.Registered && p.Votes > Fixed8.Zero) || sv.Contains(p.PublicKey)).OrderByDescending(p => p.Votes).ThenBy(p => p.PublicKey).Select(p => p.PublicKey).Take(count).ToArray();
            IEnumerable<ECPoint> result;
            if (pubkeys.Length == count)
            {
                result = pubkeys;
            }
            else
            {
                HashSet<ECPoint> hashSet = new HashSet<ECPoint>(pubkeys);
                for (int i = 0; i < Blockchain.StandbyValidators.Length && hashSet.Count < count; i++)
                    hashSet.Add(Blockchain.StandbyValidators[i]);
                result = hashSet;
            }
            return result.OrderBy(p => p);
        }
    }
}
