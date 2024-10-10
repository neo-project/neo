// Copyright (C) 2015-2024 The Neo Project.
//
// ContractMethodParametersJsonConverter.cs file belongs to the neo project and is free
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

namespace Neo.Plugins.RestServer.Newtonsoft.Json;
public class ContractMethodParametersJsonConverter : JsonConverter<ContractParameterDefinition>
{
    public override bool CanRead => base.CanRead;

    public override bool CanWrite => base.CanWrite;

    public override ContractParameterDefinition ReadJson(JsonReader reader, Type objectType, ContractParameterDefinition? existingValue, bool hasExistingValue, JsonSerializer serializer) => throw new NotImplementedException();
    public override void WriteJson(JsonWriter writer, ContractParameterDefinition? value, JsonSerializer serializer)
    {
        if (value is null) throw new ArgumentNullException(nameof(value));

        var j = RestServerUtility.ContractMethodParameterToJToken(value, serializer);
        j.WriteTo(writer);
    }
}
