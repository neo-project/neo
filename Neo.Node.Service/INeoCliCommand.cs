// Copyright (C) 2015-2024 The Neo Project.
//
// INeoCliCommand.cs file belongs to the neo project and is free
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
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Node.Service
{
    internal enum CommandType : byte
    {
        None = 0x00,
        Exit = 0xee,
    }

    internal interface INeoCliCommand
    {
        CommandType Command { get; }
        string[] Args { get; }
        Task<object?> ExecuteAsync(CancellationToken cancellationToken);
    }

    internal class PipeCommand : INeoCliCommand
    {
        private static readonly ConcurrentDictionary<CommandType, Func<string[], CancellationToken, object?>> s_methods = new();

        public CommandType Command { get; set; } = CommandType.None;
        public string[] Args { get; set; } = Array.Empty<string>();

        public static void RegisterMethods(object handler)
        {
            var handlerType = handler.GetType();
            var methods = handlerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var method in methods)
            {
                var pipeAttr = method.GetCustomAttribute<PipeMethodAttribute>();
                if (pipeAttr == null) continue;
                if (s_methods.ContainsKey(pipeAttr.Command) && pipeAttr.Overwrite == false)
                    throw new MethodAccessException($"{handlerType.FullName}::{method.Name}: Pipe command {pipeAttr.Command} already exists.");
                s_methods[pipeAttr.Command] = method.CreateDelegate<Func<string[], CancellationToken, object?>>(handler);
            }
        }

        public static bool Contains(CommandType command) =>
            s_methods.ContainsKey(command);

        public async Task<object?> ExecuteAsync(CancellationToken cancellationToken)
        {
            if (s_methods.TryGetValue(Command, out var commandFunc) == false)
                throw new MissingMethodException($"{Command}");

            var methodObj = commandFunc(Args, cancellationToken);

            if (methodObj is Task<object?> awaitMethodTask)
                methodObj = await awaitMethodTask;

            return methodObj;
        }
    }
}
