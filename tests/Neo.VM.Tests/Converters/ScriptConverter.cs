// Copyright (C) 2015-2024 The Neo Project.
//
// ScriptConverter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Test.Extensions;
using Neo.VM;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace Neo.Test.Converters
{
    internal class ScriptConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(byte[]) || objectType == typeof(string);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.String:
                    {
                        if (reader.Value is string str)
                        {
                            Assert.IsTrue(str.StartsWith("0x"), $"'0x' prefix required for value: '{str}'");
                            return str.FromHexString();
                        }
                        break;
                    }
                case JsonToken.Bytes:
                    {
                        if (reader.Value is byte[] data) return data;
                        break;
                    }
                case JsonToken.StartArray:
                    {
                        using var script = new ScriptBuilder();

                        foreach (var entry in JArray.Load(reader))
                        {
                            var mul = 1;
                            var value = entry.Value<string>();

                            if (Enum.IsDefined(typeof(OpCode), value) && Enum.TryParse<OpCode>(value, out var opCode))
                            {
                                for (int x = 0; x < mul; x++)
                                {
                                    script.Emit(opCode);
                                }
                            }
                            else
                            {
                                for (int x = 0; x < mul; x++)
                                {
                                    Assert.IsTrue(value.StartsWith("0x"), $"'0x' prefix required for value: '{value}'");
                                    script.EmitRaw(value.FromHexString());
                                }
                            }
                        }

                        return script.ToArray();
                    }
            }

            throw new FormatException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is byte[] data)
            {
                int ip = 0;
                var array = new JArray();

                try
                {
                    for (ip = 0; ip < data.Length;)
                    {
                        var instruction = new Instruction(data, ip);

                        array.Add(instruction.OpCode.ToString().ToUpperInvariant());

                        // Operand Size

                        if (instruction.Size - 1 - instruction.Operand.Length > 0)
                        {
                            array.Add(data.Skip(ip + 1).Take(instruction.Size - 1 - instruction.Operand.Length).ToArray().ToHexString());
                        }

                        if (!instruction.Operand.IsEmpty)
                        {
                            // Data

                            array.Add(instruction.Operand.ToArray().ToHexString());
                        }

                        ip += instruction.Size;
                    }
                }
                catch
                {
                    // Something was wrong, but maybe it's intentioned

                    if (Enum.IsDefined(typeof(OpCode), data[ip]))
                    {
                        // Check if it was the content and not the opcode

                        array.Add(((OpCode)data[ip]).ToString().ToUpperInvariant());
                        array.Add(data[(ip + 1)..].ToHexString());
                    }
                    else
                    {
                        array.Add(data[ip..].ToHexString());
                    }
                }

                // Write the script

                writer.WriteStartArray();
                foreach (var entry in array) writer.WriteValue(entry.Value<string>());
                writer.WriteEndArray();

                // Double check - Ensure that the format is exactly the same

                using var script = new ScriptBuilder();

                foreach (var entry in array)
                {
                    if (Enum.TryParse<OpCode>(entry.Value<string>(), out var opCode))
                    {
                        script.Emit(opCode);
                    }
                    else
                    {
                        script.EmitRaw(entry.Value<string>().FromHexString());
                    }
                }

                if (script.ToArray().ToHexString() != data.ToHexString())
                {
                    throw new FormatException();
                }
            }
            else
            {
                throw new FormatException();
            }
        }
    }
}
