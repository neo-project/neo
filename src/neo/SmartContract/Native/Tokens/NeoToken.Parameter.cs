
using Neo.Ledger;
using Neo.VM;
using Neo.VM.Types;
using System.Numerics;

namespace Neo.SmartContract.Native.Tokens
{
    public partial class NeoToken
    {
        private const byte Prefix_Ecomonic = 27;

        [ContractMethod(1_00000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.Integer }, ParameterNames = new[] { "gasPerBlock" })]
        private StackItem SetGasPerBlock(ApplicationEngine engine, Array args)
        {
            if (!CheckCommitteeWitness(engine)) return false;
            BigInteger gasPerBlock = args[0].GetBigInteger();
            if (gasPerBlock < 0) return false;

            EconomicParameter economic = GetAndChangeEconomicParameter(engine);
            economic.GasPerBlock = gasPerBlock;
            return true;
        }

        [ContractMethod(1_00000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.Integer }, ParameterNames = new[] { "neoHoldersRewardRatio" })]
        private StackItem SetNeoHoldersRewardRatio(ApplicationEngine engine, Array args)
        {
            if (!CheckCommitteeWitness(engine)) return false;
            BigInteger neoHoldersRewardRatio = args[0].GetBigInteger();
            if (neoHoldersRewardRatio < 0 || neoHoldersRewardRatio > uint.MaxValue) return false;

            EconomicParameter economic = GetAndChangeEconomicParameter(engine);
            economic.NeoHoldersRewardRatio = (uint)neoHoldersRewardRatio;
            return true;
        }

        [ContractMethod(1_00000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.Integer }, ParameterNames = new[] { "committeesRewardRatio" })]
        private StackItem SetCommitteesRewardRatio(ApplicationEngine engine, Array args)
        {
            if (!CheckCommitteeWitness(engine)) return false;
            BigInteger committeesRewardRatio = args[0].GetBigInteger();
            if (committeesRewardRatio < 0 || committeesRewardRatio > uint.MaxValue) return false;

            EconomicParameter economic = GetAndChangeEconomicParameter(engine);
            economic.CommitteesRewardRatio = (uint)committeesRewardRatio;
            return true;
        }

        [ContractMethod(1_00000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.Integer }, ParameterNames = new[] { "votersRewardRatio" })]
        private StackItem SetVotersRewardRatio(ApplicationEngine engine, Array args)
        {
            if (!CheckCommitteeWitness(engine)) return false;
            BigInteger votersRewardRatio = args[0].GetBigInteger();
            if (votersRewardRatio < 0 || votersRewardRatio > uint.MaxValue) return false;

            EconomicParameter economic = GetAndChangeEconomicParameter(engine);
            economic.VotersRewardRatio = (uint)votersRewardRatio;
            return true;
        }

        [ContractMethod(0_08000000, ContractParameterType.Integer, CallFlags.AllowStates)]
        private StackItem GetGasPerBlock(ApplicationEngine engine, Array args)
        {
            return GetEconomicParameter(engine).GasPerBlock;
        }

        [ContractMethod(0_08000000, ContractParameterType.Integer, CallFlags.AllowStates)]
        private StackItem GetNeoHoldersRewardRatio(ApplicationEngine engine, Array args)
        {
            return GetEconomicParameter(engine).NeoHoldersRewardRatio;
        }

        [ContractMethod(0_08000000, ContractParameterType.Integer, CallFlags.AllowStates)]
        private StackItem GetCommitteesRewardRatio(ApplicationEngine engine, Array args)
        {
            return GetEconomicParameter(engine).CommitteesRewardRatio;
        }

        [ContractMethod(0_08000000, ContractParameterType.Integer, CallFlags.AllowStates)]
        private StackItem GetVotersRewardRatio(ApplicationEngine engine, Array args)
        {
            return GetEconomicParameter(engine).VotersRewardRatio;
        }

        private EconomicParameter GetAndChangeEconomicParameter(ApplicationEngine engine)
        {
            StorageItem storageItem = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_Ecomonic), () => new StorageItem(new EconomicParameter()));
            return storageItem.GetInteroperable<EconomicParameter>();
        }

        private EconomicParameter GetEconomicParameter(ApplicationEngine engine)
        {
            StorageItem storageItem = engine.Snapshot.Storages.TryGet(CreateStorageKey(Prefix_Ecomonic));
            return storageItem.GetInteroperable<EconomicParameter>();
        }
    }
}
