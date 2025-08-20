// Copyright (C) 2015-2025 The Neo Project.
//
// InteropInterfaceJsonConverter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Neo.Plugins.RestServer.Newtonsoft.Json
{
    public class InteropInterfaceJsonConverter : JsonConverter<InteropInterface>
    {
        public override InteropInterface ReadJson(JsonReader reader, Type objectType, InteropInterface? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var t = JToken.Load(reader);
            if (RestServerUtility.StackItemFromJToken(t) is InteropInterface iface) return iface;

            throw new FormatException();
        }

        public override void WriteJson(JsonWriter writer, InteropInterface? value, JsonSerializer serializer)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));

            var t = RestServerUtility.StackItemToJToken(value, null, serializer);
            t.WriteTo(writer);
        }
    }
}
