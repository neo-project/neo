// Copyright (C) 2015-2026 The Neo Project.
//
// JsonPlusEncoder.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Text.Encodings.Web;

namespace Neo.Json
{
    /// <summary>
    /// <see cref="JavaScriptEncoder"/> matching <see cref="JavaScriptEncoder.Default"/>
    /// except that <c>+</c> is not escaped to <c>\u002B</c>.
    /// </summary>
    /// <remarks>
    /// System.Text.Json's default encoder rewrites <c>+</c> as <c>\u002B</c>, which inflates
    /// base64 payloads in storage (see neo-project/neo#2612). Other escaping (quotes, controls,
    /// non-ASCII BMP as <c>\uXXXX</c>, HTML-sensitive chars) is unchanged.
    /// </remarks>
    public sealed class JsonPlusEncoder : JavaScriptEncoder
    {
        /// <summary>
        /// Shared instance.
        /// </summary>
        public static JsonPlusEncoder Instance { get; } = new();

        private static readonly JavaScriptEncoder Inner = JavaScriptEncoder.Default;

        private JsonPlusEncoder() { }

        /// <inheritdoc />
        public override int MaxOutputCharactersPerInputCharacter =>
            Inner.MaxOutputCharactersPerInputCharacter;

        /// <inheritdoc />
        public override unsafe int FindFirstCharacterToEncode(char* text, int textLength)
        {
            for (var i = 0; i < textLength; i++)
            {
                var ch = text[i];
                if (ch == '+')
                    continue;

                if (char.IsHighSurrogate(ch))
                {
                    if (i + 1 < textLength && char.IsLowSurrogate(text[i + 1]))
                    {
                        var scalar = char.ConvertToUtf32(ch, text[i + 1]);
                        if (WillEncode(scalar))
                            return i;
                        i++;
                        continue;
                    }
                }

                if (WillEncode(ch))
                    return i;
            }

            return -1;
        }

        /// <inheritdoc />
        public override bool WillEncode(int unicodeScalar)
        {
            if (unicodeScalar == '+')
                return false;
            return Inner.WillEncode(unicodeScalar);
        }

        /// <inheritdoc />
        public override unsafe bool TryEncodeUnicodeScalar(
            int unicodeScalar, char* buffer, int bufferLength, out int numberOfCharactersWritten)
        {
            if (unicodeScalar == '+')
            {
                if (bufferLength < 1)
                {
                    numberOfCharactersWritten = 0;
                    return false;
                }

                buffer[0] = '+';
                numberOfCharactersWritten = 1;
                return true;
            }

            return Inner.TryEncodeUnicodeScalar(unicodeScalar, buffer, bufferLength, out numberOfCharactersWritten);
        }
    }
}
