// Copyright (C) 2015-2025 The Neo Project.
//
// NeoBuildException.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Exceptions.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Neo.Build.Exceptions
{
    /// <inheritdoc />
    internal class NeoBuildException() : Exception(), INeoBuildException
    {
        /// <summary>
        /// Used for exit code and making property <see cref="ErrorCode" />.
        /// </summary>
        public virtual new int HResult =>
            NeoBuildErrorCodes.General.InternalCrash;

        /// <summary>
        /// Used for standardizing build errors.
        /// </summary>
        public virtual new string Message =>
            InnerException is null ?
                NeoBuildErrorCodes.FormatErrorMessage(this, base.Message) :
                NeoBuildErrorCodes.FormatErrorMessage(this, base.Message, InnerException);

        /// <summary>
        /// Message returned to the client when a build fails.
        /// </summary>
        public virtual string ErrorCode =>
            NeoBuildErrorCodes.MakeErrorCode(HResult);

        /// <inheritdoc />
        [return: NotNull]
        public virtual new string ToString() =>
            Message;
    }
}
