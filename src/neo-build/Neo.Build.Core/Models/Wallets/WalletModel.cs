// Copyright (C) 2015-2025 The Neo Project.
//
// WalletModel.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Interfaces;
using Neo.Build.Core.Wallets;
using System;
using System.Text.Json.Serialization;

namespace Neo.Build.Core.Models.Wallets
{
    public class WalletModel : JsonModel, IConvertToObject<DevWallet>
    {
        public string? Name { get; set; }

        public Version? Version { get; set; }

        [JsonPropertyName("scrypt")]
        public SCryptModel? SCrypt { get; set; }

        public WalletAccountModel[]? Accounts { get; set; }

        public ProtocolOptionsModel? Extra { get; set; }

        /// <summary>
        /// Converts to <see cref="DevWallet"/>.
        /// <code>
        /// Note: If 'Extra' property is <see langword="null"/> than <see cref="ProtocolSettings.Default"/> is used.
        /// </code>
        /// </summary>
        /// <returns><see cref="DevWallet"/></returns>
        public DevWallet ToObject() =>
            new(this);
    }
}
