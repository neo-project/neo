// Copyright (C) 2015-2024 The Neo Project.
//
// ErrorModel.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Plugins.RestServer.Models.Error
{
    internal class ErrorModel
    {
        /// <summary>
        /// Error's HResult Code.
        /// </summary>
        /// <example>1000</example>
        public int Code { get; init; } = 1000;
        /// <summary>
        /// Error's name of the type.
        /// </summary>
        /// <example>GeneralException</example>
        public string Name { get; init; } = "GeneralException";
        /// <summary>
        /// Error's exception message.
        /// </summary>
        /// <example>An error occurred.</example>
        /// <remarks>Could be InnerException message as well, If exists.</remarks>
        public string Message { get; init; } = "An error occurred.";
    }
}
