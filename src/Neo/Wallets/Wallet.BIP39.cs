// Copyright (C) 2015-2025 The Neo Project.
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
using System.Globalization;

namespace Neo.Wallets;

partial class Wallet
{
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
        string resName = $"BIP-39.{culture.Name}";
        string? txt = Resources.ResourceManager.GetString(resName);
        if (txt is null)
            return GetMnemonicCode(entropy, culture.Parent);
        string[] wordlist = txt.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
        return GetMnemonicCode(entropy, wordlist);
    }

    static string[] GetMnemonicCode(ReadOnlySpan<byte> entropy, string[] wordlist)
    {
        if (entropy.Length < 16 || entropy.Length > 32)
            throw new ArgumentException("The length of entropy should be between 128 and 256 bits.", nameof(entropy));
        if (entropy.Length % 4 != 0)
            throw new ArgumentException("The length of entropy should be a multiple of 32 bits.", nameof(entropy));
        int bits_entropy = entropy.Length * 8;
        int bits_checksum = bits_entropy / 32;
        int totalBits = bits_entropy + bits_checksum;
        byte[] checksum = entropy.Sha256();
        int wordCount = totalBits / 11;
        string[] mnemonic = new string[wordCount];
        for (int i = 0; i < wordCount; i++)
        {
            int index = 0;
            for (int j = 0; j < 11; j++)
            {
                int bitPos = i * 11 + j;
                bool bit = bitPos < bits_entropy
                    ? GetBitMSB(entropy, bitPos)
                    : GetBitMSB(checksum, bitPos - bits_entropy);
                if (bit) index |= 1 << (10 - j);
            }
            mnemonic[i] = wordlist[index];
        }
        return mnemonic;
    }

    static bool GetBitMSB(ReadOnlySpan<byte> data, int bitIndex)
    {
        int byteIndex = bitIndex / 8;
        int bitInByte = 7 - (bitIndex % 8); // MSB-first
        return (data[byteIndex] & (1 << bitInByte)) != 0;
    }
}
