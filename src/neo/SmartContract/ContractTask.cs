using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Neo.SmartContract
{
    class ContractTask : INotifyCompletion
    {
        private Action continuation;

        public bool IsCompleted => false;

        public ContractTask GetAwaiter() => this;

        public void GetResult() { }

        public void OnCompleted(Action continuation)
        {
            Interlocked.CompareExchange(ref this.continuation, continuation, null);
        }

        public void RunContinuation()
        {
            continuation();
        }
    }

    class ContractTask<T> : ContractTask
    {
        private readonly ApplicationEngine engine;

        public ContractTask(ApplicationEngine engine)
        {
            this.engine = engine;
        }

        public new T GetResult()
        {
            return (T)engine.Convert(engine.Pop(), new InteropParameterDescriptor(typeof(T)));
        }
    }
}
