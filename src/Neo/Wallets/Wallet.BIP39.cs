// Copyright (C) 2015-2026 The Neo Project.
//
// Wallet.BIP39.cs file belongs to the neo project and is free
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

namespace Neo.Wallets
{
    partial class Wallet
    {
        static readonly Dictionary<string, string[]> wordlists = new();
        static readonly Dictionary<string, int> wordlists_reverse_index = new();

        static Wallet()
        {
            var resourceSet = Resources.ResourceManager.GetResourceSet(CultureInfo.InvariantCulture, true, false)!;
            foreach (var res in resourceSet.Cast<DictionaryEntry>())
            {
                string key = (string)res.Key;
                if (!key.StartsWith("BIP-39.")) continue;
                string value = (string)res.Value!;
                string[] wordlist = value.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
                wordlists.Add(key[7..], wordlist);
                for (int i = 0; i < wordlist.Length; i++)
                    wordlists_reverse_index[wordlist[i]] = i;
            }
        }

        /// <summary>
        /// Generates a BIP-39 mnemonic code from the specified entropy using the current culture's wordlist.
        /// </summary>
        /// <remarks>The mnemonic code is generated using the wordlist associated with the current thread's
        /// culture. To specify a different culture, use the overload that accepts a CultureInfo parameter.</remarks>
        /// <param name="entropy">A read-only span of bytes representing the entropy to convert into a mnemonic code. The length must be valid
        /// according to the BIP-39 specification.</param>
        /// <returns>An array of strings containing the mnemonic words corresponding to the provided entropy.</returns>
        public static string[] GetMnemonicCode(ReadOnlySpan<byte> entropy)
        {
            return GetMnemonicCode(entropy, CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Generates a BIP-39 mnemonic code from the specified entropy using the wordlist for the given culture.
        /// </summary>
        /// <remarks>If the specified culture or its parent cultures are not supported, the method defaults to
        /// using the English wordlist. The mnemonic code is generated according to the BIP-39 standard.</remarks>
        /// <param name="entropy">A read-only span of bytes representing the entropy to convert into a mnemonic code. The length must be valid
        /// according to the BIP-39 specification.</param>
        /// <param name="culture">The culture used to select the appropriate wordlist for the mnemonic code. If the specified culture is not
        /// supported, the method attempts to use its parent culture.</param>
        /// <returns>An array of strings containing the mnemonic words corresponding to the provided entropy and culture.</returns>
        public static string[] GetMnemonicCode(ReadOnlySpan<byte> entropy, CultureInfo culture)
        {
            if (culture.Equals(CultureInfo.InvariantCulture))
                return GetMnemonicCode(entropy, new CultureInfo("en"));
            if (!wordlists.TryGetValue(culture.Name, out string[]? wordlist))
                return GetMnemonicCode(entropy, culture.Parent);
            return GetMnemonicCode(entropy, wordlist);
        }

        static string[] GetMnemonicCode(ReadOnlySpan<byte> entropy, string[] wordlist)
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
            string[] mnemonic = new string[wordCount];
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
                mnemonic[i] = wordlist[index];
            }
            return mnemonic;
        }

        public static byte[] MnemonicToEntropy(string[] mnemonic)
        {
            int wordCount = mnemonic.Length;
            if (wordCount < 12 || wordCount > 24 || wordCount % 3 != 0)
                throw new ArgumentException("The number of words should be 12, 15, 18, 21 or 24.", nameof(mnemonic));
            int totalBits = wordCount * 11;
            int entropyBits = totalBits * 32 / 33;
            int checksumBits = totalBits - entropyBits;
            int entropyBytes = entropyBits / 8;
            byte[] entropy = new byte[entropyBytes];
            Span<byte> checksum = stackalloc byte[(checksumBits + 7) / 8];
            for (int i = 0; i < wordCount; i++)
            {
                if (!wordlists_reverse_index.TryGetValue(mnemonic[i], out int index))
                    throw new ArgumentException($"The word '{mnemonic[i]}' is not in the BIP-39 wordlist.", nameof(mnemonic));
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
                    throw new ArgumentException("Invalid mnemonic: checksum does not match.", nameof(mnemonic));
            }
            return entropy;
        }

        static bool GetBitMSB(ReadOnlySpan<byte> data, int bitIndex)
        {
            int byteIndex = bitIndex / 8;
            int bitInByte = 7 - (bitIndex % 8); // MSB-first
            return (data[byteIndex] & (1 << bitInByte)) != 0;
        }
    }
}
