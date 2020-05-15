
using Neo.Ledger;
using Neo.Persistence;
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

            EconomicParameter economic = GetAndChangeEconomicParameter(engine.Snapshot);
            economic.GasPerBlock = gasPerBlock;
            return true;
        }

        [ContractMethod(1_00000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.Integer }, ParameterNames = new[] { "neoHoldersRewardRatio" })]
        private StackItem SetNeoHoldersRewardRatio(ApplicationEngine engine, Array args)
        {
            if (!CheckCommitteeWitness(engine)) return false;
            var NeoHoldersRewardRatio = args[0].GetBigInteger();
            if (NeoHoldersRewardRatio < 0 || NeoHoldersRewardRatio > byte.MaxValue) return false;
            EconomicParameter economic = GetAndChangeEconomicParameter(engine.Snapshot);
            economic.NeoHoldersRewardRatio = (byte)NeoHoldersRewardRatio;
            return true;
        }

        [ContractMethod(1_00000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.Integer }, ParameterNames = new[] { "committeesRewardRatio" })]
        private StackItem SetCommitteesRewardRatio(ApplicationEngine engine, Array args)
        {
            if (!CheckCommitteeWitness(engine)) return false;
            var CommitteesRewardRatio = args[0].GetBigInteger();
            if (CommitteesRewardRatio < 0 || CommitteesRewardRatio > byte.MaxValue) return false;
            EconomicParameter economic = GetAndChangeEconomicParameter(engine.Snapshot);
            economic.CommitteesRewardRatio = (byte)CommitteesRewardRatio;
            return true;
        }

        [ContractMethod(1_00000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.Integer }, ParameterNames = new[] { "votersRewardRatio" })]
        private StackItem SetVotersRewardRatio(ApplicationEngine engine, Array args)
        {
            if (!CheckCommitteeWitness(engine)) return false;
            var VotersRewardRatio = args[0].GetBigInteger();
            if (VotersRewardRatio < 0 || VotersRewardRatio > byte.MaxValue) return false;
            EconomicParameter economic = GetAndChangeEconomicParameter(engine.Snapshot);
            economic.VotersRewardRatio = (byte)VotersRewardRatio;
            return true;
        }

        [ContractMethod(0_08000000, ContractParameterType.Integer, CallFlags.AllowStates)]
        private StackItem GetGasPerBlock(ApplicationEngine engine, Array args)
        {
            return GetEconomicParameter(engine.Snapshot).GasPerBlock;
        }

        [ContractMethod(0_08000000, ContractParameterType.Integer, CallFlags.AllowStates)]
        private StackItem GetNeoHoldersRewardRatio(ApplicationEngine engine, Array args)
        {
            return (int)GetEconomicParameter(engine.Snapshot).NeoHoldersRewardRatio;
        }

        [ContractMethod(0_08000000, ContractParameterType.Integer, CallFlags.AllowStates)]
        private StackItem GetCommitteesRewardRatio(ApplicationEngine engine, Array args)
        {
            return (int)GetEconomicParameter(engine.Snapshot).CommitteesRewardRatio;
        }

        [ContractMethod(0_08000000, ContractParameterType.Integer, CallFlags.AllowStates)]
        private StackItem GetVotersRewardRatio(ApplicationEngine engine, Array args)
        {
            return (int)GetEconomicParameter(engine.Snapshot).VotersRewardRatio;
        }

        private EconomicParameter GetAndChangeEconomicParameter(StoreView snapshot)
        {
            StorageItem storageItem = snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_Ecomonic));
            return storageItem.GetInteroperable<EconomicParameter>();
        }

        private EconomicParameter GetEconomicParameter(StoreView snapshot)
        {
            StorageItem storageItem = snapshot.Storages.TryGet(CreateStorageKey(Prefix_Ecomonic));
            return storageItem.GetInteroperable<EconomicParameter>();
        }
    }
}
