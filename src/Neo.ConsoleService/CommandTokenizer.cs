// Copyright (C) 2015-2025 The Neo Project.
//
// CommandTokenizer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Neo.ConsoleService
{
    public static class CommandTokenizer
    {
        private static char EscapedChar(char ch)
        {
            return ch switch
            {
                '\\' => '\\',
                '"' => '"',
                '\'' => '\'',
                'n' => '\n',
                'r' => '\r',
                't' => '\t',
                'v' => '\v',
                'b' => '\b',
                'f' => '\f',
                'a' => '\a',
                'e' => '\e',
                '0' => '\0',
                ' ' => ' ',
                _ => throw new ArgumentException($"Invalid escaped character: {ch}")
            };
        }

        private static (char, int) EscapedChar(string commandLine, int index)
        {
            index++; // next char after \
            if (index >= commandLine.Length)
            {
                throw new ArgumentException("Unexpected end of command line while processing escape sequence." +
                    " The command line ends with a backslash character.");
            }

            if (commandLine[index] == 'x')
            {
                if (index + 2 >= commandLine.Length)
                {
                    throw new ArgumentException("Unexpected end of command line while processing escape sequence." +
                        " Too few hex digits after \\x");
                }

                if (!byte.TryParse(commandLine.AsSpan(index + 1, 2), NumberStyles.AllowHexSpecifier, null, out var ch))
                    throw new ArgumentException($"Invalid hex digits after \\x");

                return new((char)ch, 1 + 2);
            }

            if (commandLine[index] == 'u')
            {
                if (index + 4 >= commandLine.Length)
                {
                    throw new ArgumentException("Unexpected end of command line while processing escape sequence." +
                        " Too few hex digits after \\u");
                }

                if (!ushort.TryParse(commandLine.AsSpan(index + 1, 4), NumberStyles.AllowHexSpecifier, null, out var ch))
                    throw new ArgumentException($"Invalid hex digits after \\u");

                // handle invalid surrogate pairs if needed, but good enough for a cli tool
                return new((char)ch, 1 + 4);
            }

            return new(EscapedChar(commandLine[index]), 1);
        }

        /// <summary>
        /// Tokenize a command line
        /// </summary>
        /// <param name="commandLine">The command line to tokenize</param>
        /// <returns>The tokens</returns>
        public static List<CommandToken> Tokenize(this string commandLine)
        {
            var tokens = new List<CommandToken>();
            var token = new StringBuilder();
            var quoteChar = CommandToken.NoQuoteChar;
            var addToken = (int index, char quote) =>
            {
                var value = token.ToString();
                tokens.Add(new CommandToken(index - value.Length, value, quote));
                token.Clear();
            };

            for (var index = 0; index < commandLine.Length; index++)
            {
                var ch = commandLine[index];
                if (ch == '\\' && quoteChar != CommandToken.NoEscapedChar)
                {
                    (var escapedChar, var length) = EscapedChar(commandLine, index);
                    token.Append(escapedChar);
                    index += length;
                }
                else if (quoteChar != CommandToken.NoQuoteChar)
                {
                    if (ch == quoteChar)
                    {
                        addToken(index, quoteChar);
                        quoteChar = CommandToken.NoQuoteChar;
                    }
                    else
                    {
                        token.Append(ch);
                    }
                }
                else if (ch == '"' || ch == '\'' || ch == CommandToken.NoEscapedChar)
                {
                    if (token.Length == 0) // If ch is the first char. To keep consistency with legacy behavior
                    {
                        quoteChar = ch;
                    }
                    else
                    {
                        token.Append(ch); // If ch is not the first char, append it as a normal char
                    }
                }
                else if (char.IsWhiteSpace(ch))
                {
                    if (token.Length > 0) addToken(index, quoteChar);

                    token.Append(ch);
                    while (index + 1 < commandLine.Length && char.IsWhiteSpace(commandLine[index + 1]))
                    {
                        token.Append(commandLine[++index]);
                    }
                    addToken(index, quoteChar);
                }
                else
                {
                    token.Append(ch);
                }
            }

            if (quoteChar != CommandToken.NoQuoteChar) // uncompleted quote
                throw new ArgumentException($"Unmatched quote({quoteChar})");
            if (token.Length > 0) addToken(commandLine.Length, quoteChar);
            return tokens;
        }

        /// <summary>
        /// Join the raw token values into a single string without prefix and suffix white spaces
        /// </summary>
        /// <param name="tokens">The list of tokens</param>
        /// <returns>The joined string</returns>
        public static string JoinRaw(this IList<CommandToken> tokens)
        {
            return string.Join("", tokens.Trim().Select(t => t.RawValue));
        }

        /// <summary>
        /// Consume the first token from the list without prefix and suffix white spaces
        /// </summary>
        /// <param name="tokens">The list of tokens</param>
        /// <returns>The value of the first non-white space token</returns>
        public static string Consume(this IList<CommandToken> tokens)
        {
            tokens.Trim();
            if (tokens.Count == 0) return "";

            var token = tokens[0];
            tokens.RemoveAt(0);
            return token.Value;
        }

        /// <summary>
        /// Consume all tokens from the list and join them without prefix and suffix white spaces
        /// </summary>
        /// <param name="tokens">The list of tokens</param>
        /// <returns>The joined value of all tokens without prefix and suffix white spaces</returns>
        public static string ConsumeAll(this IList<CommandToken> tokens)
        {
            var result = tokens.Trim().JoinRaw();
            tokens.Clear();
            return result;
        }

        /// <summary>
        /// Remove the prefix and suffix white spaces from the list of tokens
        /// </summary>
        /// <param name="tokens">The list of tokens</param>
        /// <returns>The trimmed list of tokens</returns>
        public static IList<CommandToken> Trim(this IList<CommandToken> tokens)
        {
            while (tokens.Count > 0 && tokens[0].IsWhiteSpace) tokens.RemoveAt(0);
            while (tokens.Count > 0 && tokens[^1].IsWhiteSpace) tokens.RemoveAt(tokens.Count - 1);
            return tokens;
        }
    }
}
