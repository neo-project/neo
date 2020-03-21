using Neo.Ledger;
using Neo.VM;
using Neo.VM.Types;

namespace Neo.SmartContract
{
    partial class InteropService
    {
        public static class Blockchain
        {
            public static readonly InteropDescriptor GetHeight = Register("System.Blockchain.GetHeight", Blockchain_GetHeight, 0_00000400, TriggerType.Application, CallFlags.None);
            public static readonly InteropDescriptor GetContract = Register("System.Blockchain.GetContract", Blockchain_GetContract, 0_01000000, TriggerType.Application, CallFlags.None);

            private static bool Blockchain_GetHeight(ApplicationEngine engine)
            {
                engine.CurrentContext.EvaluationStack.Push(engine.Snapshot.Height);
                return true;
            }

            private static bool Blockchain_GetContract(ApplicationEngine engine)
            {
                UInt160 hash = new UInt160(engine.CurrentContext.EvaluationStack.Pop().GetSpan());
                ContractState contract = engine.Snapshot.Contracts.TryGet(hash);
                if (contract == null)
                    engine.CurrentContext.EvaluationStack.Push(StackItem.Null);
                else
                    engine.CurrentContext.EvaluationStack.Push(contract.ToStackItem(engine.ReferenceCounter));
                return true;
            }
        }
    }
}
