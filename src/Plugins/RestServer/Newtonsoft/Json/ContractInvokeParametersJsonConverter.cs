// Copyright (C) 2015-2025 The Neo Project.
//
// ContractInvokeParametersJsonConverter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Plugins.RestServer.Models.Contract;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Neo.Plugins.RestServer.Newtonsoft.Json
{
    public class ContractInvokeParametersJsonConverter : JsonConverter<InvokeParams>
    {
        public override bool CanRead => true;
        public override bool CanWrite => false;

        public override InvokeParams ReadJson(JsonReader reader, Type objectType, InvokeParams? existingValue, bool hasExistingValue, global::Newtonsoft.Json.JsonSerializer serializer)
        {
            var token = JToken.ReadFrom(reader);
            return RestServerUtility.ContractInvokeParametersFromJToken(token);
        }

        public override void WriteJson(JsonWriter writer, InvokeParams? value, global::Newtonsoft.Json.JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
