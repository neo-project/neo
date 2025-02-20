// Copyright (C) 2015-2025 The Neo Project.
//
// JsonStringAddressConverter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Exceptions;
using Neo.Wallets;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neo.Build.Core.Json.Converters
{
    public class JsonStringAddressConverter : JsonConverter<UInt160?>
    {
        public override UInt160? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
                throw new NeoBuildInvalidAddressFormatException();

            var valueString = reader.GetString();

            if (string.IsNullOrEmpty(valueString))
                return default;

            try
            {
                return valueString.ToScriptHash(ProtocolSettings.Default.AddressVersion);
            }
            catch (FormatException)
            {
                throw new NeoBuildInvalidAddressFormatException();
            }

        }

        public override void Write(Utf8JsonWriter writer, UInt160? value, JsonSerializerOptions options)
        {
            // TODO: Remove `ProtocolSettings.Default` and Create settings class.
            writer.WriteStringValue(value?.ToAddress(ProtocolSettings.Default.AddressVersion));
        }
    }
}
