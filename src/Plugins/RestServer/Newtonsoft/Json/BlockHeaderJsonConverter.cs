// Copyright (C) 2015-2024 The Neo Project.
//
// BlockHeaderJsonConverter.cs file belongs to the neo project and is free
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
    public class BlockHeaderJsonConverter : JsonConverter<Header>
    {
        public override bool CanRead => false;
        public override bool CanWrite => true;

        public override Header ReadJson(JsonReader reader, Type objectType, Header? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, Header? value, JsonSerializer serializer)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));

            var j = RestServerUtility.BlockHeaderToJToken(value, serializer);
            j.WriteTo(writer);
        }
    }
}
