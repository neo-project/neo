// Copyright (C) 2015-2025 The Neo Project.
//
// NeoBuildFileNotFoundException.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Exceptions.Interfaces;
using System.IO;

namespace Neo.Build.Exceptions
{
    /// <summary>
    /// Thrown when a file isn't found in the build process.
    /// </summary>
    /// <param name="fileInfo">File info about the location</param>
    internal class NeoBuildFileNotFoundException(
        FileInfo fileInfo) : NeoBuildException(), IFileNotFoundException
    {
        /// <inheritdoc />
        public override int HResult => NeoBuildErrorCodes.General.FileNotFound;

        /// <inheritdoc />
        public override string ErrorCode => NeoBuildErrorCodes.MakeErrorCode(HResult);

        /// <inheritdoc />
        public override string Message => $"{ErrorCode}: File '{FileInfo}' was not found.";

        /// <inheritdoc />
        public FileInfo FileInfo => fileInfo;

        /// <inheritdoc />
        public override string ToString() =>
            Message;
    }
}
