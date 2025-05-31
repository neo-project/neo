// Copyright (C) 2015-2025 The Neo Project.
//
// TransactionAttributeJsonConverter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Network.P2P.Payloads;
using Newtonsoft.Json;
using System;

namespace Neo.Plugins.RestServer.Newtonsoft.Json
{
    public class TransactionAttributeJsonConverter : JsonConverter<TransactionAttribute>
    {
        public override bool CanRead => false;

        public override bool CanWrite => true;

        public override TransactionAttribute ReadJson(JsonReader reader, Type objectType, TransactionAttribute? existingValue, bool hasExistingValue, JsonSerializer serializer) => throw new NotImplementedException();
        public override void WriteJson(JsonWriter writer, TransactionAttribute? value, JsonSerializer serializer)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));

            var j = RestServerUtility.TransactionAttributeToJToken(value, serializer);
            j.WriteTo(writer);
        }
    }
}
