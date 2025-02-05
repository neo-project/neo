// Copyright (C) 2015-2025 The Neo Project.
//
// PluginSettings.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using Org.BouncyCastle.Security;
using System;

namespace Neo.Plugins
{
    public abstract class PluginSettings(IConfigurationSection section)
    {
        public UnhandledExceptionPolicy ExceptionPolicy
        {
            get
            {
                var policyString = section.GetValue(nameof(UnhandledExceptionPolicy), nameof(UnhandledExceptionPolicy.StopNode));
                if (Enum.TryParse(policyString, true, out UnhandledExceptionPolicy policy))
                {
                    return policy;
                }

                throw new InvalidParameterException($"{policyString} is not a valid UnhandledExceptionPolicy");
            }
        }
    }
}
