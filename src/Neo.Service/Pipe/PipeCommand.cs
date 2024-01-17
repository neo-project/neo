// Copyright (C) 2015-2024 The Neo Project.
//
// PipeCommand.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Service.Pipes
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    internal enum CommandType : byte
    {
        Start = 0x10,
        Stop = 0x11,
        Exit = 0xee,
    }

    internal sealed class PipeCommand
    {
        private static readonly ConcurrentDictionary<CommandType, Func<string[], CancellationToken, object>> s_methods = new();

        [JsonInclude]
        public required CommandType Exec { get; set; }

        [JsonInclude]
        public required string[] Arguments { get; set; }

        public static void RegisterMethods(object handler)
        {
            var handlerType = handler.GetType();
            var methods = handlerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var method in methods)
            {
                var pipeAttr = method.GetCustomAttribute<PipeMethodAttribute>();
                if (pipeAttr == null) continue;
                if (s_methods.ContainsKey(pipeAttr.Command) && pipeAttr.Overwrite == false)
                    throw new AmbiguousMatchException($"{handlerType.FullName}.{method.Name}: Command {pipeAttr.Command} already exists.");
                s_methods[pipeAttr.Command] = method.CreateDelegate<Func<string[], CancellationToken, object>>(handler);
            }
        }

        public static bool Contains(CommandType command) =>
            s_methods.ContainsKey(command);

        public async Task<object> ExecuteAsync(CancellationToken cancellationToken)
        {
            if (s_methods.TryGetValue(Exec, out var commandFunc) == false)
                throw new MissingMethodException($"{Exec}");

            var methodObj = commandFunc(Arguments, cancellationToken);

            if (methodObj is Task<object> awaitMethodTask)
                methodObj = await awaitMethodTask;

            if (methodObj is null)
                throw new NullReferenceException($"{Exec}");

            return methodObj;
        }
    }
}
