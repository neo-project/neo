// Copyright (C) 2015-2025 The Neo Project.
//
// JsonModel.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Exceptions;
using System.IO;
using System.Text.Json;

namespace Neo.Build.Models
{
    internal abstract class JsonModel
    {
        protected JsonSerializerOptions _jsonSerializerOptions = NeoBuildDefaults.JsonDefaultSerializerOptions;

        public abstract override string? ToString();

        public abstract string ToJson();

        public static T? FromJson<T>(string jsonString, JsonSerializerOptions? options = default)
            where T : notnull, JsonModel =>
            JsonSerializer.Deserialize<T>(
                jsonString,
                options ?? NeoBuildDefaults.JsonDefaultSerializerOptions);

        public static T? FromJson<T>(FileInfo file, JsonSerializerOptions? options = default)
            where T : notnull, JsonModel
        {
            if (file.Exists == false)
                throw new NeoBuildFileNotFoundException(file);

            var jsonOptions = options ?? NeoBuildDefaults.JsonDefaultSerializerOptions;
            var jsonString = File.ReadAllText(file.FullName);

            if (string.IsNullOrEmpty(jsonString))
                throw new NeoBuildFileNotFoundException(file);

            return FromJson<T>(jsonString, jsonOptions);
        }
    }
}
