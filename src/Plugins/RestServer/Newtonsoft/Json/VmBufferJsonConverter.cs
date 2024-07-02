// Copyright (C) 2015-2024 The Neo Project.
//
// VmBufferJsonConverter.cs file belongs to the neo project and is free
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
using Buffer = Neo.VM.Types.Buffer;

namespace Neo.Plugins.RestServer.Newtonsoft.Json
{
    public class VmBufferJsonConverter : JsonConverter<Buffer>
    {
        public override Buffer ReadJson(JsonReader reader, Type objectType, Buffer? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var t = JToken.ReadFrom(reader);
            if (RestServerUtility.StackItemFromJToken(t) is Buffer b) return b;

            throw new FormatException();
        }

        public override void WriteJson(JsonWriter writer, Buffer? value, JsonSerializer serializer)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));

            var t = RestServerUtility.StackItemToJToken(value, null, serializer);
            t.WriteTo(writer);
        }
    }
}
