// Copyright (C) 2015-2024 The Neo Project.
//
// MainService.Tools.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.IO;
using Neo.ConsoleService;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.SmartContract;
using Neo.VM;
using Neo.Wallets;
using Org.BouncyCastle.Utilities.Encoders;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Neo.CLI
{
    public class ParseFunctionAttribute : Attribute
    {
        public string Description { get; }

        public ParseFunctionAttribute(string description)
        {
            Description = description;
        }
    }

    partial class MainService
    {
        /// <summary>
        /// Process "parse" command
        /// </summary>
        [ConsoleCommand("parse", Category = "Base Commands", Description = "Parse a value to its possible conversions.")]
        private void OnParseCommand(string value)
        {
            value = Base64Fixed(value);

            var parseFunctions = new Dictionary<string, Func<string, string?>>();
            var methods = this.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var method in methods)
            {
                var attribute = method.GetCustomAttribute<ParseFunctionAttribute>();
                if (attribute != null)
                {
                    parseFunctions.Add(attribute.Description, (Func<string, string?>)Delegate.CreateDelegate(typeof(Func<string, string?>), this, method));
                }
            }

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
        /// Converts a hexadecimal value to an UTF-8 string
        /// </summary>
        /// <param name="hexString">
        /// Hexadecimal value to be converted
        /// </param>
        /// <returns>
        /// Returns null when is not possible to parse the hexadecimal value to a UTF-8
        /// string or when the converted string is not printable; otherwise, returns
        /// the string represented by the hexadecimal value
        /// </returns>
        [ParseFunction("Hex String to String")]
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
        /// Converts a little-endian hex string to big-endian hex string
        /// input:  ce616f7f74617e0fc4b805583af2602a238df63f
        /// output: 0x3ff68d232a60f23a5805b8c40f7e61747f6f61ce
        /// </summary>
        /// <param name="hex">Hexadecimal value to be converted</param>
        /// <returns>Returns null when inputs is not little-endian hex string;
        /// otherwise, returns the string that represents the converted big-endian hex string.
        /// </returns>
        [ParseFunction("Little-endian to Big-endian")]
        private string? LittleEndianToBigEndian(string hex)
        {
            try
            {
                var clearHexString = ClearHexString(hex);
                return "0x" + clearHexString.HexToBytes().Reverse().ToArray().ToHexString(); ;
            }
            catch (FormatException)
            {
                return null;
            }
        }

        /// <summary>
        /// Converts a big-endian hex string to little-endian hex string
        /// input:  0x3ff68d232a60f23a5805b8c40f7e61747f6f61ce
        /// output: ce616f7f74617e0fc4b805583af2602a238df63f
        /// </summary>
        /// <param name="hex">Hexadecimal value to be converted</param>
        /// <returns>Returns null when inputs is not big-endian hex string;
        /// otherwise, returns the string that represents the converted little-endian hex string.
        /// </returns>
        [ParseFunction("Big-endian to Little-endian")]
        private string? BigEndianToLittleEndian(string hex)
        {
            return hex.StartsWith("0x") ? hex[2..].HexToBytes().Reverse().ToArray().ToHexString() : null;
        }

        /// <summary>
        /// Converts a hex value to a big integer
        /// </summary>
        /// <param name="hexString">
        /// Hexadecimal value to be converted
        /// </param>
        /// <returns>
        /// Returns null when is not possible to parse the hex value to big integer value;
        /// otherwise, returns the string that represents the converted big integer.
        /// </returns>
        [ParseFunction("Hex String to Big Integer")]
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
        [ParseFunction("String to Hex String")]
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
        [ParseFunction("String to Base64")]
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
        [ParseFunction("Big Integer to Hex String")]
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
        /// Converts a BigInteger to hex string to UTF8 string
        /// input:  860833102
        /// output: NEO3
        /// </summary>
        /// <param name="input">
        /// String that represents the number to be converted
        /// </param>
        /// <returns>
        /// Returns null when the string does not represent a big integer value or when
        /// it is not possible to parse the big integer value to UTF8 string value; otherwise,
        /// returns the string that represents the converted UTF8 string value
        /// </returns>
        [ParseFunction("Network Id to String")]
        private string? NumberToHexToString(string input)
        {
            try
            {
                var hex = NumberToHex(input);
                return hex == null ? null : HexToString(hex);
            }
            catch (Exception)
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
        [ParseFunction("Big Integer to Base64")]
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
        /// Fix for Base64 strings containing unicode
        /// Input  DCECbzTesnBofh/Xng1SofChKkBC7jhVmLxCN1vk\u002B49xa2pBVuezJw==
        /// Output DCECbzTesnBofh/Xng1SofChKkBC7jhVmLxCN1vk+49xa2pBVuezJw==
        /// </summary>
        /// <param name="str">Base64 strings containing unicode</param>
        /// <returns>Correct Base64 string</returns>
        public static string Base64Fixed(string str)
        {
            MatchCollection mc = Regex.Matches(str, @"\\u([\w]{2})([\w]{2})", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            byte[] bts = new byte[2];
            foreach (Match m in mc)
            {
                bts[0] = (byte)int.Parse(m.Groups[2].Value, NumberStyles.HexNumber);
                bts[1] = (byte)int.Parse(m.Groups[1].Value, NumberStyles.HexNumber);
                str = str.Replace(m.ToString(), Encoding.Unicode.GetString(bts));
            }
            return str;
        }

        /// <summary>
        /// Converts an address to its corresponding scripthash (big-endian)
        /// </summary>
        /// <param name="address">
        /// String that represents the address to be converted
        /// </param>
        /// <returns>
        /// Returns null when the string does not represent an address or when
        /// it is not possible to parse the address to scripthash; otherwise returns
        /// the string that represents the converted scripthash
        /// </returns>
        [ParseFunction("Address to ScriptHash (big-endian)")]
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
        /// Converts an address to its corresponding scripthash (little-endian)
        /// </summary>
        /// <param name="address">
        /// String that represents the address to be converted
        /// </param>
        /// <returns>
        /// Returns null when the string does not represent an address or when
        /// it is not possible to parse the address to scripthash; otherwise returns
        /// the string that represents the converted scripthash
        /// </returns>
        [ParseFunction("Address to ScriptHash (little-endian)")]
        private string? AddressToScripthashLE(string address)
        {
            try
            {
                var bigEndScript = address.ToScriptHash(NeoSystem.Settings.AddressVersion);

                return bigEndScript.ToArray().ToHexString();
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
        [ParseFunction("Address to Base64")]
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
        /// Returns null when the string does not represent a scripthash;
        /// otherwise, returns the string that represents the converted address
        /// </returns>
        [ParseFunction("ScriptHash to Address")]
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
        /// Converts a Base64 byte array to address
        /// </summary>
        /// <param name="bytearray">
        /// String that represents the Base64 value
        /// </param>
        /// <returns>
        /// Returns null when the string does not represent a Base64 value or when
        /// it is not possible to parse the Base64 value to address; otherwise,
        /// returns the string that represents the converted address
        /// </returns>
        [ParseFunction("Base64 to Address")]
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
        /// Converts a Base64 hex string to string
        /// </summary>
        /// <param name="bytearray">
        /// String that represents the Base64 value
        /// </param>
        /// <returns>
        /// Returns null when the string does not represent a Base64 value or when
        /// it is not possible to parse the Base64 value to string value or the converted
        /// string is not printable; otherwise, returns the string that represents
        /// the Base64 value.
        /// </returns>
        [ParseFunction("Base64 to String")]
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
        /// Converts a Base64 hex string to big integer value
        /// </summary>
        /// <param name="bytearray">
        /// String that represents the Base64 value
        /// </param>
        /// <returns>
        /// Returns null when the string does not represent a Base64 value or when
        /// it is not possible to parse the Base64 value to big integer value; otherwise
        /// returns the string that represents the converted big integer
        /// </returns>
        [ParseFunction("Base64 to Big Integer")]
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
        /// Converts a Base64 string to Hex string.
        /// input: SGVsbG8gV29ybGQh
        /// output: 48656c6c6f20576f726c6421
        /// </summary>
        /// <param name="base64">
        /// String that represents the Base64 value
        /// </param>
        /// <returns>
        /// Returns null when the string does not represent a Base64 value or when
        /// it is not possible to parse the Base64 value to Hex string; otherwise
        /// returns the string that represents the converted Hex string
        /// </returns>
        [ParseFunction("Base64 to Hex String")]
        public static string? Base64ToHexString(string base64)
        {
            try
            {
                var bytes = Convert.FromBase64String(base64.Trim());
                return bytes.ToHexString();
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Converts a public key to neo3 address.
        /// input:  03dab84c1243ec01ab2500e1a8c7a1546a26d734628180b0cf64e72bf776536997
        /// output: Nd9NceysETPT9PZdWRTeQXJix68WM2x6Wv
        /// </summary>
        /// <param name="pubKey">public key</param>
        /// <returns>Returns null when the string does not represent a public key; otherwise
        /// returns the string that represents the converted neo3 address.
        /// </returns>
        [ParseFunction("Public Key to Address")]
        private string? PublicKeyToAddress(string pubKey)
        {
            pubKey = pubKey.ToLower();
            if (!new Regex("^(0[23][0-9a-f]{64})+$").IsMatch(pubKey)) return null;
            return Contract.CreateSignatureContract(ECPoint.Parse(pubKey, ECCurve.Secp256r1)).ScriptHash.ToAddress(NeoSystem.Settings.AddressVersion);
        }

        /// <summary>
        /// Converts a Private key(WIF) to public key
        /// </summary>
        /// <param name="wif">Private key in WIF format</param>
        /// <returns>Returns null when the string does not represent a private key; otherwise
        /// returns the string that represents the converted public key.
        /// </returns>
        [ParseFunction("WIF to Public Key")]
        public static string? WIFToPublicKey(string wif)
        {
            try
            {
                var privateKey = Wallet.GetPrivateKeyFromWIF(wif);
                var account = new KeyPair(privateKey);
                return account.PublicKey.ToArray().ToHexString();
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Converts a Private key(WIF) to neo3 address
        /// </summary>
        /// <param name="wif">Private key in WIF format</param>
        /// <returns>Returns null when the string does not represent a private key; otherwise
        /// returns the string that represents the converted neo3 address.
        /// </returns>
        [ParseFunction("WIF to Address")]
        public static string? WIFToAddress(string wif)
        {
            try
            {
                var pubKey = WIFToPublicKey(wif);
                return Contract.CreateSignatureContract(ECPoint.Parse(pubKey, ECCurve.Secp256r1)).ScriptHash.ToAddress(0x35);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Converts a Scripts to OpCode
        /// </summary>
        /// <param name="base64">Scripts in Base64 string</param>
        /// <returns>Returns null when the string does not represent a scripts; otherwise
        /// returns the string that represents the converted OpCode.
        /// </returns>
        [ParseFunction("Base64 Smart Contract Script Analysis")]
        private string? ScriptsToOpCode(string base64)
        {
            List<byte> scripts;
            try
            {
                scripts = Convert.FromBase64String(base64).ToList();
            }
            catch (Exception)
            {
                return null;
            }
            try
            {
                _ = new Script(scripts.ToArray(), true);
            }
            catch (Exception)
            {
                return null;
            }
            return ScriptsToOpCode(scripts);
        }

        /// <summary>
        /// Converts a Hex Scripts to OpCode
        /// </summary>
        /// <param name="hex">Scripts in Hex string</param>
        /// <returns>Returns null when the string does not represent a scripts; otherwise
        /// returns the string that represents the converted OpCode.
        /// </returns>
        [ParseFunction("Hex String Smart Contract Script Analysis")]
        private string? HexScriptsToOpCode(string hex)
        {
            List<byte> scripts;
            try
            {
                scripts = hex.HexToBytes().ToList();
            }
            catch (Exception)
            {
                return null;
            }
            try
            {
                _ = new Script(scripts.ToArray(), true);
            }
            catch (Exception)
            {
                return null;
            }
            return ScriptsToOpCode(scripts);
        }

        private string ScriptsToOpCode(List<byte> scripts)
        {
            //Initialize all OpCodes
            var OperandSizePrefixTable = new int[256];
            var OperandSizeTable = new int[256];
            foreach (FieldInfo field in typeof(OpCode).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var attribute = field.GetCustomAttribute<OperandSizeAttribute>();
                if (attribute == null) continue;
                int index = (int)(OpCode)field.GetValue(null);
                OperandSizePrefixTable[index] = attribute.SizePrefix;
                OperandSizeTable[index] = attribute.Size;
            }
            //Initialize all InteropService
            var dic = new Dictionary<uint, string>();
            ApplicationEngine.Services.ToList().ForEach(p => dic.Add(p.Value.Hash, p.Value.Name));

            //Analyzing Scripts
            var result = new List<string>();
            while (scripts.Count > 0)
            {
                var op = (OpCode)scripts[0];
                var operandSizePrefix = OperandSizePrefixTable[scripts[0]];
                var operandSize = OperandSizeTable[scripts[0]];
                scripts.RemoveAt(0);

                var onlyOpCode = true;
                if (operandSize > 0)
                {
                    var operand = scripts.Take(operandSize).ToArray();
                    if (op.ToString().StartsWith("PUSHINT"))
                    {
                        result.Add($"{op} {new BigInteger(operand)}");
                    }
                    else if (op == OpCode.SYSCALL)
                    {
                        result.Add($"{op} {dic[BitConverter.ToUInt32(operand)]}");
                    }
                    else
                    {
                        result.Add($"{op} {operand.ToHexString()}");
                    }
                    scripts.RemoveRange(0, operandSize);
                    onlyOpCode = false;
                }
                if (operandSizePrefix > 0)
                {
                    var bytes = scripts.Take(operandSizePrefix).ToArray();
                    var number = bytes.Length == 1 ? bytes[0] : (int)new BigInteger(bytes);
                    scripts.RemoveRange(0, operandSizePrefix);
                    var operand = scripts.Take(number).ToArray();

                    var asicii = Encoding.Default.GetString(operand);
                    asicii = asicii.Any(p => p < '0' || p > 'z') ? operand.ToHexString() : asicii;

                    result.Add($"{op} {(number == 20 ? new UInt160(operand).ToString() : asicii)}");
                    scripts.RemoveRange(0, number);
                    onlyOpCode = false;
                }
                if (onlyOpCode)
                {
                    result.Add($"{op}");
                }
            }
            return Environment.NewLine + string.Join("\r\n", result.ToArray());
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
