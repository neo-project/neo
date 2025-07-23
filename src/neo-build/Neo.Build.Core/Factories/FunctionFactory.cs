// Copyright (C) 2015-2025 The Neo Project.
//
// FunctionFactory.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Persistence;
using Neo.SmartContract.Native;
using System.IO;
using System.Threading;

namespace Neo.Build.Core.Factories
{
    public static class FunctionFactory
    {
        private static readonly uint s_networkSeed = 810960196u; // DEV0 Magic Code
        private static readonly string s_globalString = "Global\\";

        public static uint GetDevNetwork(uint index) =>
            (uint)(s_networkSeed & ~(0xf << 24) | (index << 24));

        public static string GetContractName(DataCache snapshot, UInt160 scriptHash)
        {
            var contractState = NativeContract.ContractManagement.GetContract(snapshot, scriptHash);
            if (contractState is not null)
                return contractState.Manifest.Name;
            return scriptHash.ToString();
        }

        public static FileInfo ResolveFileName(string filename, string rootPath)
        {
            if (Path.IsPathRooted(filename))
                return new(filename);
            if (Path.IsPathRooted(rootPath) == false)
                rootPath = Path.GetFullPath(rootPath);
            return new(Path.Combine(rootPath, filename));
        }

        public static Mutex CreateMutex(string? name = null)
        {
            if (string.IsNullOrEmpty(name))
                name = Path.Combine(s_globalString, Path.GetRandomFileName());
            return new(initiallyOwned: true, name);
        }
    }
}
