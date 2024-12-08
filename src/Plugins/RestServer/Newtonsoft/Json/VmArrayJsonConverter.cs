// Copyright (C) 2015-2024 The Neo Project.
//
// VmArrayJsonConverter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using Array = Neo.VM.Types.Array;

namespace Neo.Plugins.RestServer.Newtonsoft.Json
{
    public class VmArrayJsonConverter : JsonConverter<Array>
    {
        public override Array ReadJson(JsonReader reader, Type objectType, Array? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var t = JToken.Load(reader);
            if (RestServerUtility.StackItemFromJToken(t) is Array a) return a;

            throw new FormatException();
        }

        public override void WriteJson(JsonWriter writer, Array? value, JsonSerializer serializer)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));

            var t = RestServerUtility.StackItemToJToken(value, null, serializer);
            t.WriteTo(writer);
        }
    }
}
