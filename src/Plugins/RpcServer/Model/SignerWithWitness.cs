// Copyright (C) 2015-2024 The Neo Project.
//
// SignerWithWitness.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable

using Neo.Cryptography.ECC;
using Neo.Json;
using Neo.Network.P2P.Payloads;
using Neo.Wallets;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Neo.Plugins.RpcServer.Model;

public class SignerWithWitness(Signer? signer, Witness? witness)
{
    public Signer? Signer { get; } = signer;
    public Witness? Witness { get; } = witness;

    public static bool TryParse(JToken value, ProtocolSettings settings, [NotNullWhen(true)] out SignerWithWitness? signerWithWitness)
    {
        signerWithWitness = null;

        if (value == null)
            return false;

        if (value is JObject jObject)
        {
            Signer? signer = null;
            Witness? witness = null;

            if (jObject.ContainsProperty("account"))
            {
                signer = SignerFromJson(jObject, settings);
            }
            if (jObject.ContainsProperty("invocation") || jObject.ContainsProperty("verification"))
            {
                witness = WitnessFromJson(jObject);
            }

            if (signer != null || witness != null)
            {
                signerWithWitness = new SignerWithWitness(signer, witness);
                return true;
            }
        }

        return false;
    }

    private static Signer SignerFromJson(JObject jObject, ProtocolSettings settings)
    {
        return new Signer
        {
            Account = AddressToScriptHash(jObject["account"].AsString(), settings.AddressVersion),
            Scopes = (WitnessScope)Enum.Parse(typeof(WitnessScope), jObject["scopes"]?.AsString()),
            AllowedContracts = ((JArray)jObject["allowedcontracts"])?.Select(p => UInt160.Parse(p.AsString())).ToArray() ?? Array.Empty<UInt160>(),
            AllowedGroups = ((JArray)jObject["allowedgroups"])?.Select(p => ECPoint.Parse(p.AsString(), ECCurve.Secp256r1)).ToArray() ?? Array.Empty<ECPoint>(),
            Rules = ((JArray)jObject["rules"])?.Select(r => WitnessRule.FromJson((JObject)r)).ToArray() ?? Array.Empty<WitnessRule>(),
        };
    }

    private static Witness WitnessFromJson(JObject jObject)
    {
        return new Witness
        {
            InvocationScript = Convert.FromBase64String(jObject["invocation"]?.AsString() ?? string.Empty),
            VerificationScript = Convert.FromBase64String(jObject["verification"]?.AsString() ?? string.Empty)
        };
    }

    public static SignerWithWitness[] ParseArray(JArray array, ProtocolSettings settings)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));

        if (array.Count > Transaction.MaxTransactionAttributes)
            throw new RpcException(RpcError.InvalidParams.WithData("Max allowed signers or witnesses exceeded."));

        return array.Select(item =>
        {
            if (TryParse(item, settings, out var signerWithWitness))
                return signerWithWitness;
            throw new ArgumentException($"Invalid signer or witness format: {item}");
        }).ToArray();
    }

    private static UInt160 AddressToScriptHash(string address, byte version)
    {
        if (UInt160.TryParse(address, out var scriptHash))
        {
            return scriptHash;
        }

        return address.ToScriptHash(version);
    }
}
