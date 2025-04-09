// Copyright (C) 2015-2025 The Neo Project.
//
// Helper.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.SmartContract.Manifest;
using Neo.Wallets;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Neo.CLI
{
    internal static class Helper
    {
        public static bool IsYes(this string input)
        {
            if (input == null) return false;

            input = input.ToLowerInvariant();

            return input == "yes" || input == "y";
        }

        public static string ToBase64String(this byte[] input) => Convert.ToBase64String(input);

        public static void IsScriptValid(this ReadOnlyMemory<byte> script, ContractAbi abi)
        {
            try
            {
                SmartContract.Helper.Check(script.ToArray(), abi);
            }
            catch (Exception e)
            {
                throw new FormatException($"Bad Script or Manifest Format: {e.Message}");
            }
        }

        /// <summary>
        /// Gets the sinks from a Serilog logger.
        /// </summary>
        /// <param name="logger">The Serilog logger instance</param>
        /// <returns>A collection of sink objects</returns>
        public static IEnumerable<object> GetSinks(this ILogger logger)
        {
            try
            {
                // Access the internal pipeline property using reflection
                var loggerConfig = GetPrivateMember<object>(logger, "core");
                var pipeline = GetPrivateMember<object>(loggerConfig, "pipeline");
                var sinks = GetPrivateMember<IEnumerable<object>>(pipeline, "Sinks");
                return sinks;
            }
            catch
            {
                return Array.Empty<object>(); // Empty collection if we can't get it
            }
        }

        private static T GetPrivateMember<T>(object instance, string memberName)
        {
            var type = instance.GetType();
            MemberInfo? member = type.GetField(memberName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (member == null)
            {
                member = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.NonPublic);
            }

            if (member == null)
                throw new InvalidOperationException($"Member {memberName} not found on type {type.FullName}");

            return member switch
            {
                FieldInfo field => (T)field.GetValue(instance)!,
                PropertyInfo property => (T)property.GetValue(instance)!,
                _ => throw new InvalidOperationException($"Member {memberName} is not a field or property on type {type.FullName}")
            };
        }
    }
}
