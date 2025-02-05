// Copyright (C) 2015-2025 The Neo Project.
//
// NeoBuildException.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Exceptions.Interfaces;
using System;

namespace Neo.Build.Exceptions
{
    /// <inheritdoc />
    internal abstract class NeoBuildException() : Exception(), INeoBuildException
    {
        /// <summary>
        /// Used for exit code and making property <see cref="ErrorCode" />.
        /// </summary>
        public abstract new int HResult { get; }

        /// <summary>
        /// Used for standardizing build errors.
        /// </summary>
        public abstract new string Message { get; }

        /// <summary>
        /// Message returned to the client when a build fails.
        /// </summary>
        public virtual string ErrorCode => NeoBuildErrorCodes.MakeErrorCode(HResult);

        /// <inheritdoc />
        public abstract override string ToString();
    }
}
