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

using Neo.ConsoleService;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.SmartContract;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

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
            value = Base64Fixed(value);

            var parseFunctions = new Dictionary<string, Func<string, string?>>();
            var methods = GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var method in methods)
            {
                var attribute = method.GetCustomAttribute<ParseFunctionAttribute>();
                if (attribute != null)
                {
                    parseFunctions.Add(attribute.Description, (Func<string, string?>)Delegate.CreateDelegate(typeof(Func<string, string?>), this, method));
                }
            }

            var any = false;

            foreach (var pair in parseFunctions)
            {
                var parseMethod = pair.Value;
                var result = parseMethod(value);

                if (result != null)
                {
                    ConsoleHelper.Info("", "-----", pair.Key, "-----");
                    ConsoleHelper.Info("", result, Environment.NewLine);
                    any = true;
                }
            }

            if (!any)
            {
                ConsoleHelper.Warning($"Was not possible to convert: '{value}'");
            }
        }

        /// <summary>
        /// Little-endian to Big-endian
        /// input:  ce616f7f74617e0fc4b805583af2602a238df63f
        /// output: 0x3ff68d232a60f23a5805b8c40f7e61747f6f61ce
        /// </summary>
        [ParseFunction("Little-endian to Big-endian")]
        private string? LittleEndianToBigEndian(string hex)
        {
            try
            {
                if (!IsHex(hex)) return null;
                return "0x" + hex.HexToBytes().Reverse().ToArray().ToHexString();
            }
            catch (FormatException)
            {
                return null;
            }
        }

        /// <summary>
        /// Big-endian to Little-endian
        /// input:  0x3ff68d232a60f23a5805b8c40f7e61747f6f61ce
        /// output: ce616f7f74617e0fc4b805583af2602a238df63f
        /// </summary>
        [ParseFunction("Big-endian to Little-endian")]
        private string? BigEndianToLittleEndian(string hex)
        {
            try
            {
                var hasHexPrefix = hex.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase);
                hex = hasHexPrefix ? hex[2..] : hex;
                if (!hasHexPrefix || !IsHex(hex)) return null;
                return hex.HexToBytes().Reverse().ToArray().ToHexString();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// String to Base64
        /// input:  Hello World!
        /// output: SGVsbG8gV29ybGQh
        /// </summary>
        [ParseFunction("String to Base64")]
        private string? StringToBase64(string strParam)
        {
            try
            {
                var bytearray = Utility.StrictUTF8.GetBytes(strParam);
                return Convert.ToBase64String(bytearray.AsSpan());
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Big Integer to Base64
        /// input:  123456
        /// output: QOIB
        /// </summary>
        [ParseFunction("Big Integer to Base64")]
        private string? NumberToBase64(string strParam)
        {
            try
            {
                if (!BigInteger.TryParse(strParam, out var number))
                {
                    return null;
                }
                var bytearray = number.ToByteArray();
                return Convert.ToBase64String(bytearray.AsSpan());
            }
            catch
            {
                return null;
            }
        }

        private static bool IsHex(string str) => str.Length % 2 == 0 && str.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'));

        /// <summary>
        /// Fix for Base64 strings containing unicode
        /// input:  DCECbzTesnBofh/Xng1SofChKkBC7jhVmLxCN1vk\u002B49xa2pBVuezJw==
        /// output: DCECbzTesnBofh/Xng1SofChKkBC7jhVmLxCN1vk+49xa2pBVuezJw==
        /// </summary>
        /// <param name="str">Base64 strings containing unicode</param>
        /// <returns>Correct Base64 string</returns>
        private static string Base64Fixed(string str)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < str.Length; i++)
            {
                if (str[i] == '\\' && i + 5 < str.Length && str[i + 1] == 'u')
                {
                    var hex = str.Substring(i + 2, 4);
                    if (IsHex(hex))
                    {
                        var bts = new byte[2];
                        bts[0] = (byte)int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
                        bts[1] = (byte)int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
                        sb.Append(Encoding.Unicode.GetString(bts));
                        i += 5;
                    }
                    else
                    {
                        sb.Append(str[i]);
                    }
                }
                else
                {
                    sb.Append(str[i]);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Address to ScriptHash (big-endian)
        /// input:  NejD7DJWzD48ZG4gXKDVZt3QLf1fpNe1PF
        /// output: 0x3ff68d232a60f23a5805b8c40f7e61747f6f61ce
        /// </summary>
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
        /// Address to ScriptHash (blittleig-endian)
        /// input:  NejD7DJWzD48ZG4gXKDVZt3QLf1fpNe1PF
        /// output: ce616f7f74617e0fc4b805583af2602a238df63f
        /// </summary>
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
        /// Address to Base64
        /// input:  NejD7DJWzD48ZG4gXKDVZt3QLf1fpNe1PF
        /// output: zmFvf3Rhfg/EuAVYOvJgKiON9j8=
        /// </summary>
        [ParseFunction("Address to Base64")]
        private string? AddressToBase64(string address)
        {
            try
            {
                var script = address.ToScriptHash(NeoSystem.Settings.AddressVersion);
                return Convert.ToBase64String(script.ToArray().AsSpan());
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// ScriptHash to Address
        /// input:  0x3ff68d232a60f23a5805b8c40f7e61747f6f61ce
        /// output: NejD7DJWzD48ZG4gXKDVZt3QLf1fpNe1PF
        /// </summary>
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
                    var bigEndScript = littleEndScript.ToArray().ToHexString();
                    if (!UInt160.TryParse(bigEndScript, out scriptHash))
                    {
                        return null;
                    }
                }

                return scriptHash.ToAddress(NeoSystem.Settings.AddressVersion);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Base64 to Address
        /// input:  zmFvf3Rhfg/EuAVYOvJgKiON9j8=
        /// output: NejD7DJWzD48ZG4gXKDVZt3QLf1fpNe1PF
        /// </summary>
        [ParseFunction("Base64 to Address")]
        private string? Base64ToAddress(string bytearray)
        {
            try
            {
                var result = Convert.FromBase64String(bytearray).Reverse().ToArray();
                var hex = result.ToHexString();

                if (!UInt160.TryParse(hex, out var scripthash))
                {
                    return null;
                }

                return scripthash.ToAddress(NeoSystem.Settings.AddressVersion);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Base64 to String
        /// input:  SGVsbG8gV29ybGQh
        /// output: Hello World!
        /// </summary>
        [ParseFunction("Base64 to String")]
        private string? Base64ToString(string bytearray)
        {
            try
            {
                var result = Convert.FromBase64String(bytearray);
                var utf8String = Utility.StrictUTF8.GetString(result);
                return IsPrintable(utf8String) ? utf8String : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Base64 to Big Integer
        /// input:  QOIB
        /// output: 123456
        /// </summary>
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
        /// Public Key to Address
        /// input:  03dab84c1243ec01ab2500e1a8c7a1546a26d734628180b0cf64e72bf776536997
        /// output: NU7RJrzNgCSnoPLxmcY7C72fULkpaGiSpJ
        /// </summary>
        [ParseFunction("Public Key to Address")]
        private string? PublicKeyToAddress(string pubKey)
        {
            if (ECPoint.TryParse(pubKey, ECCurve.Secp256r1, out var publicKey) == false)
                return null;
            return Contract.CreateSignatureContract(publicKey)
                .ScriptHash
                .ToAddress(NeoSystem.Settings.AddressVersion);
        }

        /// <summary>
        /// WIF to Public Key
        /// </summary>
        [ParseFunction("WIF to Public Key")]
        private string? WIFToPublicKey(string wif)
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
        /// WIF to Address
        /// </summary>
        [ParseFunction("WIF to Address")]
        private string? WIFToAddress(string wif)
        {
            try
            {
                var pubKey = WIFToPublicKey(wif);
                return Contract.CreateSignatureContract(ECPoint.Parse(pubKey, ECCurve.Secp256r1)).ScriptHash.ToAddress(NeoSystem.Settings.AddressVersion);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Base64 Smart Contract Script Analysis
        /// input: DARkYXRhAgBlzR0MFPdcrAXPVptVduMEs2lf1jQjxKIKDBT3XKwFz1abVXbjBLNpX9Y0I8SiChTAHwwIdHJhbnNmZXIMFKNSbimM12LkFYX/8KGvm2ttFxulQWJ9W1I=
        /// output:
        /// PUSHDATA1 data
        /// PUSHINT32 500000000
        /// PUSHDATA1 0x0aa2c42334d65f69b304e376559b56cf05ac5cf7
        /// PUSHDATA1 0x0aa2c42334d65f69b304e376559b56cf05ac5cf7
        /// PUSH4
        /// PACK
        /// PUSH15
        /// PUSHDATA1 transfer
        /// PUSHDATA1 0xa51b176d6b9bafa1f0ff8515e462d78c296e52a3
        /// SYSCALL System.Contract.Call
        /// </summary>
        [ParseFunction("Base64 Smart Contract Script Analysis")]
        private string? ScriptsToOpCode(string base64)
        {
            try
            {
                var bytes = Convert.FromBase64String(base64);
                var sb = new StringBuilder();
                var line = 0;

                foreach (var instruct in new VMInstruction(bytes))
                {
                    if (instruct.OperandSize == 0)
                        sb.AppendFormat("L{0:D04}:{1:X04} {2}{3}", line, instruct.Position, instruct.OpCode, Environment.NewLine);
                    else
                        sb.AppendFormat("L{0:D04}:{1:X04} {2,-10}{3}{4}", line, instruct.Position, instruct.OpCode, DecodeOperand(instruct), Environment.NewLine);
                    line++;
                }

                return sb.ToString();
            }
            catch
            {
                return null;
            }
        }

        private string DecodeOperand(VMInstruction instruction)
        {
            var operand = instruction.Operand[instruction.OperandPrefixSize..].ToArray();
            var asStr = Encoding.UTF8.GetString(operand);
            var readable = asStr.All(char.IsAscii);
            return instruction.OpCode switch
            {
                VM.OpCode.JMP or
                VM.OpCode.JMPIF or
                VM.OpCode.JMPIFNOT or
                VM.OpCode.JMPEQ or
                VM.OpCode.JMPNE or
                VM.OpCode.JMPGT or
                VM.OpCode.JMPLT or
                VM.OpCode.CALL or
                VM.OpCode.ENDTRY => $"[{checked(instruction.Position + instruction.AsToken<sbyte>()):X02}]",
                VM.OpCode.PUSHA or
                VM.OpCode.JMP_L or
                VM.OpCode.JMPIF_L or
                VM.OpCode.JMPIFNOT_L or
                VM.OpCode.JMPEQ_L or
                VM.OpCode.JMPNE_L or
                VM.OpCode.JMPGT_L or
                VM.OpCode.JMPLT_L or
                VM.OpCode.CALL_L or
                VM.OpCode.ENDTRY_L => $"[{checked(instruction.Position + instruction.AsToken<int>()):X04}]",
                VM.OpCode.TRY or
                VM.OpCode.INITSLOT => $"{instruction.AsToken<byte>()}, {instruction.AsToken<byte>(1)}",
                VM.OpCode.TRY_L => $"[{checked(instruction.Position + instruction.AsToken<int>()):X04}, {checked(instruction.Position + instruction.AsToken<int>()):X04}]",
                VM.OpCode.NEWARRAY_T or
                VM.OpCode.ISTYPE or
                VM.OpCode.CONVERT => $"{instruction.AsToken<byte>():X02}",
                VM.OpCode.STLOC or
                VM.OpCode.LDLOC or
                VM.OpCode.LDSFLD or
                VM.OpCode.STSFLD or
                VM.OpCode.LDARG or
                VM.OpCode.STARG or
                VM.OpCode.INITSSLOT => $"{instruction.AsToken<byte>()}",
                VM.OpCode.PUSHINT8 => $"{instruction.AsToken<sbyte>()}",
                VM.OpCode.PUSHINT16 => $"{instruction.AsToken<short>()}",
                VM.OpCode.PUSHINT32 => $"{instruction.AsToken<int>()}",
                VM.OpCode.PUSHINT64 => $"{instruction.AsToken<long>()}",
                VM.OpCode.PUSHINT128 or
                VM.OpCode.PUSHINT256 => $"{new BigInteger(operand)}",
                VM.OpCode.SYSCALL => $"[{ApplicationEngine.Services[Unsafe.As<byte, uint>(ref operand[0])].Name}]",
                VM.OpCode.PUSHDATA1 or
                VM.OpCode.PUSHDATA2 or
                VM.OpCode.PUSHDATA4 => readable ? $"{Convert.ToHexString(operand)} // {asStr}" : Convert.ToHexString(operand),
                _ => readable ? $"\"{asStr}\"" : $"{Convert.ToHexString(operand)} // {asStr}",
            };
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
        private static bool IsPrintable(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && value.Any(c => !char.IsControl(c));
        }
    }
}
