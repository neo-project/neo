// Copyright (C) 2015-2024 The Neo Project.
//
// RestServerUtility.JTokens.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Network.P2P.Payloads;
using Neo.Network.P2P.Payloads.Conditions;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace Neo.Plugins.RestServer
{
    public static partial class RestServerUtility
    {
        public static JToken BlockHeaderToJToken(Header header, global::Newtonsoft.Json.JsonSerializer serializer) =>
            JToken.FromObject(new
            {
                header.Timestamp,
                header.Version,
                header.PrimaryIndex,
                header.Index,
                header.Nonce,
                header.Hash,
                header.MerkleRoot,
                header.PrevHash,
                header.NextConsensus,
                Witness = WitnessToJToken(header.Witness, serializer),
                header.Size,
            }, serializer);

        public static JToken WitnessToJToken(Witness witness, global::Newtonsoft.Json.JsonSerializer serializer) =>
            JToken.FromObject(new
            {
                witness.InvocationScript,
                witness.VerificationScript,
                witness.ScriptHash,
            }, serializer);

        public static JToken BlockToJToken(Block block, global::Newtonsoft.Json.JsonSerializer serializer) =>
            JToken.FromObject(new
            {
                block.Timestamp,
                block.Version,
                block.PrimaryIndex,
                block.Index,
                block.Nonce,
                block.Hash,
                block.MerkleRoot,
                block.PrevHash,
                block.NextConsensus,
                Witness = WitnessToJToken(block.Witness, serializer),
                block.Size,
                Transactions = block.Transactions.Select(s => TransactionToJToken(s, serializer)),
            }, serializer);

        public static JToken TransactionToJToken(Transaction tx, global::Newtonsoft.Json.JsonSerializer serializer) =>
            JToken.FromObject(new
            {
                tx.Hash,
                tx.Sender,
                tx.Script,
                tx.FeePerByte,
                tx.NetworkFee,
                tx.SystemFee,
                tx.Size,
                tx.Nonce,
                tx.Version,
                tx.ValidUntilBlock,
                Witnesses = tx.Witnesses.Select(s => WitnessToJToken(s, serializer)),
                Signers = tx.Signers.Select(s => SignerToJToken(s, serializer)),
                Attributes = tx.Attributes.Select(s => TransactionAttributeToJToken(s, serializer)),
            }, serializer);

        public static JToken SignerToJToken(Signer signer, global::Newtonsoft.Json.JsonSerializer serializer) =>
            JToken.FromObject(new
            {
                Rules = signer.Rules != null ? signer.Rules.Select(s => WitnessRuleToJToken(s, serializer)) : [],
                signer.Account,
                signer.AllowedContracts,
                signer.AllowedGroups,
                signer.Scopes,
            }, serializer);

        public static JToken TransactionAttributeToJToken(TransactionAttribute attribute, global::Newtonsoft.Json.JsonSerializer serializer) =>
            JToken.FromObject(attribute switch
            {
                Conflicts c => new
                {
                    c.Type,
                    c.Hash,
                    c.Size,
                },
                OracleResponse o => new
                {
                    o.Type,
                    o.Id,
                    o.Code,
                    o.Result,
                    o.Size,
                },
                HighPriorityAttribute h => new
                {
                    h.Type,
                    h.Size,
                },
                NotValidBefore n => new
                {
                    n.Type,
                    n.Height,
                    n.Size,
                },
                _ => new
                {
                    attribute.Type,
                    attribute.Size,
                }
            }, serializer);

        public static JToken WitnessRuleToJToken(WitnessRule rule, global::Newtonsoft.Json.JsonSerializer serializer) =>
            JToken.FromObject(new
            {
                rule.Action,
                Condition = WitnessConditionToJToken(rule.Condition, serializer),
            }, serializer);

        public static JToken WitnessConditionToJToken(WitnessCondition condition, global::Newtonsoft.Json.JsonSerializer serializer)
        {
            JToken j = JValue.CreateNull();
            switch (condition.Type)
            {
                case WitnessConditionType.Boolean:
                    var b = (BooleanCondition)condition;
                    j = JToken.FromObject(new
                    {
                        b.Type,
                        b.Expression,
                    }, serializer);
                    break;
                case WitnessConditionType.Not:
                    var n = (NotCondition)condition;
                    j = JToken.FromObject(new
                    {
                        n.Type,
                        Expression = WitnessConditionToJToken(n.Expression, serializer),
                    }, serializer);
                    break;
                case WitnessConditionType.And:
                    var a = (AndCondition)condition;
                    j = JToken.FromObject(new
                    {
                        a.Type,
                        Expressions = a.Expressions.Select(s => WitnessConditionToJToken(s, serializer)),
                    }, serializer);
                    break;
                case WitnessConditionType.Or:
                    var o = (OrCondition)condition;
                    j = JToken.FromObject(new
                    {
                        o.Type,
                        Expressions = o.Expressions.Select(s => WitnessConditionToJToken(s, serializer)),
                    }, serializer);
                    break;
                case WitnessConditionType.ScriptHash:
                    var s = (ScriptHashCondition)condition;
                    j = JToken.FromObject(new
                    {
                        s.Type,
                        s.Hash,
                    }, serializer);
                    break;
                case WitnessConditionType.Group:
                    var g = (GroupCondition)condition;
                    j = JToken.FromObject(new
                    {
                        g.Type,
                        g.Group,
                    }, serializer);
                    break;
                case WitnessConditionType.CalledByEntry:
                    var e = (CalledByEntryCondition)condition;
                    j = JToken.FromObject(new
                    {
                        e.Type,
                    }, serializer);
                    break;
                case WitnessConditionType.CalledByContract:
                    var c = (CalledByContractCondition)condition;
                    j = JToken.FromObject(new
                    {
                        c.Type,
                        c.Hash,
                    }, serializer);
                    break;
                case WitnessConditionType.CalledByGroup:
                    var p = (CalledByGroupCondition)condition;
                    j = JToken.FromObject(new
                    {
                        p.Type,
                        p.Group,
                    }, serializer);
                    break;
                default:
                    break;
            }
            return j;
        }

        public static JToken ContractStateToJToken(ContractState contract, global::Newtonsoft.Json.JsonSerializer serializer) =>
            JToken.FromObject(new
            {
                contract.Id,
                contract.Manifest.Name,
                contract.Hash,
                Manifest = ContractManifestToJToken(contract.Manifest, serializer),
                NefFile = ContractNefFileToJToken(contract.Nef, serializer),
            }, serializer);

        public static JToken ContractManifestToJToken(ContractManifest manifest, global::Newtonsoft.Json.JsonSerializer serializer) =>
            JToken.FromObject(new
            {
                manifest.Name,
                Abi = ContractAbiToJToken(manifest.Abi, serializer),
                Groups = manifest.Groups.Select(s => ContractGroupToJToken(s, serializer)),
                Permissions = manifest.Permissions.Select(s => ContractPermissionToJToken(s, serializer)),
                Trusts = manifest.Trusts.Select(s => ContractPermissionDescriptorToJToken(s, serializer)),
                manifest.SupportedStandards,
                Extra = manifest.Extra?.Count > 0 ?
                    new JObject(manifest.Extra.Properties.Select(s => new JProperty(s.Key.ToString(), s.Value?.AsString()))) :
                    null,
            }, serializer);

        public static JToken ContractAbiToJToken(ContractAbi abi, global::Newtonsoft.Json.JsonSerializer serializer) =>
            JToken.FromObject(new
            {
                Methods = abi.Methods.Select(s => ContractMethodToJToken(s, serializer)),
                Events = abi.Events.Select(s => ContractEventToJToken(s, serializer)),
            }, serializer);

        public static JToken ContractMethodToJToken(ContractMethodDescriptor method, global::Newtonsoft.Json.JsonSerializer serializer) =>
            JToken.FromObject(new
            {
                method.Name,
                method.Safe,
                method.Offset,
                Parameters = method.Parameters.Select(s => ContractMethodParameterToJToken(s, serializer)),
                method.ReturnType,
            }, serializer);

        public static JToken ContractMethodParameterToJToken(ContractParameterDefinition parameter, global::Newtonsoft.Json.JsonSerializer serializer) =>
            JToken.FromObject(new
            {
                parameter.Type,
                parameter.Name,
            }, serializer);

        public static JToken ContractGroupToJToken(ContractGroup group, global::Newtonsoft.Json.JsonSerializer serializer) =>
            JToken.FromObject(new
            {
                group.PubKey,
                group.Signature,
            }, serializer);

        public static JToken ContractPermissionToJToken(ContractPermission permission, global::Newtonsoft.Json.JsonSerializer serializer) =>
            JToken.FromObject(new
            {
                Contract = ContractPermissionDescriptorToJToken(permission.Contract, serializer),
                Methods = permission.Methods.Count > 0 ?
                    permission.Methods.Select(s => s).ToArray() :
                    (object)"*",
            }, serializer);

        public static JToken ContractPermissionDescriptorToJToken(ContractPermissionDescriptor desc, global::Newtonsoft.Json.JsonSerializer serializer)
        {
            JToken j = JValue.CreateNull();
            if (desc.IsWildcard)
                j = JValue.CreateString("*");
            else if (desc.IsGroup)
                j = JToken.FromObject(new
                {
                    desc.Group
                }, serializer);
            else if (desc.IsHash)
                j = JToken.FromObject(new
                {
                    desc.Hash,
                }, serializer);
            return j;
        }

        public static JToken ContractEventToJToken(ContractEventDescriptor desc, global::Newtonsoft.Json.JsonSerializer serializer) =>
            JToken.FromObject(new
            {
                desc.Name,
                Parameters = desc.Parameters.Select(s => ContractParameterDefinitionToJToken(s, serializer)),
            }, serializer);

        public static JToken ContractParameterDefinitionToJToken(ContractParameterDefinition definition, global::Newtonsoft.Json.JsonSerializer serializer) =>
            JToken.FromObject(new
            {
                definition.Type,
                definition.Name,
            }, serializer);

        public static JToken ContractNefFileToJToken(NefFile nef, global::Newtonsoft.Json.JsonSerializer serializer) =>
            JToken.FromObject(new
            {
                Checksum = nef.CheckSum,
                nef.Compiler,
                nef.Script,
                nef.Source,
                Tokens = nef.Tokens.Select(s => MethodTokenToJToken(s, serializer)),
            }, serializer);

        public static JToken MethodTokenToJToken(MethodToken token, global::Newtonsoft.Json.JsonSerializer serializer) =>
            JToken.FromObject(new
            {
                token.Hash,
                token.Method,
                token.CallFlags,
                token.ParametersCount,
                token.HasReturnValue,
            }, serializer);
    }
}
