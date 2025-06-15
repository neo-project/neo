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

using Neo.Build.Core.Exceptions;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;

namespace Neo.Build.Core.Models
{
    public abstract class JsonModel
    {
        protected JsonSerializerOptions _jsonSerializerOptions = NeoBuildDefaults.JsonDefaultSerializerOptions;

        [return: NotNull]
        public virtual string? ToString(JsonSerializerOptions? options = default) =>
            ToJson(options);

        public virtual string ToJson(JsonSerializerOptions? options) =>
            JsonSerializer.Serialize<object>(this, options ?? _jsonSerializerOptions);

        public static TModel? FromJson<TModel>(string jsonString, JsonSerializerOptions? options = default)
            where TModel : notnull, JsonModel =>
            JsonSerializer.Deserialize<TModel>(
                jsonString,
                options ?? NeoBuildDefaults.JsonDefaultSerializerOptions);

        public static TModel? FromJson<TModel>(FileInfo file, JsonSerializerOptions? options = default)
            where TModel : notnull, JsonModel
        {
            if (file.Exists == false)
                throw new NeoBuildFileNotFoundException(file);

            var jsonOptions = options ?? NeoBuildDefaults.JsonDefaultSerializerOptions;
            var jsonString = File.ReadAllText(file.FullName);

            if (string.IsNullOrEmpty(jsonString))
                throw new NeoBuildInvalidFileFormatException(file);

            return FromJson<TModel>(jsonString, jsonOptions);
        }
    }
}
