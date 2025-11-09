// Copyright (C) 2015-2025 The Neo Project.
//
// WitnessConditionJsonConverter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using Neo.Network.P2P.Payloads.Conditions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace Neo.Plugins.RestServer.Newtonsoft.Json
{
    public sealed class WitnessConditionJsonConverter : JsonConverter<WitnessCondition>
    {
        public override bool CanWrite => true;
        public override bool CanRead => true;

        public override WitnessCondition ReadJson(JsonReader reader, Type objectType, WitnessCondition? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var token = JToken.ReadFrom(reader);
            if (token.Type == JTokenType.Object)
                return FromJson((JObject)token);
            throw new NotSupportedException($"{nameof(WitnessCondition)} Type({token.Type}) is not supported from JSON.");
        }

        public override void WriteJson(JsonWriter writer, WitnessCondition? value, JsonSerializer serializer)
        {
            ArgumentNullException.ThrowIfNull(value);

            var j = RestServerUtility.WitnessConditionToJToken(value, serializer);
            j.WriteTo(writer);
        }

        public static WitnessCondition FromJson(JObject json)
        {
            ArgumentNullException.ThrowIfNull(json, nameof(json));

            var typeProp = json.Properties().Single(s => EqualsIgnoreCase(s.Name, "type"));
            var typeValue = typeProp.Value<string>();

            try
            {
                if (typeValue is null) throw new ArgumentNullException(nameof(json), "no 'type' in json");

                var type = Enum.Parse<WitnessConditionType>(typeValue);

                switch (type)
                {
                    case WitnessConditionType.Boolean:
                        var valueProp = json.Properties().Single(s => EqualsIgnoreCase(s.Name, "expression"));
                        return new BooleanCondition() { Expression = valueProp.Value<bool>() };
                    case WitnessConditionType.Not:
                        valueProp = json.Properties().Single(s => EqualsIgnoreCase(s.Name, "expression"));
                        return new NotCondition() { Expression = FromJson((JObject)valueProp.Value) };
                    case WitnessConditionType.And:
                        valueProp = json.Properties().Single(s => EqualsIgnoreCase(s.Name, "expressions"));
                        if (valueProp.Type == JTokenType.Array)
                        {
                            var array = (JArray)valueProp.Value;
                            return new AndCondition() { Expressions = array.Select(s => FromJson((JObject)s)).ToArray() };
                        }
                        break;
                    case WitnessConditionType.Or:
                        valueProp = json.Properties().Single(s => EqualsIgnoreCase(s.Name, "expressions"));
                        if (valueProp.Type == JTokenType.Array)
                        {
                            var array = (JArray)valueProp.Value;
                            return new OrCondition() { Expressions = array.Select(s => FromJson((JObject)s)).ToArray() };
                        }
                        break;
                    case WitnessConditionType.ScriptHash:
                        valueProp = json.Properties().Single(s => EqualsIgnoreCase(s.Name, "hash"));
                        return new ScriptHashCondition() { Hash = UInt160.Parse(valueProp.Value<string>()!) };
                    case WitnessConditionType.Group:
                        valueProp = json.Properties().Single(s => EqualsIgnoreCase(s.Name, "group"));
                        return new GroupCondition() { Group = ECPoint.Parse(valueProp.Value<string>() ?? throw new NullReferenceException("In the witness json data, group is null."), ECCurve.Secp256r1) };
                    case WitnessConditionType.CalledByEntry:
                        return new CalledByEntryCondition();
                    case WitnessConditionType.CalledByContract:
                        valueProp = json.Properties().Single(s => EqualsIgnoreCase(s.Name, "hash"));
                        return new CalledByContractCondition { Hash = UInt160.Parse(valueProp.Value<string>()!) };
                    case WitnessConditionType.CalledByGroup:
                        valueProp = json.Properties().Single(s => EqualsIgnoreCase(s.Name, "group"));
                        return new CalledByGroupCondition { Group = ECPoint.Parse(valueProp.Value<string>() ?? throw new NullReferenceException("In the witness json data, group is null."), ECCurve.Secp256r1) };
                }
            }
            catch (ArgumentNullException ex)
            {
                throw new NotSupportedException($"{ex.ParamName} is not supported from JSON.");
            }
            throw new NotSupportedException($"WitnessConditionType({typeValue}) is not supported from JSON.");
        }

        private static bool EqualsIgnoreCase(string left, string right) =>
            left.Equals(right, StringComparison.InvariantCultureIgnoreCase);
    }
}
