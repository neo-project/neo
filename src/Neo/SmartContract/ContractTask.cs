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
        private readonly ContractTaskAwaiter awaiter;

        public static ContractTask CompletedTask { get; }

        static ContractTask()
        {
            CompletedTask = new ContractTask();
            CompletedTask.GetAwaiter().SetResult();
        }

        public ContractTask()
        {
            awaiter = CreateAwaiter();
        }

        protected virtual ContractTaskAwaiter CreateAwaiter() => new();

        public virtual ContractTaskAwaiter GetAwaiter() => awaiter;

        public virtual object GetResult() => null;
    }

    [AsyncMethodBuilder(typeof(ContractTaskMethodBuilder<>))]
    class ContractTask<T>
    {
        private readonly ContractTaskAwaiter awaiter;

        public static ContractTask CompletedTask { get; }

        static ContractTask()
        {
            CompletedTask = new ContractTask();
            CompletedTask.GetAwaiter().SetResult();
        }

        public ContractTask()
        {
            awaiter = CreateAwaiter();
        }

        protected ContractTaskAwaiter<T> CreateAwaiter() => new();

        public ContractTaskAwaiter<T> GetAwaiter() => (ContractTaskAwaiter<T>)awaiter;

        public object GetResult() => GetAwaiter().GetResult();
    }
}
