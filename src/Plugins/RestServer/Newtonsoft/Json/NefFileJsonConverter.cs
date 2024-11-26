// Copyright (C) 2015-2024 The Neo Project.
//
// NefFileJsonConverter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.SmartContract;
using Newtonsoft.Json;
using System;

namespace Neo.Plugins.RestServer.Newtonsoft.Json
{
    public class NefFileJsonConverter : JsonConverter<NefFile>
    {
        public override NefFile ReadJson(JsonReader reader, Type objectType, NefFile? existingValue, bool hasExistingValue, global::Newtonsoft.Json.JsonSerializer serializer) => throw new NotImplementedException();
        public override void WriteJson(JsonWriter writer, NefFile? value, global::Newtonsoft.Json.JsonSerializer serializer)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));

            var j = RestServerUtility.ContractNefFileToJToken(value, serializer);
            j.WriteTo(writer);
        }
    }
}
