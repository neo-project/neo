// Copyright (C) 2015-2024 The Neo Project.
//
// Settings.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using Neo.SmartContract.Native;

namespace Neo.Plugins
{
    internal class Settings
    {
        /// <summary>
        /// Amount of storages states (heights) to be dump in a given json file
        /// </summary>
        public uint BlockCacheSize { get; }
        /// <summary>
        /// Height to begin storage dump
        /// </summary>
        public uint HeightToBegin { get; }
        /// <summary>
        /// Default number of items per folder
        /// </summary>
        public uint StoragePerFolder { get; }
        public IReadOnlyList<int> Exclude { get; }

        public static Settings? Default { get; private set; }

        private Settings(IConfigurationSection section)
        {
            /// Geting settings for storage changes state dumper
            BlockCacheSize = section.GetValue("BlockCacheSize", 1000u);
            HeightToBegin = section.GetValue("HeightToBegin", 0u);
            StoragePerFolder = section.GetValue("StoragePerFolder", 100000u);
            Exclude = section.GetSection("Exclude").Exists()
                ? section.GetSection("Exclude").GetChildren().Select(p => int.Parse(p.Value)).ToArray()
                : new[] { NativeContract.Ledger.Id };
        }

        public static void Load(IConfigurationSection section)
        {
            Default = new Settings(section);
        }
    }
}
