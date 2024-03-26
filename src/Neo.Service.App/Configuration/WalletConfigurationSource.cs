// Copyright (C) 2015-2024 The Neo Project.
//
// WalletConfigurationSource.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using Neo.Service.App.Providers;
using System.Diagnostics.CodeAnalysis;
using System.Security;

namespace Neo.Service.App.Configuration
{
    internal class WalletConfigurationSource : IConfigurationSource
    {
        public ProtocolSettings ProtocolSettings { get; set; } = ProtocolSettings.Default;

        public string? Name
        {
            get; [param: DisallowNull]
            set;
        }

        public string? Path
        {
            get; [param: DisallowNull]
            set;
        }

        public SecureString? Password
        {
            get; [param: DisallowNull]
            set;
        }


        #region IConfigurationSource

        IConfigurationProvider IConfigurationSource.Build(IConfigurationBuilder builder)
        {
            return new WalletConfigurationProvider(this);
        }

        #endregion
    }
}
