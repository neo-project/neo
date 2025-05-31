// Copyright (C) 2015-2025 The Neo Project.
//
// Utility.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.Json;
using Neo.Network.P2P.Payloads;
using Neo.Network.P2P.Payloads.Conditions;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM.Types;
using Neo.Wallets;
using System;
using System.Linq;
using System.Numerics;
using Array = Neo.VM.Types.Array;
using Buffer = Neo.VM.Types.Buffer;

namespace Neo.Network.RPC
{
    public static class Utility
    {
        private static (BigInteger numerator, BigInteger denominator) Fraction(decimal d)
        {
            int[] bits = decimal.GetBits(d);
            BigInteger numerator = (1 - ((bits[3] >> 30) & 2)) *
                                   unchecked(((BigInteger)(uint)bits[2] << 64) |
                                             ((BigInteger)(uint)bits[1] << 32) |
                                              (uint)bits[0]);
            BigInteger denominator = BigInteger.Pow(10, (bits[3] >> 16) & 0xff);
            return (numerator, denominator);
        }

        public static UInt160 ToScriptHash(this JToken value, ProtocolSettings protocolSettings)
        {
            var addressOrScriptHash = value.AsString();

            return addressOrScriptHash.Length < 40 ?
                addressOrScriptHash.ToScriptHash(protocolSettings.AddressVersion) : UInt160.Parse(addressOrScriptHash);
        }

        public static string AsScriptHash(this string addressOrScriptHash)
        {
            foreach (var native in NativeContract.Contracts)
            {
                if (addressOrScriptHash.Equals(native.Name, StringComparison.InvariantCultureIgnoreCase) ||
                    addressOrScriptHash == native.Id.ToString())
                    return native.Hash.ToString();
            }

            return addressOrScriptHash.Length < 40 ?
                addressOrScriptHash : UInt160.Parse(addressOrScriptHash).ToString();
        }

        /// <summary>
        /// Parse WIF or private key hex string to KeyPair
        /// </summary>
        /// <param name="key">WIF or private key hex string
        /// Example: WIF ("KyXwTh1hB76RRMquSvnxZrJzQx7h9nQP2PCRL38v6VDb5ip3nf1p"), PrivateKey ("450d6c2a04b5b470339a745427bae6828400cf048400837d73c415063835e005")</param>
        /// <returns></returns>
        public static KeyPair GetKeyPair(string key)
        {
            if (string.IsNullOrEmpty(key)) { throw new ArgumentNullException(nameof(key)); }
            if (key.StartsWith("0x")) { key = key[2..]; }

            return key.Length switch
            {
                52 => new KeyPair(Wallet.GetPrivateKeyFromWIF(key)),
                64 => new KeyPair(key.HexToBytes()),
                _ => throw new FormatException()
            };
        }

        /// <summary>
        /// Parse address, scripthash or public key string to UInt160
        /// </summary>
        /// <param name="account">account address, scripthash or public key string
        /// Example: address ("Ncm9TEzrp8SSer6Wa3UCSLTRnqzwVhCfuE"), scripthash ("0xb0a31817c80ad5f87b6ed390ecb3f9d312f7ceb8"), public key ("02f9ec1fd0a98796cf75b586772a4ddd41a0af07a1dbdf86a7238f74fb72503575")</param>
        /// <param name="protocolSettings">The protocol settings</param>
        /// <returns></returns>
        public static UInt160 GetScriptHash(string account, ProtocolSettings protocolSettings)
        {
            if (string.IsNullOrEmpty(account)) { throw new ArgumentNullException(nameof(account)); }
            if (account.StartsWith("0x")) { account = account[2..]; }

            return account.Length switch
            {
                34 => account.ToScriptHash(protocolSettings.AddressVersion),
                40 => UInt160.Parse(account),
                66 => Contract.CreateSignatureRedeemScript(ECPoint.Parse(account, ECCurve.Secp256r1)).ToScriptHash(),
                _ => throw new FormatException(),
            };
        }

        /// <summary>
        /// Convert decimal amount to BigInteger: amount * 10 ^ decimals
        /// </summary>
        /// <param name="amount">float value</param>
        /// <param name="decimals">token decimals</param>
        /// <returns></returns>
        public static BigInteger ToBigInteger(this decimal amount, uint decimals)
        {
            BigInteger factor = BigInteger.Pow(10, (int)decimals);
            var (numerator, denominator) = Fraction(amount);
            if (factor < denominator)
            {
                throw new ArgumentException("The decimal places is too long.");
            }

            BigInteger res = factor * numerator / denominator;
            return res;
        }

        public static Block BlockFromJson(JObject json, ProtocolSettings protocolSettings)
        {
            return new Block()
            {
                Header = HeaderFromJson(json, protocolSettings),
                Transactions = ((JArray)json["tx"]).Select(p => TransactionFromJson((JObject)p, protocolSettings)).ToArray()
            };
        }

        public static JObject BlockToJson(Block block, ProtocolSettings protocolSettings)
        {
            JObject json = block.ToJson(protocolSettings);
            json["tx"] = block.Transactions.Select(p => TransactionToJson(p, protocolSettings)).ToArray();
            return json;
        }

        public static Header HeaderFromJson(JObject json, ProtocolSettings protocolSettings)
        {
            return new Header
            {
                Version = (uint)json["version"].AsNumber(),
                PrevHash = UInt256.Parse(json["previousblockhash"].AsString()),
                MerkleRoot = UInt256.Parse(json["merkleroot"].AsString()),
                Timestamp = (ulong)json["time"].AsNumber(),
                Nonce = Convert.ToUInt64(json["nonce"].AsString(), 16),
                Index = (uint)json["index"].AsNumber(),
                PrimaryIndex = (byte)json["primary"].AsNumber(),
                NextConsensus = json["nextconsensus"].ToScriptHash(protocolSettings),
                Witness = ((JArray)json["witnesses"]).Select(p => WitnessFromJson((JObject)p)).FirstOrDefault()
            };
        }

        public static Transaction TransactionFromJson(JObject json, ProtocolSettings protocolSettings)
        {
            return new Transaction
            {
                Version = byte.Parse(json["version"].AsString()),
                Nonce = uint.Parse(json["nonce"].AsString()),
                Signers = ((JArray)json["signers"]).Select(p => SignerFromJson((JObject)p, protocolSettings)).ToArray(),
                SystemFee = long.Parse(json["sysfee"].AsString()),
                NetworkFee = long.Parse(json["netfee"].AsString()),
                ValidUntilBlock = uint.Parse(json["validuntilblock"].AsString()),
                Attributes = ((JArray)json["attributes"]).Select(p => TransactionAttributeFromJson((JObject)p)).ToArray(),
                Script = Convert.FromBase64String(json["script"].AsString()),
                Witnesses = ((JArray)json["witnesses"]).Select(p => WitnessFromJson((JObject)p)).ToArray()
            };
        }

        public static JObject TransactionToJson(Transaction tx, ProtocolSettings protocolSettings)
        {
            JObject json = tx.ToJson(protocolSettings);
            json["sysfee"] = tx.SystemFee.ToString();
            json["netfee"] = tx.NetworkFee.ToString();
            return json;
        }

        public static Signer SignerFromJson(JObject json, ProtocolSettings protocolSettings)
        {
            return new Signer
            {
                Account = json["account"].ToScriptHash(protocolSettings),
                Rules = ((JArray)json["rules"])?.Select(p => RuleFromJson((JObject)p, protocolSettings)).ToArray(),
                Scopes = (WitnessScope)Enum.Parse(typeof(WitnessScope), json["scopes"].AsString()),
                AllowedContracts = ((JArray)json["allowedcontracts"])?.Select(p => p.ToScriptHash(protocolSettings)).ToArray(),
                AllowedGroups = ((JArray)json["allowedgroups"])?.Select(p => ECPoint.Parse(p.AsString(), ECCurve.Secp256r1)).ToArray()
            };
        }

        public static TransactionAttribute TransactionAttributeFromJson(JObject json)
        {
            TransactionAttributeType usage = Enum.Parse<TransactionAttributeType>(json["type"].AsString());
            return usage switch
            {
                TransactionAttributeType.HighPriority => new HighPriorityAttribute(),
                TransactionAttributeType.OracleResponse => new OracleResponse()
                {
                    Id = (ulong)json["id"].AsNumber(),
                    Code = Enum.Parse<OracleResponseCode>(json["code"].AsString()),
                    Result = Convert.FromBase64String(json["result"].AsString()),
                },
                TransactionAttributeType.NotValidBefore => new NotValidBefore()
                {
                    Height = (uint)json["height"].AsNumber(),
                },
                TransactionAttributeType.Conflicts => new Conflicts()
                {
                    Hash = UInt256.Parse(json["hash"].AsString())
                },
                TransactionAttributeType.NotaryAssisted => new NotaryAssisted()
                {
                    NKeys = (byte)json["nkeys"].AsNumber()
                },
                _ => throw new FormatException(),
            };
        }

        public static Witness WitnessFromJson(JObject json)
        {
            return new Witness
            {
                InvocationScript = Convert.FromBase64String(json["invocation"].AsString()),
                VerificationScript = Convert.FromBase64String(json["verification"].AsString())
            };
        }

        public static WitnessRule RuleFromJson(JObject json, ProtocolSettings protocolSettings)
        {
            return new WitnessRule()
            {
                Action = Enum.Parse<WitnessRuleAction>(json["action"].AsString()),
                Condition = RuleExpressionFromJson((JObject)json["condition"], protocolSettings)
            };
        }

        public static WitnessCondition RuleExpressionFromJson(JObject json, ProtocolSettings protocolSettings)
        {
            return json["type"].AsString() switch
            {
                "Or" => new OrCondition { Expressions = ((JArray)json["expressions"])?.Select(p => RuleExpressionFromJson((JObject)p, protocolSettings)).ToArray() },
                "And" => new AndCondition { Expressions = ((JArray)json["expressions"])?.Select(p => RuleExpressionFromJson((JObject)p, protocolSettings)).ToArray() },
                "Boolean" => new BooleanCondition { Expression = json["expression"].AsBoolean() },
                "Not" => new NotCondition { Expression = RuleExpressionFromJson((JObject)json["expression"], protocolSettings) },
                "Group" => new GroupCondition { Group = ECPoint.Parse(json["group"].AsString(), ECCurve.Secp256r1) },
                "CalledByContract" => new CalledByContractCondition { Hash = json["hash"].ToScriptHash(protocolSettings) },
                "ScriptHash" => new ScriptHashCondition { Hash = json["hash"].ToScriptHash(protocolSettings) },
                "CalledByEntry" => new CalledByEntryCondition(),
                "CalledByGroup" => new CalledByGroupCondition { Group = ECPoint.Parse(json["group"].AsString(), ECCurve.Secp256r1) },
                _ => throw new FormatException("Wrong rule's condition type"),
            };
        }

        public static StackItem StackItemFromJson(JObject json)
        {
            StackItemType type = json["type"].GetEnum<StackItemType>();
            switch (type)
            {
                case StackItemType.Boolean:
                    return json["value"].GetBoolean() ? StackItem.True : StackItem.False;
                case StackItemType.Buffer:
                    return new Buffer(Convert.FromBase64String(json["value"].AsString()));
                case StackItemType.ByteString:
                    return new ByteString(Convert.FromBase64String(json["value"].AsString()));
                case StackItemType.Integer:
                    return BigInteger.Parse(json["value"].AsString());
                case StackItemType.Array:
                    Array array = new();
                    foreach (JObject item in (JArray)json["value"])
                        array.Add(StackItemFromJson(item));
                    return array;
                case StackItemType.Struct:
                    Struct @struct = new();
                    foreach (JObject item in (JArray)json["value"])
                        @struct.Add(StackItemFromJson(item));
                    return @struct;
                case StackItemType.Map:
                    Map map = new();
                    foreach (var item in (JArray)json["value"])
                    {
                        PrimitiveType key = (PrimitiveType)StackItemFromJson((JObject)item["key"]);
                        map[key] = StackItemFromJson((JObject)item["value"]);
                    }
                    return map;
                case StackItemType.Pointer:
                    return new Pointer(null, (int)json["value"].AsNumber());
                case StackItemType.InteropInterface:
                    return new InteropInterface(json);
                default:
                    return json["value"]?.AsString() ?? StackItem.Null;
            }
        }

        public static string GetIteratorId(this StackItem item)
        {
            if (item is InteropInterface iop)
            {
                var json = iop.GetInterface<JObject>();
                return json["id"]?.GetString();
            }
            return null;
        }
    }
}
