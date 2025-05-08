// Copyright (C) 2015-2025 The Neo Project.
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
using System;

namespace Neo.Plugins.LedgerDebugger
{
    /// <summary>
    /// Configuration settings for the LedgerDebugger plugin.
    /// </summary>
    internal class Settings
    {
        #region Constants

        /// <summary>
        /// Default storage path pattern. {0} will be replaced with network identifier.
        /// </summary>
        private const string DefaultPath = "ReadSets_{0}";

        /// <summary>
        /// Default storage provider.
        /// </summary>
        private const string DefaultStoreProvider = "LevelDBStore";

        /// <summary>
        /// Default maximum number of read sets to keep.
        /// </summary>
        private const int DefaultMaxReadSetsToKeep = 10000;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the storage path for block read sets.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the storage provider for block read sets.
        /// </summary>
        public string StoreProvider { get; }

        /// <summary>
        /// Gets the maximum number of read sets to keep before removing older ones.
        /// </summary>
        public int MaxReadSetsToKeep { get; }

        /// <summary>
        /// Gets the singleton instance of settings.
        /// </summary>
        public static Settings? Default { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Settings"/> class.
        /// </summary>
        /// <param name="section">The configuration section to load settings from.</param>
        /// <exception cref="ArgumentNullException">Thrown if section is null.</exception>
        private Settings(IConfigurationSection section)
        {
            if (section == null)
                throw new ArgumentNullException(nameof(section));

            // Load settings with defaults
            Path = section.GetValue("Path", DefaultPath);
            StoreProvider = section.GetValue("StoreProvider", DefaultStoreProvider);
            MaxReadSetsToKeep = section.GetValue("MaxReadSetsToKeep", DefaultMaxReadSetsToKeep);

            // Validate settings
            if (string.IsNullOrEmpty(Path))
                throw new InvalidOperationException("Path cannot be null or empty");

            if (string.IsNullOrEmpty(StoreProvider))
                throw new InvalidOperationException("StoreProvider cannot be null or empty");

            if (MaxReadSetsToKeep < 0)
                throw new InvalidOperationException("MaxReadSetsToKeep cannot be negative");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Loads settings from configuration section.
        /// </summary>
        /// <param name="section">The configuration section.</param>
        /// <exception cref="ArgumentNullException">Thrown if section is null.</exception>
        public static void Load(IConfigurationSection section)
        {
            if (section == null)
                throw new ArgumentNullException(nameof(section));

            Default = new Settings(section);
        }

        #endregion
    }
}
