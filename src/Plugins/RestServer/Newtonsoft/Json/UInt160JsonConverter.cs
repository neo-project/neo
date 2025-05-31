// Copyright (C) 2015-2025 The Neo Project.
//
// UInt160JsonConverter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Plugins.RestServer.Exceptions;
using Newtonsoft.Json;
using System;

namespace Neo.Plugins.RestServer.Newtonsoft.Json
{
    public class UInt160JsonConverter : JsonConverter<UInt160>
    {
        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override UInt160 ReadJson(JsonReader reader, Type objectType, UInt160? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var value = reader.Value?.ToString();
            if (value is null) throw new ArgumentNullException(nameof(value));

            try
            {
                return RestServerUtility.ConvertToScriptHash(value, RestServerPlugin.NeoSystem!.Settings);
            }
            catch (FormatException)
            {
                throw new ScriptHashFormatException($"'{value}' is invalid.");
            }
        }

        public override void WriteJson(JsonWriter writer, UInt160? value, JsonSerializer serializer)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));

            writer.WriteValue(value.ToString());
        }
    }
}
