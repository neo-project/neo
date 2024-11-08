// Copyright (C) 2015-2024 The Neo Project.
//
// IMillerLoopDriver.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Cryptography.BLS12_381
{
    interface IMillerLoopDriver<T>
    {
        public T DoublingStep(in T f);
        public T AdditionStep(in T f);
        public T Square(in T f);
        public T Conjugate(in T f);
        public T One { get; }
    }
}
