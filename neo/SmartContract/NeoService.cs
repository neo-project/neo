using Neo.Cryptography.ECC;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract.Enumerators;
using Neo.SmartContract.Iterators;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.IO;
using System.Linq;
using System.Text;
using VMArray = Neo.VM.Types.Array;

namespace Neo.SmartContract
{
    public class NeoService : StandardService
    {
        public NeoService(TriggerType trigger, Snapshot snapshot)
            : base(trigger, snapshot)
        {
            Register("Neo.Runtime.GetTrigger", Runtime_GetTrigger);
            Register("Neo.Runtime.CheckWitness", Runtime_CheckWitness);
            Register("Neo.Runtime.Notify", Runtime_Notify);
            Register("Neo.Runtime.Log", Runtime_Log);
            Register("Neo.Runtime.GetTime", Runtime_GetTime);
            Register("Neo.Runtime.Serialize", Runtime_Serialize);
            Register("Neo.Runtime.Deserialize", Runtime_Deserialize);
            Register("Neo.Blockchain.GetHeight", Blockchain_GetHeight);
            Register("Neo.Blockchain.GetHeader", Blockchain_GetHeader);
            Register("Neo.Blockchain.GetBlock", Blockchain_GetBlock);
            Register("Neo.Blockchain.GetTransaction", Blockchain_GetTransaction);
            Register("Neo.Blockchain.GetTransactionHeight", Blockchain_GetTransactionHeight);
            Register("Neo.Blockchain.GetAccount", Blockchain_GetAccount);
            Register("Neo.Blockchain.GetValidators", Blockchain_GetValidators);
            Register("Neo.Blockchain.GetAsset", Blockchain_GetAsset);
            Register("Neo.Blockchain.GetContract", Blockchain_GetContract);
            Register("Neo.Header.GetHash", Header_GetHash);
            Register("Neo.Header.GetVersion", Header_GetVersion);
            Register("Neo.Header.GetPrevHash", Header_GetPrevHash);
            Register("Neo.Header.GetMerkleRoot", Header_GetMerkleRoot);
            Register("Neo.Header.GetTimestamp", Header_GetTimestamp);
            Register("Neo.Header.GetIndex", Header_GetIndex);
            Register("Neo.Header.GetConsensusData", Header_GetConsensusData);
            Register("Neo.Header.GetNextConsensus", Header_GetNextConsensus);
            Register("Neo.Block.GetTransactionCount", Block_GetTransactionCount);
            Register("Neo.Block.GetTransactions", Block_GetTransactions);
            Register("Neo.Block.GetTransaction", Block_GetTransaction);
            Register("Neo.Transaction.GetHash", Transaction_GetHash);
            Register("Neo.Transaction.GetType", Transaction_GetType);
            Register("Neo.Transaction.GetAttributes", Transaction_GetAttributes);
            Register("Neo.Transaction.GetInputs", Transaction_GetInputs);
            Register("Neo.Transaction.GetOutputs", Transaction_GetOutputs);
            Register("Neo.Transaction.GetReferences", Transaction_GetReferences);
            Register("Neo.Transaction.GetUnspentCoins", Transaction_GetUnspentCoins);
            Register("Neo.Transaction.GetWitnesses", Transaction_GetWitnesses);
            Register("Neo.InvocationTransaction.GetScript", InvocationTransaction_GetScript);
            Register("Neo.Witness.GetVerificationScript", Witness_GetVerificationScript);
            Register("Neo.Attribute.GetUsage", Attribute_GetUsage);
            Register("Neo.Attribute.GetData", Attribute_GetData);
            Register("Neo.Input.GetHash", Input_GetHash);
            Register("Neo.Input.GetIndex", Input_GetIndex);
            Register("Neo.Output.GetAssetId", Output_GetAssetId);
            Register("Neo.Output.GetValue", Output_GetValue);
            Register("Neo.Output.GetScriptHash", Output_GetScriptHash);
            Register("Neo.Account.GetScriptHash", Account_GetScriptHash);
            Register("Neo.Account.GetVotes", Account_GetVotes);
            Register("Neo.Account.GetBalance", Account_GetBalance);
            Register("Neo.Account.IsStandard", Account_IsStandard);
            Register("Neo.Asset.Create", Asset_Create);
            Register("Neo.Asset.Renew", Asset_Renew);
            Register("Neo.Asset.GetAssetId", Asset_GetAssetId);
            Register("Neo.Asset.GetAssetType", Asset_GetAssetType);
            Register("Neo.Asset.GetAmount", Asset_GetAmount);
            Register("Neo.Asset.GetAvailable", Asset_GetAvailable);
            Register("Neo.Asset.GetPrecision", Asset_GetPrecision);
            Register("Neo.Asset.GetOwner", Asset_GetOwner);
            Register("Neo.Asset.GetAdmin", Asset_GetAdmin);
            Register("Neo.Asset.GetIssuer", Asset_GetIssuer);
            Register("Neo.Contract.Create", Contract_Create);
            Register("Neo.Contract.Migrate", Contract_Migrate);
            Register("Neo.Contract.Destroy", Contract_Destroy);
            Register("Neo.Contract.GetScript", Contract_GetScript);
            Register("Neo.Contract.IsPayable", Contract_IsPayable);
            Register("Neo.Contract.GetStorageContext", Contract_GetStorageContext);
            Register("Neo.Storage.GetContext", Storage_GetContext);
            Register("Neo.Storage.GetReadOnlyContext", Storage_GetReadOnlyContext);
            Register("Neo.Storage.Get", Storage_Get);
            Register("Neo.Storage.Put", Storage_Put);
            Register("Neo.Storage.Delete", Storage_Delete);
            Register("Neo.Storage.Find", Storage_Find);
            Register("Neo.StorageContext.AsReadOnly", StorageContext_AsReadOnly);
            Register("Neo.Enumerator.Create", Enumerator_Create);
            Register("Neo.Enumerator.Next", Enumerator_Next);
            Register("Neo.Enumerator.Value", Enumerator_Value);
            Register("Neo.Enumerator.Concat", Enumerator_Concat);
            Register("Neo.Iterator.Create", Iterator_Create);
            Register("Neo.Iterator.Key", Iterator_Key);
            Register("Neo.Iterator.Keys", Iterator_Keys);
            Register("Neo.Iterator.Values", Iterator_Values);

            #region Aliases
            Register("Neo.Iterator.Next", Enumerator_Next);
            Register("Neo.Iterator.Value", Enumerator_Value);
            #endregion

            #region Old APIs
            Register("AntShares.Runtime.CheckWitness", Runtime_CheckWitness);
            Register("AntShares.Runtime.Notify", Runtime_Notify);
            Register("AntShares.Runtime.Log", Runtime_Log);
            Register("AntShares.Blockchain.GetHeight", Blockchain_GetHeight);
            Register("AntShares.Blockchain.GetHeader", Blockchain_GetHeader);
            Register("AntShares.Blockchain.GetBlock", Blockchain_GetBlock);
            Register("AntShares.Blockchain.GetTransaction", Blockchain_GetTransaction);
            Register("AntShares.Blockchain.GetAccount", Blockchain_GetAccount);
            Register("AntShares.Blockchain.GetValidators", Blockchain_GetValidators);
            Register("AntShares.Blockchain.GetAsset", Blockchain_GetAsset);
            Register("AntShares.Blockchain.GetContract", Blockchain_GetContract);
            Register("AntShares.Header.GetHash", Header_GetHash);
            Register("AntShares.Header.GetVersion", Header_GetVersion);
            Register("AntShares.Header.GetPrevHash", Header_GetPrevHash);
            Register("AntShares.Header.GetMerkleRoot", Header_GetMerkleRoot);
            Register("AntShares.Header.GetTimestamp", Header_GetTimestamp);
            Register("AntShares.Header.GetConsensusData", Header_GetConsensusData);
            Register("AntShares.Header.GetNextConsensus", Header_GetNextConsensus);
            Register("AntShares.Block.GetTransactionCount", Block_GetTransactionCount);
            Register("AntShares.Block.GetTransactions", Block_GetTransactions);
            Register("AntShares.Block.GetTransaction", Block_GetTransaction);
            Register("AntShares.Transaction.GetHash", Transaction_GetHash);
            Register("AntShares.Transaction.GetType", Transaction_GetType);
            Register("AntShares.Transaction.GetAttributes", Transaction_GetAttributes);
            Register("AntShares.Transaction.GetInputs", Transaction_GetInputs);
            Register("AntShares.Transaction.GetOutputs", Transaction_GetOutputs);
            Register("AntShares.Transaction.GetReferences", Transaction_GetReferences);
            Register("AntShares.Attribute.GetUsage", Attribute_GetUsage);
            Register("AntShares.Attribute.GetData", Attribute_GetData);
            Register("AntShares.Input.GetHash", Input_GetHash);
            Register("AntShares.Input.GetIndex", Input_GetIndex);
            Register("AntShares.Output.GetAssetId", Output_GetAssetId);
            Register("AntShares.Output.GetValue", Output_GetValue);
            Register("AntShares.Output.GetScriptHash", Output_GetScriptHash);
            Register("AntShares.Account.GetScriptHash", Account_GetScriptHash);
            Register("AntShares.Account.GetVotes", Account_GetVotes);
            Register("AntShares.Account.GetBalance", Account_GetBalance);
            Register("AntShares.Asset.Create", Asset_Create);
            Register("AntShares.Asset.Renew", Asset_Renew);
            Register("AntShares.Asset.GetAssetId", Asset_GetAssetId);
            Register("AntShares.Asset.GetAssetType", Asset_GetAssetType);
            Register("AntShares.Asset.GetAmount", Asset_GetAmount);
            Register("AntShares.Asset.GetAvailable", Asset_GetAvailable);
            Register("AntShares.Asset.GetPrecision", Asset_GetPrecision);
            Register("AntShares.Asset.GetOwner", Asset_GetOwner);
            Register("AntShares.Asset.GetAdmin", Asset_GetAdmin);
            Register("AntShares.Asset.GetIssuer", Asset_GetIssuer);
            Register("AntShares.Contract.Create", Contract_Create);
            Register("AntShares.Contract.Migrate", Contract_Migrate);
            Register("AntShares.Contract.Destroy", Contract_Destroy);
            Register("AntShares.Contract.GetScript", Contract_GetScript);
            Register("AntShares.Contract.GetStorageContext", Contract_GetStorageContext);
            Register("AntShares.Storage.GetContext", Storage_GetContext);
            Register("AntShares.Storage.Get", Storage_Get);
            Register("AntShares.Storage.Put", Storage_Put);
            Register("AntShares.Storage.Delete", Storage_Delete);
            #endregion
        }

        private bool Blockchain_GetAccount(ExecutionEngine engine)
        {
            UInt160 hash = new UInt160(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            AccountState account = Snapshot.Accounts.GetOrAdd(hash, () => new AccountState(hash));
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(account));
            return true;
        }

        private bool Blockchain_GetValidators(ExecutionEngine engine)
        {
            ECPoint[] validators = Snapshot.GetValidators();
            engine.CurrentContext.EvaluationStack.Push(validators.Select(p => (StackItem)p.EncodePoint(true)).ToArray());
            return true;
        }

        private bool Blockchain_GetAsset(ExecutionEngine engine)
        {
            UInt256 hash = new UInt256(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            AssetState asset = Snapshot.Assets.TryGet(hash);
            if (asset == null) return false;
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(asset));
            return true;
        }

        private bool Header_GetVersion(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                BlockBase header = _interface.GetInterface<BlockBase>();
                if (header == null) return false;
                engine.CurrentContext.EvaluationStack.Push(header.Version);
                return true;
            }
            return false;
        }

        private bool Header_GetMerkleRoot(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                BlockBase header = _interface.GetInterface<BlockBase>();
                if (header == null) return false;
                engine.CurrentContext.EvaluationStack.Push(header.MerkleRoot.ToArray());
                return true;
            }
            return false;
        }

        private bool Header_GetConsensusData(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                BlockBase header = _interface.GetInterface<BlockBase>();
                if (header == null) return false;
                engine.CurrentContext.EvaluationStack.Push(header.ConsensusData);
                return true;
            }
            return false;
        }

        private bool Header_GetNextConsensus(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                BlockBase header = _interface.GetInterface<BlockBase>();
                if (header == null) return false;
                engine.CurrentContext.EvaluationStack.Push(header.NextConsensus.ToArray());
                return true;
            }
            return false;
        }

        private bool Transaction_GetType(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                Transaction tx = _interface.GetInterface<Transaction>();
                if (tx == null) return false;
                engine.CurrentContext.EvaluationStack.Push((int)tx.Type);
                return true;
            }
            return false;
        }

        private bool Transaction_GetAttributes(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                Transaction tx = _interface.GetInterface<Transaction>();
                if (tx == null) return false;
                if (tx.Attributes.Length > ApplicationEngine.MaxArraySize)
                    return false;
                engine.CurrentContext.EvaluationStack.Push(tx.Attributes.Select(p => StackItem.FromInterface(p)).ToArray());
                return true;
            }
            return false;
        }

        private bool Transaction_GetInputs(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                Transaction tx = _interface.GetInterface<Transaction>();
                if (tx == null) return false;
                if (tx.Inputs.Length > ApplicationEngine.MaxArraySize)
                    return false;
                engine.CurrentContext.EvaluationStack.Push(tx.Inputs.Select(p => StackItem.FromInterface(p)).ToArray());
                return true;
            }
            return false;
        }

        private bool Transaction_GetOutputs(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                Transaction tx = _interface.GetInterface<Transaction>();
                if (tx == null) return false;
                if (tx.Outputs.Length > ApplicationEngine.MaxArraySize)
                    return false;
                engine.CurrentContext.EvaluationStack.Push(tx.Outputs.Select(p => StackItem.FromInterface(p)).ToArray());
                return true;
            }
            return false;
        }

        private bool Transaction_GetReferences(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                Transaction tx = _interface.GetInterface<Transaction>();
                if (tx == null) return false;
                if (tx.Inputs.Length > ApplicationEngine.MaxArraySize)
                    return false;
                engine.CurrentContext.EvaluationStack.Push(tx.Inputs.Select(p => StackItem.FromInterface(tx.References[p])).ToArray());
                return true;
            }
            return false;
        }

        private bool Transaction_GetUnspentCoins(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                Transaction tx = _interface.GetInterface<Transaction>();
                if (tx == null) return false;
                TransactionOutput[] outputs = Snapshot.GetUnspent(tx.Hash).ToArray();
                if (outputs.Length > ApplicationEngine.MaxArraySize)
                    return false;
                engine.CurrentContext.EvaluationStack.Push(outputs.Select(p => StackItem.FromInterface(p)).ToArray());
                return true;
            }
            return false;
        }

        private bool Transaction_GetWitnesses(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                Transaction tx = _interface.GetInterface<Transaction>();
                if (tx == null) return false;
                if (tx.Witnesses.Length > ApplicationEngine.MaxArraySize)
                    return false;
                engine.CurrentContext.EvaluationStack.Push(WitnessWrapper.Create(tx, Snapshot).Select(p => StackItem.FromInterface(p)).ToArray());
                return true;
            }
            return false;
        }

        private bool InvocationTransaction_GetScript(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                InvocationTransaction tx = _interface.GetInterface<InvocationTransaction>();
                if (tx == null) return false;
                engine.CurrentContext.EvaluationStack.Push(tx.Script);
                return true;
            }
            return false;
        }

        private bool Witness_GetVerificationScript(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                WitnessWrapper witness = _interface.GetInterface<WitnessWrapper>();
                if (witness == null) return false;
                engine.CurrentContext.EvaluationStack.Push(witness.VerificationScript);
                return true;
            }
            return false;
        }


        private bool Attribute_GetUsage(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                TransactionAttribute attr = _interface.GetInterface<TransactionAttribute>();
                if (attr == null) return false;
                engine.CurrentContext.EvaluationStack.Push((int)attr.Usage);
                return true;
            }
            return false;
        }

        private bool Attribute_GetData(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                TransactionAttribute attr = _interface.GetInterface<TransactionAttribute>();
                if (attr == null) return false;
                engine.CurrentContext.EvaluationStack.Push(attr.Data);
                return true;
            }
            return false;
        }

        private bool Input_GetHash(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                CoinReference input = _interface.GetInterface<CoinReference>();
                if (input == null) return false;
                engine.CurrentContext.EvaluationStack.Push(input.PrevHash.ToArray());
                return true;
            }
            return false;
        }

        private bool Input_GetIndex(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                CoinReference input = _interface.GetInterface<CoinReference>();
                if (input == null) return false;
                engine.CurrentContext.EvaluationStack.Push((int)input.PrevIndex);
                return true;
            }
            return false;
        }

        private bool Output_GetAssetId(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                TransactionOutput output = _interface.GetInterface<TransactionOutput>();
                if (output == null) return false;
                engine.CurrentContext.EvaluationStack.Push(output.AssetId.ToArray());
                return true;
            }
            return false;
        }

        private bool Output_GetValue(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                TransactionOutput output = _interface.GetInterface<TransactionOutput>();
                if (output == null) return false;
                engine.CurrentContext.EvaluationStack.Push(output.Value.GetData());
                return true;
            }
            return false;
        }

        private bool Output_GetScriptHash(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                TransactionOutput output = _interface.GetInterface<TransactionOutput>();
                if (output == null) return false;
                engine.CurrentContext.EvaluationStack.Push(output.ScriptHash.ToArray());
                return true;
            }
            return false;
        }

        private bool Account_GetScriptHash(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AccountState account = _interface.GetInterface<AccountState>();
                if (account == null) return false;
                engine.CurrentContext.EvaluationStack.Push(account.ScriptHash.ToArray());
                return true;
            }
            return false;
        }

        private bool Account_GetVotes(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AccountState account = _interface.GetInterface<AccountState>();
                if (account == null) return false;
                engine.CurrentContext.EvaluationStack.Push(account.Votes.Select(p => (StackItem)p.EncodePoint(true)).ToArray());
                return true;
            }
            return false;
        }

        private bool Account_GetBalance(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AccountState account = _interface.GetInterface<AccountState>();
                UInt256 asset_id = new UInt256(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
                if (account == null) return false;
                Fixed8 balance = account.Balances.TryGetValue(asset_id, out Fixed8 value) ? value : Fixed8.Zero;
                engine.CurrentContext.EvaluationStack.Push(balance.GetData());
                return true;
            }
            return false;
        }

        private bool Account_IsStandard(ExecutionEngine engine)
        {
            UInt160 hash = new UInt160(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            ContractState contract = Snapshot.Contracts.TryGet(hash);
            bool isStandard = contract is null || contract.Script.IsStandardContract();
            engine.CurrentContext.EvaluationStack.Push(isStandard);
            return true;
        }

        private bool Asset_Create(ExecutionEngine engine)
        {
            if (Trigger != TriggerType.Application) return false;
            InvocationTransaction tx = (InvocationTransaction)engine.ScriptContainer;
            AssetType asset_type = (AssetType)(byte)engine.CurrentContext.EvaluationStack.Pop().GetBigInteger();
            if (!Enum.IsDefined(typeof(AssetType), asset_type) || asset_type == AssetType.CreditFlag || asset_type == AssetType.DutyFlag || asset_type == AssetType.GoverningToken || asset_type == AssetType.UtilityToken)
                return false;
            if (engine.CurrentContext.EvaluationStack.Peek().GetByteArray().Length > 1024)
                return false;
            string name = Encoding.UTF8.GetString(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            Fixed8 amount = new Fixed8((long)engine.CurrentContext.EvaluationStack.Pop().GetBigInteger());
            if (amount == Fixed8.Zero || amount < -Fixed8.Satoshi) return false;
            if (asset_type == AssetType.Invoice && amount != -Fixed8.Satoshi)
                return false;
            byte precision = (byte)engine.CurrentContext.EvaluationStack.Pop().GetBigInteger();
            if (precision > 8) return false;
            if (asset_type == AssetType.Share && precision != 0) return false;
            if (amount != -Fixed8.Satoshi && amount.GetData() % (long)Math.Pow(10, 8 - precision) != 0)
                return false;
            ECPoint owner = ECPoint.DecodePoint(engine.CurrentContext.EvaluationStack.Pop().GetByteArray(), ECCurve.Secp256r1);
            if (owner.IsInfinity) return false;
            if (!CheckWitness(engine, owner))
                return false;
            UInt160 admin = new UInt160(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            UInt160 issuer = new UInt160(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            AssetState asset = Snapshot.Assets.GetOrAdd(tx.Hash, () => new AssetState
            {
                AssetId = tx.Hash,
                AssetType = asset_type,
                Name = name,
                Amount = amount,
                Available = Fixed8.Zero,
                Precision = precision,
                Fee = Fixed8.Zero,
                FeeAddress = new UInt160(),
                Owner = owner,
                Admin = admin,
                Issuer = issuer,
                Expiration = Snapshot.Height + 1 + 2000000,
                IsFrozen = false
            });
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(asset));
            return true;
        }

        private bool Asset_Renew(ExecutionEngine engine)
        {
            if (Trigger != TriggerType.Application) return false;
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AssetState asset = _interface.GetInterface<AssetState>();
                if (asset == null) return false;
                byte years = (byte)engine.CurrentContext.EvaluationStack.Pop().GetBigInteger();
                asset = Snapshot.Assets.GetAndChange(asset.AssetId);
                if (asset.Expiration < Snapshot.Height + 1)
                    asset.Expiration = Snapshot.Height + 1;
                try
                {
                    asset.Expiration = checked(asset.Expiration + years * 2000000u);
                }
                catch (OverflowException)
                {
                    asset.Expiration = uint.MaxValue;
                }
                engine.CurrentContext.EvaluationStack.Push(asset.Expiration);
                return true;
            }
            return false;
        }

        private bool Asset_GetAssetId(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AssetState asset = _interface.GetInterface<AssetState>();
                if (asset == null) return false;
                engine.CurrentContext.EvaluationStack.Push(asset.AssetId.ToArray());
                return true;
            }
            return false;
        }

        private bool Asset_GetAssetType(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AssetState asset = _interface.GetInterface<AssetState>();
                if (asset == null) return false;
                engine.CurrentContext.EvaluationStack.Push((int)asset.AssetType);
                return true;
            }
            return false;
        }

        private bool Asset_GetAmount(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AssetState asset = _interface.GetInterface<AssetState>();
                if (asset == null) return false;
                engine.CurrentContext.EvaluationStack.Push(asset.Amount.GetData());
                return true;
            }
            return false;
        }

        private bool Asset_GetAvailable(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AssetState asset = _interface.GetInterface<AssetState>();
                if (asset == null) return false;
                engine.CurrentContext.EvaluationStack.Push(asset.Available.GetData());
                return true;
            }
            return false;
        }

        private bool Asset_GetPrecision(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AssetState asset = _interface.GetInterface<AssetState>();
                if (asset == null) return false;
                engine.CurrentContext.EvaluationStack.Push((int)asset.Precision);
                return true;
            }
            return false;
        }

        private bool Asset_GetOwner(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AssetState asset = _interface.GetInterface<AssetState>();
                if (asset == null) return false;
                engine.CurrentContext.EvaluationStack.Push(asset.Owner.EncodePoint(true));
                return true;
            }
            return false;
        }

        private bool Asset_GetAdmin(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AssetState asset = _interface.GetInterface<AssetState>();
                if (asset == null) return false;
                engine.CurrentContext.EvaluationStack.Push(asset.Admin.ToArray());
                return true;
            }
            return false;
        }

        private bool Asset_GetIssuer(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AssetState asset = _interface.GetInterface<AssetState>();
                if (asset == null) return false;
                engine.CurrentContext.EvaluationStack.Push(asset.Issuer.ToArray());
                return true;
            }
            return false;
        }

        private bool Contract_Create(ExecutionEngine engine)
        {
            if (Trigger != TriggerType.Application) return false;
            byte[] script = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
            if (script.Length > 1024 * 1024) return false;
            ContractParameterType[] parameter_list = engine.CurrentContext.EvaluationStack.Pop().GetByteArray().Select(p => (ContractParameterType)p).ToArray();
            if (parameter_list.Length > 252) return false;
            ContractParameterType return_type = (ContractParameterType)(byte)engine.CurrentContext.EvaluationStack.Pop().GetBigInteger();
            ContractPropertyState contract_properties = (ContractPropertyState)(byte)engine.CurrentContext.EvaluationStack.Pop().GetBigInteger();
            if (engine.CurrentContext.EvaluationStack.Peek().GetByteArray().Length > 252) return false;
            string name = Encoding.UTF8.GetString(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            if (engine.CurrentContext.EvaluationStack.Peek().GetByteArray().Length > 252) return false;
            string version = Encoding.UTF8.GetString(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            if (engine.CurrentContext.EvaluationStack.Peek().GetByteArray().Length > 252) return false;
            string author = Encoding.UTF8.GetString(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            if (engine.CurrentContext.EvaluationStack.Peek().GetByteArray().Length > 252) return false;
            string email = Encoding.UTF8.GetString(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            if (engine.CurrentContext.EvaluationStack.Peek().GetByteArray().Length > 65536) return false;
            string description = Encoding.UTF8.GetString(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            UInt160 hash = script.ToScriptHash();
            ContractState contract = Snapshot.Contracts.TryGet(hash);
            if (contract == null)
            {
                contract = new ContractState
                {
                    Script = script,
                    ParameterList = parameter_list,
                    ReturnType = return_type,
                    ContractProperties = contract_properties,
                    Name = name,
                    CodeVersion = version,
                    Author = author,
                    Email = email,
                    Description = description
                };
                Snapshot.Contracts.Add(hash, contract);
                ContractsCreated.Add(hash, new UInt160(engine.CurrentContext.ScriptHash));
            }
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(contract));
            return true;
        }

        private bool Contract_Migrate(ExecutionEngine engine)
        {
            if (Trigger != TriggerType.Application) return false;
            byte[] script = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
            if (script.Length > 1024 * 1024) return false;
            ContractParameterType[] parameter_list = engine.CurrentContext.EvaluationStack.Pop().GetByteArray().Select(p => (ContractParameterType)p).ToArray();
            if (parameter_list.Length > 252) return false;
            ContractParameterType return_type = (ContractParameterType)(byte)engine.CurrentContext.EvaluationStack.Pop().GetBigInteger();
            ContractPropertyState contract_properties = (ContractPropertyState)(byte)engine.CurrentContext.EvaluationStack.Pop().GetBigInteger();
            if (engine.CurrentContext.EvaluationStack.Peek().GetByteArray().Length > 252) return false;
            string name = Encoding.UTF8.GetString(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            if (engine.CurrentContext.EvaluationStack.Peek().GetByteArray().Length > 252) return false;
            string version = Encoding.UTF8.GetString(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            if (engine.CurrentContext.EvaluationStack.Peek().GetByteArray().Length > 252) return false;
            string author = Encoding.UTF8.GetString(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            if (engine.CurrentContext.EvaluationStack.Peek().GetByteArray().Length > 252) return false;
            string email = Encoding.UTF8.GetString(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            if (engine.CurrentContext.EvaluationStack.Peek().GetByteArray().Length > 65536) return false;
            string description = Encoding.UTF8.GetString(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            UInt160 hash = script.ToScriptHash();
            ContractState contract = Snapshot.Contracts.TryGet(hash);
            if (contract == null)
            {
                contract = new ContractState
                {
                    Script = script,
                    ParameterList = parameter_list,
                    ReturnType = return_type,
                    ContractProperties = contract_properties,
                    Name = name,
                    CodeVersion = version,
                    Author = author,
                    Email = email,
                    Description = description
                };
                Snapshot.Contracts.Add(hash, contract);
                ContractsCreated.Add(hash, new UInt160(engine.CurrentContext.ScriptHash));
                if (contract.HasStorage)
                {
                    foreach (var pair in Snapshot.Storages.Find(engine.CurrentContext.ScriptHash).ToArray())
                    {
                        Snapshot.Storages.Add(new StorageKey
                        {
                            ScriptHash = hash,
                            Key = pair.Key.Key
                        }, new StorageItem
                        {
                            Value = pair.Value.Value,
                            IsConstant = false
                        });
                    }
                }
            }
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(contract));
            return Contract_Destroy(engine);
        }

        private bool Contract_GetScript(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                ContractState contract = _interface.GetInterface<ContractState>();
                if (contract == null) return false;
                engine.CurrentContext.EvaluationStack.Push(contract.Script);
                return true;
            }
            return false;
        }

        private bool Contract_IsPayable(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                ContractState contract = _interface.GetInterface<ContractState>();
                if (contract == null) return false;
                engine.CurrentContext.EvaluationStack.Push(contract.Payable);
                return true;
            }
            return false;
        }

        private bool Storage_Find(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                StorageContext context = _interface.GetInterface<StorageContext>();
                if (!CheckStorageContext(context)) return false;
                byte[] prefix = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
                byte[] prefix_key;
                using (MemoryStream ms = new MemoryStream())
                {
                    int index = 0;
                    int remain = prefix.Length;
                    while (remain >= 16)
                    {
                        ms.Write(prefix, index, 16);
                        ms.WriteByte(0);
                        index += 16;
                        remain -= 16;
                    }
                    if (remain > 0)
                        ms.Write(prefix, index, remain);
                    prefix_key = context.ScriptHash.ToArray().Concat(ms.ToArray()).ToArray();
                }
                StorageIterator iterator = new StorageIterator(Snapshot.Storages.Find(prefix_key).Where(p => p.Key.Key.Take(prefix.Length).SequenceEqual(prefix)).GetEnumerator());
                engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(iterator));
                Disposables.Add(iterator);
                return true;
            }
            return false;
        }

        private bool Enumerator_Create(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is VMArray array)
            {
                IEnumerator enumerator = new ArrayWrapper(array);
                engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(enumerator));
                return true;
            }
            return false;
        }

        private bool Enumerator_Next(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                IEnumerator enumerator = _interface.GetInterface<IEnumerator>();
                engine.CurrentContext.EvaluationStack.Push(enumerator.Next());
                return true;
            }
            return false;
        }

        private bool Enumerator_Value(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                IEnumerator enumerator = _interface.GetInterface<IEnumerator>();
                engine.CurrentContext.EvaluationStack.Push(enumerator.Value());
                return true;
            }
            return false;
        }

        private bool Enumerator_Concat(ExecutionEngine engine)
        {
            if (!(engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface1)) return false;
            if (!(engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface2)) return false;
            IEnumerator first = _interface1.GetInterface<IEnumerator>();
            IEnumerator second = _interface2.GetInterface<IEnumerator>();
            IEnumerator result = new ConcatenatedEnumerator(first, second);
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(result));
            return true;
        }

        private bool Iterator_Create(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is Map map)
            {
                IIterator iterator = new MapWrapper(map);
                engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(iterator));
                return true;
            }
            return false;
        }

        private bool Iterator_Key(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                IIterator iterator = _interface.GetInterface<IIterator>();
                engine.CurrentContext.EvaluationStack.Push(iterator.Key());
                return true;
            }
            return false;
        }

        private bool Iterator_Keys(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                IIterator iterator = _interface.GetInterface<IIterator>();
                engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(new IteratorKeysWrapper(iterator)));
                return true;
            }
            return false;
        }

        private bool Iterator_Values(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                IIterator iterator = _interface.GetInterface<IIterator>();
                engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(new IteratorValuesWrapper(iterator)));
                return true;
            }
            return false;
        }
    }
}
