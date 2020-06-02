using Neo.VM;
using Neo.VM.Types;
using System;

namespace Neo.SmartContract
{
    internal class Callback
    {
        public readonly StackItem[] Arguments;
        public readonly Action<ApplicationEngine> Action;

        public Callback(Action<ApplicationEngine> action, params StackItem[] args)
        {
            Action = action;
            Arguments = args;
        }
    }
}
