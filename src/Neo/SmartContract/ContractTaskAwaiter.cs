// Copyright (C) 2015-2022 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Neo.SmartContract
{
    class ContractTaskAwaiter : INotifyCompletion
    {
        private Action continuation;
        private Exception exception;

        public bool IsCompleted { get; private set; }

        public void GetResult()
        {
            if (exception is not null)
                throw exception;
        }

        public void SetResult() => RunContinuation();

        public virtual void SetResult(ApplicationEngine engine) => SetResult();

        public void SetException(Exception exception)
        {
            this.exception = exception;
            RunContinuation();
        }

        public void OnCompleted(Action continuation)
        {
            Interlocked.CompareExchange(ref this.continuation, continuation, null);
        }

        protected void RunContinuation()
        {
            IsCompleted = true;
            continuation?.Invoke();
        }
    }

    class ContractTaskAwaiter<T> : ContractTaskAwaiter
    {
        private T result;

        public new T GetResult()
        {
            base.GetResult();
            return result;
        }

        public void SetResult(T result)
        {
            this.result = result;
            RunContinuation();
        }

        public override void SetResult(ApplicationEngine engine)
        {
            SetResult((T)engine.Convert(engine.Pop(), new InteropParameterDescriptor(typeof(T))));
        }
    }
}
