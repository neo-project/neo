// Copyright (C) 2015-2024 The Neo Project.
//
// SignerOrWitness.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using Neo.Json;
using Neo.Network.P2P.Payloads;
using Neo.Wallets;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Neo.Plugins.RpcServer.Model;

public class SignerOrWitness
{
    private readonly object _value;

    public SignerOrWitness(Signer signer)
    {
        _value = signer ?? throw new ArgumentNullException(nameof(signer));
    }

    public SignerOrWitness(Witness witness)
    {
        _value = witness ?? throw new ArgumentNullException(nameof(witness));
    }

    public bool IsSigner => _value is Signer;

    public static bool TryParse(JToken value, ProtocolSettings settings, [NotNullWhen(true)] out SignerOrWitness? signerOrWitness)
    {
        signerOrWitness = null;

        if (value == null)
            return false;

        if (value is JObject jObject)
        {
            if (jObject.ContainsProperty("account"))
            {
                signerOrWitness = new SignerOrWitness(SignerFromJson(jObject, settings));
                return true;
            }
            else if (jObject.ContainsProperty("invocation") || jObject.ContainsProperty("verification"))
            {
                signerOrWitness = new SignerOrWitness(WitnessFromJson(jObject));
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
            Scopes = jObject.ContainsProperty("scopes")
                ? (WitnessScope)Enum.Parse(typeof(WitnessScope), jObject["scopes"].AsString())
                : WitnessScope.CalledByEntry,
            AllowedContracts = ((JArray)jObject["allowedcontracts"])?.Select(p => UInt160.Parse(p.AsString())).ToArray() ?? Array.Empty<UInt160>(),
            AllowedGroups = ((JArray)jObject["allowedgroups"])?.Select(p => ECPoint.Parse(p.AsString(), ECCurve.Secp256r1)).ToArray() ?? Array.Empty<ECPoint>(),
            Rules = ((JArray)jObject["rules"])?.Select(r => WitnessRule.FromJson((JObject)r)).ToArray() ?? Array.Empty<WitnessRule>()
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

    public static SignerOrWitness[] ParseArray(JArray array, ProtocolSettings settings)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));

        if (array.Count > Transaction.MaxTransactionAttributes)
            throw new RpcException(RpcError.InvalidParams.WithData("Max allowed signers or witnesses exceeded."));

        return array.Select(item =>
        {
            if (TryParse(item, settings, out var signerOrWitness))
                return signerOrWitness;
            throw new ArgumentException($"Invalid signer or witness format: {item}");
        }).ToArray();
    }

    public Signer AsSigner()
    {
        return _value as Signer ?? throw new InvalidOperationException("The value is not a Signer.");
    }

    public Witness AsWitness()
    {
        return _value as Witness ?? throw new InvalidOperationException("The value is not a Witness.");
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
