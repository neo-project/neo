// Copyright (C) 2015-2026 The Neo Project.
//
// Mnemonic.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography;
using Neo.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Neo.Wallets
{
    /// <summary>
    /// Represents a BIP-0039 mnemonic phrase, which encodes cryptographic entropy as a sequence of words for use in
    /// deterministic key generation and wallet recovery.
    /// </summary>
    /// <remarks>
    /// A mnemonic phrase is a human-readable representation of binary entropy, commonly used in cryptocurrency wallets for
    /// backup and recovery. This class provides methods to create, parse, and represent mnemonic phrases according to the
    /// BIP-0039 standard, supporting multiple languages based on the current or specified culture.
    /// </remarks>
    public class Mnemonic : IReadOnlyList<string>
    {
        static readonly Dictionary<string, string[]> s_wordlists = new();
        static readonly Dictionary<string, int> s_wordlistsReverseIndex = new();
        readonly string[] words;

        /// <summary>
        /// Gets the word at the specified zero-based index.
        /// </summary>
        /// <param name="index">The zero-based index of the word to retrieve. Must be greater than or equal to 0 and less than the total number
        /// of words.</param>
        /// <returns>The word at the specified index.</returns>
        public string this[int index] => words[index];

        /// <summary>
        /// Gets the total number of words in the mnemonic phrase.
        /// </summary>
        public int Count => words.Length;

        static Mnemonic()
        {
            var resourceSet = Resources.ResourceManager.GetResourceSet(CultureInfo.InvariantCulture, true, false)!;
            foreach (var res in resourceSet.Cast<DictionaryEntry>())
            {
                string key = (string)res.Key;
                if (!key.StartsWith("BIP-39.")) continue;
                string value = (string)res.Value!;
                string[] wordlist = value.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
                s_wordlists.Add(key[7..], wordlist);
                for (int i = 0; i < wordlist.Length; i++)
                    s_wordlistsReverseIndex[wordlist[i]] = i;
            }
        }

        Mnemonic(ReadOnlySpan<byte> entropy, string[] wordlist)
        {
            if (entropy.Length < 16 || entropy.Length > 32)
                throw new ArgumentException("The length of entropy should be between 128 and 256 bits.", nameof(entropy));
            if (entropy.Length % 4 != 0)
                throw new ArgumentException("The length of entropy should be a multiple of 32 bits.", nameof(entropy));
            int entropyBits = entropy.Length * 8;
            int checksumBits = entropyBits / 32;
            int totalBits = entropyBits + checksumBits;
            byte[] checksum = entropy.Sha256();
            int wordCount = totalBits / 11;
            words = new string[wordCount];
            for (int i = 0; i < wordCount; i++)
            {
                int index = 0;
                for (int j = 0; j < 11; j++)
                {
                    int bitPos = i * 11 + j;
                    bool bit = bitPos < entropyBits
                        ? GetBitMSB(entropy, bitPos)
                        : GetBitMSB(checksum, bitPos - entropyBits);
                    if (bit) index |= 1 << (10 - j);
                }
                words[i] = wordlist[index];
            }
        }

        Mnemonic(string[] words, string[] wordlist)
        {
            int wordCount = words.Length;
            if (wordCount < 12 || wordCount > 24 || wordCount % 3 != 0)
                throw new ArgumentException("The number of words should be 12, 15, 18, 21 or 24.", nameof(words));
            int totalBits = wordCount * 11;
            int entropyBits = totalBits * 32 / 33;
            int checksumBits = totalBits - entropyBits;
            int entropyBytes = entropyBits / 8;
            byte[] entropy = new byte[entropyBytes];
            Span<byte> checksum = stackalloc byte[(checksumBits + 7) / 8];
            for (int i = 0; i < wordCount; i++)
            {
                if (!s_wordlistsReverseIndex.TryGetValue(words[i], out int index))
                    throw new ArgumentException($"The word '{words[i]}' is not in the BIP-0039 wordlist.", nameof(words));
                for (int j = 0; j < 11; j++)
                {
                    int bitPos = i * 11 + j;
                    bool bit = (index & (1 << (10 - j))) != 0;
                    if (bitPos < entropyBits)
                    {
                        int byteIndex = bitPos / 8;
                        int bitInByte = 7 - (bitPos % 8);
                        if (bit) entropy[byteIndex] |= (byte)(1 << bitInByte);
                    }
                    else
                    {
                        int csBitPos = bitPos - entropyBits;
                        int byteIndex = csBitPos / 8;
                        int bitInByte = 7 - (csBitPos % 8);
                        if (bit) checksum[byteIndex] |= (byte)(1 << bitInByte);
                    }
                }
            }
            byte[] hash = entropy.Sha256();
            for (int i = 0; i < checksumBits; i++)
            {
                int byteIndex = i / 8;
                int bitInByte = 7 - (i % 8);
                bool bitFromHash = (hash[byteIndex] & (1 << bitInByte)) != 0;
                bool bitFromChecksum = (checksum[byteIndex] & (1 << bitInByte)) != 0;
                if (bitFromHash != bitFromChecksum)
                    throw new ArgumentException("Invalid mnemonic: checksum does not match.", nameof(words));
            }
            this.words = words;
        }

        /// <summary>
        /// Creates a new mnemonic phrase using randomly generated entropy of the specified bit length.
        /// </summary>
        /// <param name="bits">The length of entropy, in bits, to use for generating the mnemonic. Must be a multiple of 32 between 128 and
        /// 256, inclusive. The security of the mnemonic increases with higher entropy.</param>
        /// <returns>A new <see cref="Mnemonic"/> instance generated from the specified amount of random entropy.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="bits"/> is less than 128, greater than 256, or not a multiple of 32.</exception>
        public static Mnemonic Create(int bits = 128)
        {
            if (bits < 128 || bits > 256)
                throw new ArgumentException("The length of entropy should be between 128 and 256 bits.", nameof(bits));
            if (bits % 4 != 0)
                throw new ArgumentException("The length of entropy should be a multiple of 32 bits.", nameof(bits));
            byte[] entropy = RandomNumberGenerator.GetBytes(bits / 8);
            return Create(entropy);
        }

        /// <summary>
        /// Creates a new mnemonic phrase from the specified entropy using the current culture's language settings.
        /// </summary>
        /// <remarks>The language used for the mnemonic phrase is determined by <see
        /// cref="System.Globalization.CultureInfo.CurrentCulture"/>. To specify a different language, use the overload that
        /// accepts a <see cref="System.Globalization.CultureInfo"/> parameter.</remarks>
        /// <param name="entropy">A read-only span of bytes representing the entropy to use for generating the mnemonic phrase. The length and
        /// content must conform to the requirements of the mnemonic standard.</param>
        /// <returns>A <see cref="Mnemonic"/> instance representing the generated mnemonic phrase.</returns>
        public static Mnemonic Create(ReadOnlySpan<byte> entropy)
        {
            return Create(entropy, CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Creates a new mnemonic phrase using the specified entropy and culture-specific wordlist.
        /// </summary>
        /// <remarks>If the specified culture or its parent cultures are not supported, the method defaults to
        /// using the English wordlist.</remarks>
        /// <param name="entropy">A read-only span of bytes representing the entropy used to generate the mnemonic. The length and content must
        /// conform to the requirements of the mnemonic standard being used.</param>
        /// <param name="culture">The culture that determines which language wordlist is used for the mnemonic phrase. If the specified culture is
        /// not supported, the method attempts to use its parent culture.</param>
        /// <returns>A new instance of the Mnemonic class generated from the provided entropy and the wordlist corresponding to the
        /// specified culture.</returns>
        public static Mnemonic Create(ReadOnlySpan<byte> entropy, CultureInfo culture)
        {
            if (culture.Equals(CultureInfo.InvariantCulture))
                return Create(entropy, new CultureInfo("en"));
            if (!s_wordlists.TryGetValue(culture.Name, out string[]? wordlist))
                return Create(entropy, culture.Parent);
            return new(entropy, wordlist);
        }

        /// <summary>
        /// Parses a mnemonic phrase and returns a corresponding Mnemonic instance if the phrase is valid.
        /// </summary>
        /// <remarks>The mnemonic phrase must use words from a single supported wordlist. The method trims
        /// whitespace and splits the phrase on whitespace characters. Only phrases with a word count that is a multiple of
        /// 3 and between 12 and 24 words (inclusive) are considered valid.</remarks>
        /// <param name="mnemonic">The mnemonic phrase to parse. Must consist of 12, 15, 18, 21, or 24 space-separated words from a supported
        /// wordlist.</param>
        /// <returns>A Mnemonic instance representing the parsed mnemonic phrase.</returns>
        /// <exception cref="ArgumentException">Thrown if the mnemonic is null, empty, contains an invalid number of words, or includes words not found in any
        /// supported wordlist.</exception>
        public static Mnemonic Parse(string mnemonic)
        {
            string[] words = mnemonic.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (words.Length < 12 || words.Length > 24)
                throw new ArgumentException("Mnemonic word count should be between 12 and 24.", nameof(mnemonic));
            if (words.Length % 3 != 0)
                throw new ArgumentException("Mnemonic word count should be a multiple of 3.", nameof(mnemonic));
            foreach (var (_, wordlist) in s_wordlists)
                if (words.All(p => wordlist.Contains(p)))
                    return new(words, wordlist);
            throw new ArgumentException("The mnemonic contains words that are not in the wordlist.", nameof(mnemonic));
        }

        /// <summary>
        /// Derives a cryptographic seed from the mnemonic using the specified passphrase according to BIP-0039 standards.
        /// </summary>
        /// <remarks>The seed is generated using PBKDF2 with HMAC-SHA512, 2048 iterations, and a salt composed of
        /// the string "mnemonic" concatenated with the passphrase. This method is compatible with BIP-0039 wallet
        /// implementations.</remarks>
        /// <param name="passphrase">An optional passphrase to strengthen the seed derivation. If not specified, an empty string is used.</param>
        /// <returns>A 64-byte array containing the derived seed suitable for use in hierarchical deterministic wallets and other
        /// cryptographic applications.</returns>
        public byte[] DeriveSeed(string passphrase = "")
        {
            string mnemonic = ToString().Normalize(NormalizationForm.FormKD);
            byte[] salt = Encoding.UTF8.GetBytes("mnemonic" + passphrase.Normalize(NormalizationForm.FormKD));
            return Rfc2898DeriveBytes.Pbkdf2(mnemonic, salt, 2048, HashAlgorithmName.SHA512, 64);
        }

        /// <summary>
        /// Returns a string that represents the concatenated words separated by spaces.
        /// </summary>
        /// <returns>A string containing all words joined by a single space character.</returns>
        public override string ToString()
        {
            return string.Join(' ', words);
        }

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            return words.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return words.GetEnumerator();
        }

        static bool GetBitMSB(ReadOnlySpan<byte> data, int bitIndex)
        {
            int byteIndex = bitIndex / 8;
            int bitInByte = 7 - (bitIndex % 8); // MSB-first
            return (data[byteIndex] & (1 << bitInByte)) != 0;
        }
    }
}
