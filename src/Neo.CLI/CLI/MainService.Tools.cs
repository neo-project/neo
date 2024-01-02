// Copyright (C) 2016-2023 The Neo Project.
// 
// The neo-cli is free software distributed under the MIT software 
// license, see the accompanying file LICENSE in the main directory of
// the project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.ConsoleService;
using Neo.IO;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Neo.CLI
{
    partial class MainService
    {
        /// <summary>
        /// Process "parse" command
        /// </summary>
        [ConsoleCommand("parse", Category = "Base Commands", Description = "Parse a value to its possible conversions.")]
        private void OnParseCommand(string value)
        {
            var parseFunctions = new Dictionary<string, Func<string, string?>>()
            {
                { "Address to ScriptHash", AddressToScripthash },
                { "Address to Base64", AddressToBase64 },
                { "ScriptHash to Address", ScripthashToAddress },
                { "Base64 to Address", Base64ToAddress },
                { "Base64 to String", Base64ToString },
                { "Base64 to Big Integer", Base64ToNumber },
                { "Big Integer to Hex String", NumberToHex },
                { "Big Integer to Base64", NumberToBase64 },
                { "Hex String to String", HexToString },
                { "Hex String to Big Integer", HexToNumber },
                { "String to Hex String", StringToHex },
                { "String to Base64", StringToBase64 }
            };

            bool any = false;

            foreach (var pair in parseFunctions)
            {
                var parseMethod = pair.Value;
                var result = parseMethod(value);

                if (result != null)
                {
                    Console.WriteLine($"{pair.Key,-30}\t{result}");
                    any = true;
                }
            }

            if (!any)
            {
                ConsoleHelper.Warning($"Was not possible to convert: '{value}'");
            }
        }

        /// <summary>
        /// Converts an hexadecimal value to an UTF-8 string
        /// </summary>
        /// <param name="hexString">
        /// Hexadecimal value to be converted
        /// </param>
        /// <returns>
        /// Returns null when is not possible to parse the hexadecimal value to a UTF-8
        /// string or when the converted string is not printable; otherwise, returns
        /// the string represented by the hexadecimal value
        /// </returns>
        private string? HexToString(string hexString)
        {
            try
            {
                var clearHexString = ClearHexString(hexString);
                var bytes = clearHexString.HexToBytes();
                var utf8String = Utility.StrictUTF8.GetString(bytes);
                return IsPrintable(utf8String) ? utf8String : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Converts an hex value to a big integer
        /// </summary>
        /// <param name="hexString">
        /// Hexadecimal value to be converted
        /// </param>
        /// <returns>
        /// Returns null when is not possible to parse the hex value to big integer value;
        /// otherwise, returns the string that represents the converted big integer.
        /// </returns>
        private string? HexToNumber(string hexString)
        {
            try
            {
                var clearHexString = ClearHexString(hexString);
                var bytes = clearHexString.HexToBytes();
                var number = new BigInteger(bytes);

                return number.ToString();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Formats a string value to a default hexadecimal representation of a byte array
        /// </summary>
        /// <param name="hexString">
        /// The string value to be formatted
        /// </param>
        /// <returns>
        /// Returns the formatted string.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Throw when is the string is not a valid hex representation of a byte array.
        /// </exception>
        private string ClearHexString(string hexString)
        {
            bool hasHexPrefix = hexString.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase);

            try
            {
                if (hasHexPrefix)
                {
                    hexString = hexString.Substring(2);
                }

                if (hexString.Length % 2 == 1)
                {
                    // if the length is an odd number, it cannot be parsed to a byte array
                    // it may be a valid hex string, so include a leading zero to parse correctly
                    hexString = "0" + hexString;
                }

                if (hasHexPrefix)
                {
                    // if the input value starts with '0x', the first byte is the less significant
                    // to parse correctly, reverse the byte array
                    return hexString.HexToBytes().Reverse().ToArray().ToHexString();
                }
            }
            catch (FormatException)
            {
                throw new ArgumentException();
            }

            return hexString;
        }

        /// <summary>
        /// Converts a string in a hexadecimal value
        /// </summary>
        /// <param name="strParam">
        /// String value to be converted
        /// </param>
        /// <returns>
        /// Returns null when it is not possible to parse the string value to a hexadecimal
        /// value; otherwise returns the hexadecimal value that represents the converted string
        /// </returns>
        private string? StringToHex(string strParam)
        {
            try
            {
                var bytesParam = Utility.StrictUTF8.GetBytes(strParam);
                return bytesParam.ToHexString();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Converts a string in Base64 string
        /// </summary>
        /// <param name="strParam">
        /// String value to be converted
        /// </param>
        /// <returns>
        /// Returns null when is not possible to parse the string value to a Base64 value;
        /// otherwise returns the Base64 value that represents the converted string
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Throw .
        /// </exception>
        private string? StringToBase64(string strParam)
        {
            try
            {
                byte[] bytearray = Utility.StrictUTF8.GetBytes(strParam);
                string base64 = Convert.ToBase64String(bytearray.AsSpan());
                return base64;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Converts a string number in hexadecimal format
        /// </summary>
        /// <param name="strParam">
        /// String that represents the number to be converted
        /// </param>
        /// <returns>
        /// Returns null when the string does not represent a big integer value or when
        /// it is not possible to parse the big integer value to hexadecimal; otherwise,
        /// returns the string that represents the converted hexadecimal value
        /// </returns>
        private string? NumberToHex(string strParam)
        {
            try
            {
                if (!BigInteger.TryParse(strParam, out var numberParam))
                {
                    return null;
                }
                return numberParam.ToByteArray().ToHexString();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Converts a string number in Base64 byte array
        /// </summary>
        /// <param name="strParam">
        /// String that represents the number to be converted
        /// </param>
        /// <returns>
        /// Returns null when the string does not represent a big integer value or when
        /// it is not possible to parse the big integer value to Base64 value; otherwise,
        /// returns the string that represents the converted Base64 value
        /// </returns>
        private string? NumberToBase64(string strParam)
        {
            try
            {
                if (!BigInteger.TryParse(strParam, out var number))
                {
                    return null;
                }
                byte[] bytearray = number.ToByteArray();
                string base64 = Convert.ToBase64String(bytearray.AsSpan());

                return base64;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Converts an address to its corresponding scripthash
        /// </summary>
        /// <param name="address">
        /// String that represents the address to be converted
        /// </param>
        /// <returns>
        /// Returns null when the string does not represent an address or when
        /// it is not possible to parse the address to scripthash; otherwise returns
        /// the string that represents the converted scripthash
        /// </returns>
        private string? AddressToScripthash(string address)
        {
            try
            {
                var bigEndScript = address.ToScriptHash(NeoSystem.Settings.AddressVersion);

                return bigEndScript.ToString();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Converts an address to Base64 byte array
        /// </summary>
        /// <param name="address">
        /// String that represents the address to be converted
        /// </param>
        /// <returns>
        /// Returns null when the string does not represent an address or when it is 
        /// not possible to parse the address to Base64 value; otherwise returns
        /// the string that represents the converted Base64 value.
        /// </returns>
        private string? AddressToBase64(string address)
        {
            try
            {
                var script = address.ToScriptHash(NeoSystem.Settings.AddressVersion);
                string base64 = Convert.ToBase64String(script.ToArray().AsSpan());

                return base64;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Converts a big end script hash to its equivalent address
        /// </summary>
        /// <param name="script">
        /// String that represents the scripthash to be converted
        /// </param>
        /// <returns>
        /// Returns null when the string does not represent an scripthash;
        /// otherwise, returns the string that represents the converted address
        /// </returns>
        private string? ScripthashToAddress(string script)
        {
            try
            {
                UInt160 scriptHash;
                if (script.StartsWith("0x"))
                {
                    if (!UInt160.TryParse(script, out scriptHash))
                    {
                        return null;
                    }
                }
                else
                {
                    if (!UInt160.TryParse(script, out UInt160 littleEndScript))
                    {
                        return null;
                    }
                    string bigEndScript = littleEndScript.ToArray().ToHexString();
                    if (!UInt160.TryParse(bigEndScript, out scriptHash))
                    {
                        return null;
                    }
                }

                var hexScript = scriptHash.ToAddress(NeoSystem.Settings.AddressVersion);
                return hexScript;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Converts an Base64 byte array to address
        /// </summary>
        /// <param name="bytearray">
        /// String that represents the Base64 value
        /// </param>
        /// <returns>
        /// Returns null when the string does not represent an Base64 value or when
        /// it is not possible to parse the Base64 value to address; otherwise,
        /// returns the string that represents the converted address
        /// </returns>
        private string? Base64ToAddress(string bytearray)
        {
            try
            {
                byte[] result = Convert.FromBase64String(bytearray).Reverse().ToArray();
                string hex = result.ToHexString();

                if (!UInt160.TryParse(hex, out var scripthash))
                {
                    return null;
                }

                string address = scripthash.ToAddress(NeoSystem.Settings.AddressVersion);
                return address;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Converts an Base64 hex string to string
        /// </summary>
        /// <param name="bytearray">
        /// String that represents the Base64 value
        /// </param>
        /// <returns>
        /// Returns null when the string does not represent an Base64 value or when
        /// it is not possible to parse the Base64 value to string value or the converted
        /// string is not printable; otherwise, returns the string that represents
        /// the Base64 value.
        /// </returns>
        private string? Base64ToString(string bytearray)
        {
            try
            {
                byte[] result = Convert.FromBase64String(bytearray);
                string utf8String = Utility.StrictUTF8.GetString(result);
                return IsPrintable(utf8String) ? utf8String : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Converts an Base64 hex string to big integer value
        /// </summary>
        /// <param name="bytearray">
        /// String that represents the Base64 value
        /// </param>
        /// <returns>
        /// Returns null when the string does not represent an Base64 value or when
        /// it is not possible to parse the Base64 value to big integer value; otherwise
        /// returns the string that represents the converted big integer
        /// </returns>
        private string? Base64ToNumber(string bytearray)
        {
            try
            {
                var bytes = Convert.FromBase64String(bytearray);
                var number = new BigInteger(bytes);
                return number.ToString();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Checks if the string is null or cannot be printed.
        /// </summary>
        /// <param name="value">
        /// The string to test
        /// </param>
        /// <returns>
        /// Returns false if the string is null, or if it is empty, or if each character cannot be printed;
        /// otherwise, returns true.
        /// </returns>
        private bool IsPrintable(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && value.Any(c => !char.IsControl(c));
        }
    }
}
