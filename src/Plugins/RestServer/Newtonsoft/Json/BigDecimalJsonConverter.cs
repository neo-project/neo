// Copyright (C) 2015-2024 The Neo Project.
//
// BigDecimalJsonConverter.cs file belongs to the neo project and is free
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
using System.Linq;
using System.Numerics;

namespace Neo.Plugins.RestServer.Newtonsoft.Json
{
    public class BigDecimalJsonConverter : JsonConverter<BigDecimal>
    {
        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override BigDecimal ReadJson(JsonReader reader, Type objectType, BigDecimal existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var token = JToken.ReadFrom(reader);

            switch (token.Type)
            {
                case JTokenType.Object:
                    {
                        var jobj = (JObject)token;
                        var valueProp = jobj.Properties().SingleOrDefault(p => p.Name.Equals("value", StringComparison.InvariantCultureIgnoreCase));
                        var decimalsProp = jobj.Properties().SingleOrDefault(p => p.Name.Equals("decimals", StringComparison.InvariantCultureIgnoreCase));

                        if (valueProp != null && decimalsProp != null)
                        {
                            return new BigDecimal(valueProp.ToObject<BigInteger>(), decimalsProp.ToObject<byte>());
                        }
                        break;
                    }
                case JTokenType.Float:
                    {
                        if (token is JValue jval && jval.Value is not null)
                        {
                            return new BigDecimal((decimal)jval.Value);
                        }
                        break;
                    }
            }

            throw new FormatException();
        }

        public override void WriteJson(JsonWriter writer, BigDecimal value, JsonSerializer serializer)
        {
            var o = JToken.FromObject(new
            {
                value.Value,
                value.Decimals,
            }, serializer);
            o.WriteTo(writer);
        }
    }
}
