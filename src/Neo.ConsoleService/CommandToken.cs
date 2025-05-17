// Copyright (C) 2015-2025 The Neo Project.
//
// CommandToken.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.ConsoleService
{
    public readonly struct CommandToken(int offset, string value, char quoteChar)
    {
        public const char NoQuoteChar = '\0';

        /// <summary>
        /// The start offset of the token in the command line
        /// </summary>
        public readonly int Offset { get; } = offset;

        /// <summary>
        /// The value of the token
        /// </summary>
        public readonly string Value { get; } = value;

        private readonly char _quoteChar = quoteChar;

        /// <summary>
        /// The raw value of the token(includes quote character if raw value is quoted)
        /// </summary>
        public readonly string RawValue => _quoteChar == NoQuoteChar ? Value : $"{_quoteChar}{Value}{_quoteChar}";

        /// <summary>
        /// Whether the token is  white spaces(includes empty) or not
        /// </summary>
        public readonly bool IsWhiteSpace => _quoteChar == NoQuoteChar && string.IsNullOrWhiteSpace(Value);
    }
}
