using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Linq;

namespace Neo.Ledger
{
    partial class Blockchain
    {
        partial class ApplicationExecuted
        {
            /// <summary>
            /// The transaction that containing the executed script. This field could be <see langword="null"/> if the contract is invoked by system.
            /// </summary>
            public Transaction Transaction { get; }

            /// <summary>
            /// The trigger of the execution.
            /// </summary>
            public TriggerType Trigger { get; }

            /// <summary>
            /// The state of the virtual machine after the contract is executed.
            /// </summary>
            public VMState VMState { get; }

            /// <summary>
            /// The exception that caused the execution to terminate abnormally. This field could be <see langword="null"/> if the execution ends normally.
            /// </summary>
            public Exception Exception { get; }

            /// <summary>
            /// GAS spent to execute.
            /// </summary>
            public long GasConsumed { get; }

            /// <summary>
            /// Items on the stack of the virtual machine after execution.
            /// </summary>
            public StackItem[] Stack { get; }

            /// <summary>
            /// The notifications sent during the execution.
            /// </summary>
            public NotifyEventArgs[] Notifications { get; }

            internal ApplicationExecuted(ApplicationEngine engine)
            {
                Transaction = engine.ScriptContainer as Transaction;
                Trigger = engine.Trigger;
                VMState = engine.State;
                GasConsumed = engine.GasConsumed;
                Exception = engine.FaultException;
                Stack = engine.ResultStack.ToArray();
                Notifications = engine.Notifications.ToArray();
            }
        }
    }
}
