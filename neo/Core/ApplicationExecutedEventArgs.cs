using Neo.SmartContract;
using Neo.VM;
using System;
using System.Linq;

namespace Neo.Core
{
    public class ApplicationExecutedEventArgs : EventArgs
    {
        public InvocationTransaction Transaction { get; }
        public VMState VMState { get; }
        public Fixed8 GasConsumed { get; }
        public StackItem[] Stack { get; }
        public NotifyEventArgs[] Notifications { get; }

        public ApplicationExecutedEventArgs(InvocationTransaction tx, NotifyEventArgs[] notifications, ApplicationEngine engine)
        {
            this.Transaction = tx;
            this.VMState = engine.State;
            this.GasConsumed = engine.GasConsumed;
            this.Stack = engine.EvaluationStack.ToArray();
            this.Notifications = notifications;
        }
    }
}
