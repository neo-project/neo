// Copyright (C) 2015-2024 The Neo Project.
//
// ContractTask.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Runtime.CompilerServices;

namespace Neo.SmartContract
{
    [AsyncMethodBuilder(typeof(ContractTaskMethodBuilder))]
    class ContractTask
    {
        protected readonly ContractTaskAwaiter _awaiter;

        public static ContractTask CompletedTask { get; }

        static ContractTask()
        {
            CompletedTask = new ContractTask();
            CompletedTask.GetAwaiter().SetResult();
        }

        public ContractTask()
        {
            _awaiter = CreateAwaiter();
        }

        protected virtual ContractTaskAwaiter CreateAwaiter() => new();
        public ContractTaskAwaiter GetAwaiter() => _awaiter;
        public virtual object GetResult() => null;
    }

    [AsyncMethodBuilder(typeof(ContractTaskMethodBuilder<>))]
    class ContractTask<T> : ContractTask
    {
        public new static ContractTask<T> CompletedTask { get; }

        static ContractTask()
        {
            CompletedTask = new ContractTask<T>();
            CompletedTask.GetAwaiter().SetResult();
        }

        protected override ContractTaskAwaiter CreateAwaiter() => new ContractTaskAwaiter<T>();
#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
        public ContractTaskAwaiter<T> GetAwaiter() => (ContractTaskAwaiter<T>)_awaiter;
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword
        public override object GetResult() => GetAwaiter().GetResult();
    }
}
