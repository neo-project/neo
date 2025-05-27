// Copyright (C) 2015-2025 The Neo Project.
//
// ContractTaskAwaiter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Neo.SmartContract
{
    internal class ContractTaskAwaiter : INotifyCompletion
    {
        private Action _continuation;
        private Exception _exception;

        public bool IsCompleted { get; private set; }

        public void GetResult()
        {
            if (_exception is not null)
                throw _exception;
        }

        public void SetResult() => RunContinuation();

        public virtual void SetResult(ApplicationEngine engine) => SetResult();

        public void SetException(Exception exception)
        {
            _exception = exception;
            RunContinuation();
        }

        public void OnCompleted(Action continuation)
        {
            Interlocked.CompareExchange(ref _continuation, continuation, null);
        }

        protected void RunContinuation()
        {
            IsCompleted = true;
            _continuation?.Invoke();
        }
    }

    internal class ContractTaskAwaiter<T> : ContractTaskAwaiter
    {
        private T _result;

        public new T GetResult()
        {
            base.GetResult();
            return _result;
        }

        public void SetResult(T result)
        {
            _result = result;
            RunContinuation();
        }

        public override void SetResult(ApplicationEngine engine)
        {
            SetResult((T)engine.Convert(engine.Pop(), new InteropParameterDescriptor(typeof(T))));
        }
    }
}
