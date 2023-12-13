// Copyright (C) 2015-2022 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Neo.Cryptography.ECC;
using Neo.Network.P2P.Payloads;

namespace Neo
{
    /// <summary>
    /// Represents the protocol settings of the NEO system.
    /// </summary>
    public record ProtocolSettings
    {
        /// <summary>
        /// The magic number of the NEO network.
        /// </summary>
        public uint Network { get; init; }

        /// <summary>
        /// The address version of the NEO system.
        /// </summary>
        public byte AddressVersion { get; init; }

        /// <summary>
        /// The public keys of the standby committee members.
        /// </summary>
        public IReadOnlyList<ECPoint> StandbyCommittee { get; init; }

        /// <summary>
        /// The number of members of the committee in NEO system.
        /// </summary>
        public int CommitteeMembersCount => StandbyCommittee.Count;

        /// <summary>
        /// The number of the validators in NEO system.
        /// </summary>
        public int ValidatorsCount { get; init; }

        /// <summary>
        /// The default seed nodes list.
        /// </summary>
        public string[] SeedList { get; init; }

        /// <summary>
        /// Indicates the time in milliseconds between two blocks.
        /// </summary>
        public uint MillisecondsPerBlock { get; init; }

        /// <summary>
        /// Indicates the time between two blocks.
        /// </summary>
        public TimeSpan TimePerBlock => TimeSpan.FromMilliseconds(MillisecondsPerBlock);

        /// <summary>
        /// The maximum increment of the <see cref="Transaction.ValidUntilBlock"/> field.
        /// </summary>
        public uint MaxValidUntilBlockIncrement => 86400000 / MillisecondsPerBlock;

        /// <summary>
        /// Indicates the maximum number of transactions that can be contained in a block.
        /// </summary>
        public uint MaxTransactionsPerBlock { get; init; }

        /// <summary>
        /// Indicates the maximum number of transactions that can be contained in the memory pool.
        /// </summary>
        public int MemoryPoolMaxTransactions { get; init; }

        /// <summary>
        /// Indicates the maximum number of blocks that can be traced in the smart contract.
        /// </summary>
        public uint MaxTraceableBlocks { get; init; }

        /// <summary>
        /// Sets the block height from which a hardfork is activated.
        /// </summary>
        public ImmutableDictionary<Hardfork, uint> Hardforks { get; init; }

        /// <summary>
        /// Indicates the amount of gas to distribute during initialization.
        /// </summary>
        public ulong InitialGasDistribution { get; init; }

        private IReadOnlyList<ECPoint> _standbyValidators;
        /// <summary>
        /// The public keys of the standby validators.
        /// </summary>
        public IReadOnlyList<ECPoint> StandbyValidators => _standbyValidators ??= StandbyCommittee.Take(ValidatorsCount).ToArray();

        /// <summary>
        /// The default protocol settings for NEO MainNet.
        /// </summary>
        public static ProtocolSettings Default { get; } = new ProtocolSettings
        {
            Network = 0u,
            AddressVersion = 0x35,
            StandbyCommittee = Array.Empty<ECPoint>(),
            ValidatorsCount = 0,
            SeedList = Array.Empty<string>(),
            MillisecondsPerBlock = 15000,
            MaxTransactionsPerBlock = 512,
            MemoryPoolMaxTransactions = 50_000,
            MaxTraceableBlocks = 2_102_400,
            InitialGasDistribution = 52_000_000_00000000,
            Hardforks = ImmutableDictionary<Hardfork, uint>.Empty
        };

        /// <summary>
        /// Loads the <see cref="ProtocolSettings"/> at the specified path.
        /// </summary>
        /// <param name="path">The path of the settings file.</param>
        /// <param name="optional">Indicates whether the file is optional.</param>
        /// <returns>The loaded <see cref="ProtocolSettings"/>.</returns>
        public static ProtocolSettings Load(string path, bool optional = true)
        {
            IConfigurationRoot config = new ConfigurationBuilder().AddJsonFile(path, optional).Build();
            IConfigurationSection section = config.GetSection("ProtocolConfiguration");
            var settings = Load(section);
            CheckingHardfork(settings);
            return settings;
        }

        /// <summary>
        /// Loads the <see cref="ProtocolSettings"/> with the specified <see cref="IConfigurationSection"/>.
        /// </summary>
        /// <param name="section">The <see cref="IConfigurationSection"/> to be loaded.</param>
        /// <returns>The loaded <see cref="ProtocolSettings"/>.</returns>
        public static ProtocolSettings Load(IConfigurationSection section)
        {
            return new ProtocolSettings
            {
                Network = section.GetValue("Network", Default.Network),
                AddressVersion = section.GetValue("AddressVersion", Default.AddressVersion),
                StandbyCommittee = section.GetSection("StandbyCommittee").Exists()
                    ? section.GetSection("StandbyCommittee").GetChildren().Select(p => ECPoint.Parse(p.Get<string>(), ECCurve.Secp256r1)).ToArray()
                    : Default.StandbyCommittee,
                ValidatorsCount = section.GetValue("ValidatorsCount", Default.ValidatorsCount),
                SeedList = section.GetSection("SeedList").Exists()
                    ? section.GetSection("SeedList").GetChildren().Select(p => p.Get<string>()).ToArray()
                    : Default.SeedList,
                MillisecondsPerBlock = section.GetValue("MillisecondsPerBlock", Default.MillisecondsPerBlock),
                MaxTransactionsPerBlock = section.GetValue("MaxTransactionsPerBlock", Default.MaxTransactionsPerBlock),
                MemoryPoolMaxTransactions = section.GetValue("MemoryPoolMaxTransactions", Default.MemoryPoolMaxTransactions),
                MaxTraceableBlocks = section.GetValue("MaxTraceableBlocks", Default.MaxTraceableBlocks),
                InitialGasDistribution = section.GetValue("InitialGasDistribution", Default.InitialGasDistribution),
                Hardforks = section.GetSection("Hardforks").Exists()
                    ? section.GetSection("Hardforks").GetChildren().ToImmutableDictionary(p => Enum.Parse<Hardfork>(p.Key), p => uint.Parse(p.Value))
                    : Default.Hardforks
            };
        }

        private static void CheckingHardfork(ProtocolSettings settings)
        {
            var allHardforks = Enum.GetValues(typeof(Hardfork)).Cast<Hardfork>().ToList();
            // Check for continuity in configured hardforks
            var sortedHardforks = settings.Hardforks.Keys
                .OrderBy(h => allHardforks.IndexOf(h))
                .ToList();

            for (int i = 0; i < sortedHardforks.Count - 1; i++)
            {
                int currentIndex = allHardforks.IndexOf(sortedHardforks[i]);
                int nextIndex = allHardforks.IndexOf(sortedHardforks[i + 1]);

                // If they aren't consecutive, return false.
                if (nextIndex - currentIndex > 1)
                    throw new Exception("Hardfork configuration is not continuous.");
            }
            // Check that block numbers are not higher in earlier hardforks than in later ones
            for (int i = 0; i < sortedHardforks.Count - 1; i++)
            {
                if (settings.Hardforks[sortedHardforks[i]] > settings.Hardforks[sortedHardforks[i + 1]])
                {
                    // This means the block number for the current hardfork is greater than the next one, which should not be allowed.
                    throw new Exception($"The Hardfork configuration for {sortedHardforks[i]} is greater than for {sortedHardforks[i + 1]}");
                }
            }
        }
    }
}
