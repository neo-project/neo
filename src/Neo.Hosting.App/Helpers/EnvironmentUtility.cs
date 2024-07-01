// Copyright (C) 2015-2024 The Neo Project.
//
// EnvironmentUtility.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Hosting.App.Host;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace Neo.Hosting.App.Helpers
{
    internal static class EnvironmentUtility
    {
        private static readonly char[] s_invalidPipeNameChars = Path.GetInvalidFileNameChars();

        public static bool TryGetServicePipeName([NotNullWhen(true)] out string? pipeName)
        {
            pipeName = Environment.GetEnvironmentVariable(NeoEnvironmentVariableDefaults.PipeName, EnvironmentVariableTarget.User)
                ?? Environment.GetEnvironmentVariable(NeoEnvironmentVariableDefaults.PipeName, EnvironmentVariableTarget.Process);

            if ((string.IsNullOrEmpty(pipeName) &&
                string.IsNullOrWhiteSpace(pipeName)) ||
                pipeName.Any(static a => s_invalidPipeNameChars.Contains(a)))
            {
                pipeName = null; // Clear invalid any pipe name
                return false;
            }

            return true;
        }

        public static bool TrySetServicePipeName(string pipeName)
        {
            if (string.IsNullOrEmpty(pipeName) &&
                string.IsNullOrWhiteSpace(pipeName))
                return false;

            if (pipeName.Any(static a => s_invalidPipeNameChars.Contains(a)) == false)
                return false;

            Environment.SetEnvironmentVariable(NeoEnvironmentVariableDefaults.PipeName, pipeName, EnvironmentVariableTarget.User);

            return true;
        }

        public static void DeleteServicePipeName() =>
            Environment.SetEnvironmentVariable(NeoEnvironmentVariableDefaults.PipeName, null, EnvironmentVariableTarget.User);

        public static bool TryGetHostingEnvironment([MaybeNullWhen(true)] out string? environment)
        {
            environment = Environment.GetEnvironmentVariable(NeoEnvironmentVariableDefaults.Environment, EnvironmentVariableTarget.User)
                ?? Environment.GetEnvironmentVariable(NeoEnvironmentVariableDefaults.Environment, EnvironmentVariableTarget.Process);

            if (string.IsNullOrEmpty(environment) &&
                string.IsNullOrWhiteSpace(environment))
            {
                environment = null;
                return false;
            }

            return true;
        }

        //public static NamedPipeEndPoint AddOrGetServicePipeName()
        //{
        //    if (TryGetServicePipeName(out var pipeName))
        //        return new(pipeName);
        //    else
        //    {
        //        var endPoint = new NamedPipeEndPoint(Path.GetRandomFileName());

        //        Environment.SetEnvironmentVariable(NeoEnvironmentVariableDefaults.PipeName, endPoint.PipeName, EnvironmentVariableTarget.User);

        //        return endPoint;
        //    }
        //}
    }
}
