// Copyright (C) 2015-2025 The Neo Project.
//
// ApplicationEngineDefaults.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.SmartContract;
using Neo.SmartContract.Native;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;

namespace Neo.Build.Core.SmartContract
{
    public static class ApplicationEngineDefaults
    {
        /// <summary>
        /// Max gas used for <see cref="ApplicationEngineSettings"/>.
        /// </summary>
        public static readonly long MaxGas = (long)BigInteger.Pow(20L, NativeContract.GAS.Decimals);

        /// <summary>
        /// Dictionary of <see cref="ApplicationEngineBase"/>'s overrides for system call methods of <see cref="ApplicationEngine"/>.
        /// </summary>
        public static IReadOnlyDictionary<uint, InteropDescriptor> SystemCallBaseServices => GetSystemCallBaseServices();

        public static ApplicationEngineSettings Settings { get; } = new();

        /// <summary>
        /// Appends a seed to the hash.
        /// </summary>
        /// <typeparam name="TEngine">Object with base type of <see cref="ApplicationEngine"/> where the method name can be found.</typeparam>
        /// <param name="methodName">Method name within the <typeparamref name="TEngine"/>.</param>
        /// <returns>Service description that can be used in <see cref="ApplicationEngine"/>.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static InteropDescriptor CreateSystemDescriptor<TEngine>(string methodName)
            where TEngine : ApplicationEngine
        {
            var methodInfo = typeof(TEngine).GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new InvalidOperationException($"{nameof(CreateSystemDescriptor)} failed to locate {methodName} method");
            var descriptor = new InteropDescriptor()
            {
                Name = methodName + Path.GetRandomFileName(), // Use we append a random name
                Handler = methodInfo,
                FixedPrice = 0L,
                RequiredCallFlags = CallFlags.All,
            };
            return descriptor;
        }

        /// <summary>
        /// Doesn't append a seed to the hash.
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <returns></returns>
        public static InteropDescriptor CreateSystemDescriptor(MethodInfo methodInfo) =>
            new()
            {
                Name = methodInfo.Name,
                Handler = methodInfo,
                FixedPrice = 0L,
                RequiredCallFlags = CallFlags.All,
            };

        private static ImmutableDictionary<uint, InteropDescriptor> GetSystemCallBaseServices()
        {
            var systemCallBaseServices = ImmutableDictionary.CreateBuilder<uint, InteropDescriptor>();

            var systemCallBaseMethods = typeof(ApplicationEngineBase).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(static w => w.Name.StartsWith("System"));
            var defaultSystemCallDescriptors = typeof(ApplicationEngine).GetFields()
                .Where(static w => w.Name.StartsWith("System_") && w.IsPublic && w.IsStatic)
                .Select(static s => KeyValuePair.Create(
                    s.Name.Replace("_", string.Empty),
                    (InteropDescriptor)s.GetValue(null)!
                )).ToImmutableDictionary();

            foreach (var systemCallMethodInfo in systemCallBaseMethods)
            {
                if (defaultSystemCallDescriptors.TryGetValue(systemCallMethodInfo.Name, out var defaultDescriptor) == false)
                    throw new KeyNotFoundException(nameof(systemCallMethodInfo.Name));
                else
                {
                    var descriptor = CreateSystemDescriptor(systemCallMethodInfo) with
                    {
                        FixedPrice = defaultDescriptor.FixedPrice,
                        RequiredCallFlags = defaultDescriptor.RequiredCallFlags,
                    };
                    systemCallBaseServices.Add(defaultDescriptor.Hash, descriptor);
                }
            }

            return systemCallBaseServices.ToImmutable();
        }
    }
}
