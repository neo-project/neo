// Copyright (C) 2015-2025 The Neo Project.
//
// ContractGroupJsonConverter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.SmartContract.Manifest;
using Newtonsoft.Json;
using System;

namespace Neo.Plugins.RestServer.Newtonsoft.Json
{
    public class ContractGroupJsonConverter : JsonConverter<ContractGroup>
    {
        public override bool CanRead => false;

        public override bool CanWrite => true;

        public override ContractGroup ReadJson(JsonReader reader, Type objectType, ContractGroup? existingValue, bool hasExistingValue, JsonSerializer serializer) => throw new NotImplementedException();
        public override void WriteJson(JsonWriter writer, ContractGroup? value, JsonSerializer serializer)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));

            var j = RestServerUtility.ContractGroupToJToken(value, serializer);
            j.WriteTo(writer);
        }
    }
}
