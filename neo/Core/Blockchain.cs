using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.IO.Caching;
using Neo.SmartContract;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Neo.Core
{
	/// <summary>
	/// 实现区块链功能的基类
	/// Base classes that implement blockchain functionality
	/// </summary>
	public abstract class Blockchain : IDisposable, IScriptTable
    {
        public static event EventHandler<Block> PersistCompleted;
		
        public CancellationTokenSource VerificationCancellationToken { get; protected set; } = new CancellationTokenSource();
        public object PersistLock { get; } = new object();

		/// <summary>
		/// 产生每个区块的时间间隔，已秒为单位
		/// The interval at which each block is generated, in seconds
		/// </summary>
		public static readonly uint SecondsPerBlock = Settings.Default.SecondsPerBlock;
        public const uint DecrementInterval = 2000000;
        public const uint MaxValidators = 1024;
        public static readonly uint[] GenerationAmount = { 8, 7, 6, 5, 4, 3, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
		/// <summary>
		/// 产生每个区块的时间间隔
		/// Generate interval for each block
		/// </summary>
		public static readonly TimeSpan TimePerBlock = TimeSpan.FromSeconds(SecondsPerBlock);
		/// <summary>
		/// 后备记账人列表
		/// Backup booker list
		/// </summary>
		public static readonly ECPoint[] StandbyValidators = Settings.Default.StandbyValidators.OfType<string>().Select(p => ECPoint.DecodePoint(p.HexToBytes(), ECCurve.Secp256r1)).ToArray();

        /// <summary>
        /// Return true if haven't got valid handle
        /// </summary>
        public abstract bool IsDisposed { get; }

#pragma warning disable CS0612
        public static readonly RegisterTransaction GoverningToken = new RegisterTransaction
        {
            AssetType = AssetType.GoverningToken,
            Name = "[{\"lang\":\"zh-CN\",\"name\":\"小蚁股\"},{\"lang\":\"en\",\"name\":\"AntShare\"}]",
            Amount = Fixed8.FromDecimal(100000000),
            Precision = 0,
            Owner = ECCurve.Secp256r1.Infinity,
            Admin = (new[] { (byte)OpCode.PUSHT }).ToScriptHash(),
            Attributes = new TransactionAttribute[0],
            Inputs = new CoinReference[0],
            Outputs = new TransactionOutput[0],
            Scripts = new Witness[0]
        };

        public static readonly RegisterTransaction UtilityToken = new RegisterTransaction
        {
            AssetType = AssetType.UtilityToken,
            Name = "[{\"lang\":\"zh-CN\",\"name\":\"小蚁币\"},{\"lang\":\"en\",\"name\":\"AntCoin\"}]",
            Amount = Fixed8.FromDecimal(GenerationAmount.Sum(p => p) * DecrementInterval),
            Precision = 8,
            Owner = ECCurve.Secp256r1.Infinity,
            Admin = (new[] { (byte)OpCode.PUSHF }).ToScriptHash(),
            Attributes = new TransactionAttribute[0],
            Inputs = new CoinReference[0],
            Outputs = new TransactionOutput[0],
            Scripts = new Witness[0]
        };
#pragma warning restore CS0612

		/// <summary>
		/// 创世区块
		/// Genesis block
		/// </summary>
		public static readonly Block GenesisBlock = new Block
        {
            PrevHash = UInt256.Zero,
            Timestamp = (new DateTime(2016, 7, 15, 15, 8, 21, DateTimeKind.Utc)).ToTimestamp(),
            Index = 0,
            ConsensusData = 2083236893, //向比特币致敬
            NextConsensus = GetConsensusAddress(StandbyValidators),
            Script = new Witness
            {
                InvocationScript = new byte[0],
                VerificationScript = new[] { (byte)OpCode.PUSHT }
            },
            Transactions = new Transaction[]
            {
                new MinerTransaction
                {
                    Nonce = 2083236893,
                    Attributes = new TransactionAttribute[0],
                    Inputs = new CoinReference[0],
                    Outputs = new TransactionOutput[0],
                    Scripts = new Witness[0]
                },
                GoverningToken,
                UtilityToken,
                new IssueTransaction
                {
                    Attributes = new TransactionAttribute[0],
                    Inputs = new CoinReference[0],
                    Outputs = new[]
                    {
                        new TransactionOutput
                        {
                            AssetId = GoverningToken.Hash,
                            Value = GoverningToken.Amount,
                            ScriptHash = Contract.CreateMultiSigRedeemScript(StandbyValidators.Length / 2 + 1, StandbyValidators).ToScriptHash()
                        }
                    },
                    Scripts = new[]
                    {
                        new Witness
                        {
                            InvocationScript = new byte[0],
                            VerificationScript = new[] { (byte)OpCode.PUSHT }
                        }
                    }
                }
            }
        };

		/// <summary>
		/// 当前最新区块散列值
		/// Current block hash value
		/// </summary>
		public abstract UInt256 CurrentBlockHash { get; }
		/// <summary>
		/// 当前最新区块头的散列值
		/// The current block header hash value
		/// </summary>
		public abstract UInt256 CurrentHeaderHash { get; }
		/// <summary>
		/// 默认的区块链实例
		/// The default blockchain instance
		/// </summary>
		public static Blockchain Default { get; private set; } = null;
		/// <summary>
		/// 区块头高度
		/// Header height
		/// </summary>
		public abstract uint HeaderHeight { get; }
		/// <summary>
		/// 区块高度
		/// Block height
		/// </summary>
		public abstract uint Height { get; }

        static Blockchain()
        {
            GenesisBlock.RebuildMerkleRoot();
        }

		/// <summary>
		/// 将指定的区块添加到区块链中
		/// Add the specified block to the blockchain
		/// </summary>
		/// <param name="block">要添加的区块 The block to add</param>
		/// <returns>返回是否添加成功  returns whether it was added successfully</returns>
		public abstract bool AddBlock(Block block);

		/// <summary>
		/// 将指定的区块头添加到区块头链中
		/// Add the specified block header to the block header
		/// </summary>
		/// <param name="headers">要添加的区块头列表 List of headers to add</param>
		protected internal abstract void AddHeaders(IEnumerable<Header> headers);

        public static Fixed8 CalculateBonus(IEnumerable<CoinReference> inputs, bool ignoreClaimed = true)
        {
            List<SpentCoin> unclaimed = new List<SpentCoin>();
            foreach (var group in inputs.GroupBy(p => p.PrevHash))
            {
                Dictionary<ushort, SpentCoin> claimable = Default.GetUnclaimed(group.Key);
                if (claimable == null || claimable.Count == 0)
                    if (ignoreClaimed)
                        continue;
                    else
                        throw new ArgumentException();
                foreach (CoinReference claim in group)
                {
                    if (!claimable.TryGetValue(claim.PrevIndex, out SpentCoin claimed))
                        if (ignoreClaimed)
                            continue;
                        else
                            throw new ArgumentException();
                    unclaimed.Add(claimed);
                }
            }
            return CalculateBonusInternal(unclaimed);
        }

        public static Fixed8 CalculateBonus(IEnumerable<CoinReference> inputs, uint height_end)
        {
            List<SpentCoin> unclaimed = new List<SpentCoin>();
            foreach (var group in inputs.GroupBy(p => p.PrevHash))
            {
                Transaction tx = Default.GetTransaction(group.Key, out int height_start);
                if (tx == null) throw new ArgumentException();
                if (height_start == height_end) continue;
                foreach (CoinReference claim in group)
                {
                    if (claim.PrevIndex >= tx.Outputs.Length || !tx.Outputs[claim.PrevIndex].AssetId.Equals(GoverningToken.Hash))
                        throw new ArgumentException();
                    unclaimed.Add(new SpentCoin
                    {
                        Output = tx.Outputs[claim.PrevIndex],
                        StartHeight = (uint)height_start,
                        EndHeight = height_end
                    });
                }
            }
            return CalculateBonusInternal(unclaimed);
        }

        private static Fixed8 CalculateBonusInternal(IEnumerable<SpentCoin> unclaimed)
        {
            Fixed8 amount_claimed = Fixed8.Zero;
            foreach (var group in unclaimed.GroupBy(p => new { p.StartHeight, p.EndHeight }))
            {
                uint amount = 0;
                uint ustart = group.Key.StartHeight / DecrementInterval;
                if (ustart < GenerationAmount.Length)
                {
                    uint istart = group.Key.StartHeight % DecrementInterval;
                    uint uend = group.Key.EndHeight / DecrementInterval;
                    uint iend = group.Key.EndHeight % DecrementInterval;
                    if (uend >= GenerationAmount.Length)
                    {
                        uend = (uint)GenerationAmount.Length;
                        iend = 0;
                    }
                    if (iend == 0)
                    {
                        uend--;
                        iend = DecrementInterval;
                    }
                    while (ustart < uend)
                    {
                        amount += (DecrementInterval - istart) * GenerationAmount[ustart];
                        ustart++;
                        istart = 0;
                    }
                    amount += (iend - istart) * GenerationAmount[ustart];
                }
                amount += (uint)(Default.GetSysFeeAmount(group.Key.EndHeight - 1) - (group.Key.StartHeight == 0 ? 0 : Default.GetSysFeeAmount(group.Key.StartHeight - 1)));
                amount_claimed += group.Sum(p => p.Value) / 100000000 * amount;
            }
            return amount_claimed;
        }

		/// <summary>
		/// 判断区块链中是否包含指定的区块
		/// Determine if the specified block is contained in the blockchain
		/// </summary>
		/// <param name="hash">区块编号 Blcok Hash</param>
		/// <returns>如果包含指定区块则返回true returns true if the specified block is included</returns>
		public abstract bool ContainsBlock(UInt256 hash);

		/// <summary>
		/// 判断区块链中是否包含指定的交易
		/// Determine whether the specified transaction is included in the blockchain
		/// </summary>
		/// <param name="hash">交易编号 Transaction Hash</param>
		/// <returns>如果包含指定交易则返回true returns true if the specified transaction is included in the blockchain </returns>
		public abstract bool ContainsTransaction(UInt256 hash);

        public bool ContainsUnspent(CoinReference input)
        {
            return ContainsUnspent(input.PrevHash, input.PrevIndex);
        }

        public abstract bool ContainsUnspent(UInt256 hash, ushort index);

        public abstract MetaDataCache<T> GetMetaData<T>() where T : class, ISerializable, new();

        public abstract DataCache<TKey, TValue> GetStates<TKey, TValue>()
            where TKey : IEquatable<TKey>, ISerializable, new()
            where TValue : StateBase, ICloneable<TValue>, new();

        public abstract void Dispose();

        public abstract AccountState GetAccountState(UInt160 script_hash);

        public abstract AssetState GetAssetState(UInt256 asset_id);

		/// <summary>
		/// 根据指定的高度，返回对应的区块信息
		/// Return the corresponding block information based on the specified height
		/// </summary>
		/// <param name="height">区块高度 Block height</param>
		/// <returns>返回对应的区块信息 returns the corresponding block information</returns>
		public Block GetBlock(uint height)
        {
            UInt256 hash = GetBlockHash(height);
            if (hash == null) return null;
            return GetBlock(hash);
        }

		/// <summary>
		/// 根据指定的散列值，返回对应的区块信息
		/// Returns the corresponding block information according to the specified hash value
		/// </summary>
		/// <param name="hash">散列值 Block Hash</param>
		/// <returns>返回对应的区块信息 returns the corresponding block info</returns>
		public abstract Block GetBlock(UInt256 hash);

		/// <summary>
		/// 根据指定的高度，返回对应区块的散列值
		/// Returns the hash value of the corresponding block based on the specified height
		/// </summary>
		/// <param name="height">区块高度 block height</param>
		/// <returns>返回对应区块的散列值 returns the hash value of the corresponding block</returns>
		public abstract UInt256 GetBlockHash(uint height);

        public abstract ContractState GetContract(UInt160 hash);

        public abstract IEnumerable<ValidatorState> GetEnrollments();

		/// <summary>
		/// 根据指定的高度，返回对应的区块头信息
		///  Returns the corresponding block header information according to the specified height
		/// </summary>
		/// <param name="height">区块高度 block height</param>
		/// <returns>返回对应的区块头信息 returns corresponding header information</returns>
		public abstract Header GetHeader(uint height);

		/// <summary>
		/// 根据指定的散列值，返回对应的区块头信息
		/// Returns the corresponding block header information according to the specified hash value
		/// </summary>
		/// <param name="hash">散列值 hash value</param>
		/// <returns>返回对应的区块头信息</returns>
		public abstract Header GetHeader(UInt256 hash);

		/// <summary>
		/// 获取记账人的合约地址
		/// Get the contractor's contract address 
		/// </summary>
		/// <param name="validators">记账人的公钥列表 list of public keys of the booker</param>
		/// <returns>返回记账人的合约地址 returns the contractor's contract address</returns>
		public static UInt160 GetConsensusAddress(ECPoint[] validators)
        {
            return Contract.CreateMultiSigRedeemScript(validators.Length - (validators.Length - 1) / 3, validators).ToScriptHash();
        }

        private List<ECPoint> _validators = new List<ECPoint>();
		/// <summary>
		/// 获取下一个区块的记账人列表
		/// Get the list of the accounts of the next block
		/// </summary>
		/// <returns>返回一组公钥，表示下一个区块的记账人列表 returns a set of public keys representing the list of accounts for the next block</returns>
		public ECPoint[] GetValidators()
        {
            lock (_validators)
            {
                if (_validators.Count == 0)
                {
                    _validators.AddRange(GetValidators(Enumerable.Empty<Transaction>()));
                }
                return _validators.ToArray();
            }
        }

        public virtual IEnumerable<ECPoint> GetValidators(IEnumerable<Transaction> others)
        {
            DataCache<UInt160, AccountState> accounts = GetStates<UInt160, AccountState>();
            DataCache<ECPoint, ValidatorState> validators = GetStates<ECPoint, ValidatorState>();
            MetaDataCache<ValidatorsCountState> validators_count = GetMetaData<ValidatorsCountState>();
            foreach (Transaction tx in others)
            {
                foreach (TransactionOutput output in tx.Outputs)
                {
                    AccountState account = accounts.GetAndChange(output.ScriptHash, () => new AccountState(output.ScriptHash));
                    if (account.Balances.ContainsKey(output.AssetId))
                        account.Balances[output.AssetId] += output.Value;
                    else
                        account.Balances[output.AssetId] = output.Value;
                    if (output.AssetId.Equals(GoverningToken.Hash) && account.Votes.Length > 0)
                    {
                        foreach (ECPoint pubkey in account.Votes)
                            validators.GetAndChange(pubkey, () => new ValidatorState(pubkey)).Votes += output.Value;
                        validators_count.GetAndChange().Votes[account.Votes.Length - 1] += output.Value;
                    }
                }
                foreach (var group in tx.Inputs.GroupBy(p => p.PrevHash))
                {
                    Transaction tx_prev = GetTransaction(group.Key, out int height);
                    foreach (CoinReference input in group)
                    {
                        TransactionOutput out_prev = tx_prev.Outputs[input.PrevIndex];
                        AccountState account = accounts.GetAndChange(out_prev.ScriptHash);
                        if (out_prev.AssetId.Equals(GoverningToken.Hash))
                        {
                            if (account.Votes.Length > 0)
                            {
                                foreach (ECPoint pubkey in account.Votes)
                                {
                                    ValidatorState validator = validators.GetAndChange(pubkey);
                                    validator.Votes -= out_prev.Value;
                                    if (!validator.Registered && validator.Votes.Equals(Fixed8.Zero))
                                        validators.Delete(pubkey);
                                }
                                validators_count.GetAndChange().Votes[account.Votes.Length - 1] -= out_prev.Value;
                            }
                        }
                        account.Balances[out_prev.AssetId] -= out_prev.Value;
                    }
                }
                switch (tx)
                {
#pragma warning disable CS0612
                    case EnrollmentTransaction tx_enrollment:
                        validators.GetAndChange(tx_enrollment.PublicKey, () => new ValidatorState(tx_enrollment.PublicKey)).Registered = true;
                        break;
#pragma warning restore CS0612
                    case StateTransaction tx_state:
                        foreach (StateDescriptor descriptor in tx_state.Descriptors)
                            switch (descriptor.Type)
                            {
                                case StateType.Account:
                                    ProcessAccountStateDescriptor(descriptor, accounts, validators, validators_count);
                                    break;
                                case StateType.Validator:
                                    ProcessValidatorStateDescriptor(descriptor, validators);
                                    break;
                            }
                        break;
                }
            }
            int count = (int)validators_count.Get().Votes.Select((p, i) => new
            {
                Count = i,
                Votes = p
            }).Where(p => p.Votes > Fixed8.Zero).ToArray().WeightedFilter(0.25, 0.75, p => p.Votes.GetData(), (p, w) => new
            {
                p.Count,
                Weight = w
            }).WeightedAverage(p => p.Count, p => p.Weight);
            count = Math.Max(count, StandbyValidators.Length);
            HashSet<ECPoint> sv = new HashSet<ECPoint>(StandbyValidators);
            ECPoint[] pubkeys = validators.Find().Select(p => p.Value).Where(p => (p.Registered && p.Votes > Fixed8.Zero) || sv.Contains(p.PublicKey)).OrderByDescending(p => p.Votes).ThenBy(p => p.PublicKey).Select(p => p.PublicKey).Take(count).ToArray();
            IEnumerable<ECPoint> result;
            if (pubkeys.Length == count)
            {
                result = pubkeys;
            }
            else
            {
                HashSet<ECPoint> hashSet = new HashSet<ECPoint>(pubkeys);
                for (int i = 0; i < StandbyValidators.Length && hashSet.Count < count; i++)
                    hashSet.Add(StandbyValidators[i]);
                result = hashSet;
            }
            return result.OrderBy(p => p);
        }

		/// <summary>
		/// 根据指定的散列值，返回下一个区块的信息
		///  Returns the information of the next block based on the specified hash value
		/// </summary>
		/// <param name="hash">散列值 hash value</param>
		/// <returns>返回下一个区块的信息  returns information for the next block</returns>
		public abstract Block GetNextBlock(UInt256 hash);

		/// <summary>
		/// 根据指定的散列值，返回下一个区块的散列值
		/// Returns the hash value of the next block based on the specified hash value
		/// </summary>
		/// <param name="hash">散列值 hash value</param>
		/// <returns>返回下一个区块的散列值 returns the hash value of the next block</returns>
		public abstract UInt256 GetNextBlockHash(UInt256 hash);

        byte[] IScriptTable.GetScript(byte[] script_hash)
        {
            return GetContract(new UInt160(script_hash)).Script;
        }

        public abstract StorageItem GetStorageItem(StorageKey key);

		/// <summary>
		/// 根据指定的区块高度，返回对应区块及之前所有区块中包含的系统费用的总量
		///  Returns the total system cost contained in the corresponding block and all previous blocks based on the specified block heigh
		/// </summary>
		/// <param name="height">区块高度 block height</param>
		/// <returns>返回对应的系统费用的总量 returns the total amount of the corresponding system fee</returns>
		public virtual long GetSysFeeAmount(uint height)
        {
            return GetSysFeeAmount(GetBlockHash(height));
        }

		/// <summary>
		/// 根据指定的区块散列值，返回对应区块及之前所有区块中包含的系统费用的总量
		///  Returns the total amount of system costs contained in the corresponding block and all previous blocks based on the specified block hash value
		/// </summary>
		/// <param name="hash">散列值 hash value</param>
		/// <returns>返回系统费用的总量 returns the total amount of system fees</returns>
		public abstract long GetSysFeeAmount(UInt256 hash);

		/// <summary>
		/// 根据指定的散列值，返回对应的交易信息
		/// Return the corresponding transaction information based on the specified hash value
		/// </summary>
		/// <param name="hash">散列值 hash value</param>
		/// <returns>返回对应的交易信息 returns corresponding transaction information</returns>
		public Transaction GetTransaction(UInt256 hash)
        {
            return GetTransaction(hash, out _);
        }

		/// <summary>
		/// 根据指定的散列值，返回对应的交易信息与该交易所在区块的高度
		/// According to the specified hash value, return the corresponding transaction information and the height of the block where the transaction is located
		/// </summary>
		/// <param name="hash">交易散列值 Transaction hash value</param>
		/// <param name="height">返回该交易所在区块的高度 Returns the height of the block where the transaction is located</param>
		/// <returns>返回对应的交易信息 returns corresponding transaction information</returns>
		public abstract Transaction GetTransaction(UInt256 hash, out int height);

        public abstract Dictionary<ushort, SpentCoin> GetUnclaimed(UInt256 hash);

		/// <summary>
		/// 根据指定的散列值和索引，获取对应的未花费的资产
		/// Get the corresponding unspent assets based on the specified hash value and index
		/// </summary>
		/// <param name="hash">交易散列值 Transaction hash value</param>
		/// <param name="index">输出的索引 Output Index</param>
		/// <returns>返回一个交易输出，表示一个未花费的资产 returns a transaction output representing an unspent asset</returns>
		public abstract TransactionOutput GetUnspent(UInt256 hash, ushort index);

        public abstract IEnumerable<TransactionOutput> GetUnspent(UInt256 hash);

		/// <summary>
		/// 判断交易是否双花
		///  Determine if the transaction is double spend
		/// </summary>
		/// <param name="tx">交易 Transaction</param>
		/// <returns>返回交易是否双花 returns if the transaction is double spend</returns>
		public abstract bool IsDoubleSpend(Transaction tx);

		/// <summary>
		/// 当区块被写入到硬盘后调用
		/// Called after the block was written to the hard disk
		/// </summary>
		/// <param name="block">区块</param>
		protected void OnPersistCompleted(Block block)
        {
            lock (_validators)
            {
                _validators.Clear();
            }
            PersistCompleted?.Invoke(this, block);
        }

        protected void ProcessAccountStateDescriptor(StateDescriptor descriptor, DataCache<UInt160, AccountState> accounts, DataCache<ECPoint, ValidatorState> validators, MetaDataCache<ValidatorsCountState> validators_count)
        {
            UInt160 hash = new UInt160(descriptor.Key);
            AccountState account = accounts.GetAndChange(hash, () => new AccountState(hash));
            switch (descriptor.Field)
            {
                case "Votes":
                    Fixed8 balance = account.GetBalance(GoverningToken.Hash);
                    foreach (ECPoint pubkey in account.Votes)
                    {
                        ValidatorState validator = validators.GetAndChange(pubkey);
                        validator.Votes -= balance;
                        if (!validator.Registered && validator.Votes.Equals(Fixed8.Zero))
                            validators.Delete(pubkey);
                    }
                    ECPoint[] votes = descriptor.Value.AsSerializableArray<ECPoint>().Distinct().ToArray();
                    if (votes.Length != account.Votes.Length)
                    {
                        ValidatorsCountState count_state = validators_count.GetAndChange();
                        if (account.Votes.Length > 0)
                            count_state.Votes[account.Votes.Length - 1] -= balance;
                        if (votes.Length > 0)
                            count_state.Votes[votes.Length - 1] += balance;
                    }
                    account.Votes = votes;
                    foreach (ECPoint pubkey in account.Votes)
                        validators.GetAndChange(pubkey, () => new ValidatorState(pubkey)).Votes += balance;
                    break;
            }
        }

        protected void ProcessValidatorStateDescriptor(StateDescriptor descriptor, DataCache<ECPoint, ValidatorState> validators)
        {
            ECPoint pubkey = ECPoint.DecodePoint(descriptor.Key, ECCurve.Secp256r1);
            ValidatorState validator = validators.GetAndChange(pubkey, () => new ValidatorState(pubkey));
            switch (descriptor.Field)
            {
                case "Registered":
                    validator.Registered = BitConverter.ToBoolean(descriptor.Value, 0);
                    break;
            }
        }

		/// <summary>
		/// 注册默认的区块链实例
		/// Register the default blockchain instance
		/// </summary>
		/// <param name="blockchain">区块链实例 blockchain instance<</param>
		/// <returns>返回注册后的区块链实例 returns registered blockchain instances</returns>
		public static Blockchain RegisterBlockchain(Blockchain blockchain)
        {
            if (Default != null) Default.Dispose();
            Default = blockchain ?? throw new ArgumentNullException();
            return blockchain;
        }
    }
}
