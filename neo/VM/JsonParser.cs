using Neo.IO.Json;
using System;
using System.Numerics;
using System.Text;

namespace Neo.VM
{
    public static class JsonParser
    {
        /// <summary>
        /// Convert json object to stack item
        /// </summary>
        /// <param name="json">Json</param>
        /// <returns>Return stack item</returns>
        public static StackItem JsonToStackItem(JObject json)
        {
            switch (json)
            {
                case JArray array:
                    {
                        var item = new Types.Array();

                        foreach (var entry in array)
                        {
                            item.Add(JsonToStackItem(entry));
                        }

                        return item;
                    }
                case JString str:
                    {
                        return new Types.ByteArray(Encoding.UTF8.GetBytes(str.Value));
                    }
                case JNumber num:
                    {
                        return new Types.Integer((BigInteger)num.Value);
                    }
                case JBoolean boolean:
                    {
                        return new Types.Boolean(boolean.Value);
                    }
                case JObject obj:
                    {
                        var item = new Types.Map();

                        foreach (var entry in obj.Properties)
                        {
                            var key = new Types.ByteArray(Encoding.UTF8.GetBytes(entry.Key));
                            var value = JsonToStackItem(entry.Value);

                            item.Add(key, value);
                        }

                        return item;
                    }
                default: throw new FormatException();
            }
        }
    }
}