// Copyright (C) 2015-2024 The Neo Project.
//
// PluginSettings.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using Org.BouncyCastle.Security;
using System;

namespace Neo.Plugins;

public abstract class PluginSettings(IConfigurationSection section)
{
    public UnhandledExceptionPolicy ExceptionPolicy
    {
        get
        {
            var policyString = section.GetValue("UnhandledExceptionPolicy", "StopNode");
            if (Enum.TryParse(policyString, out UnhandledExceptionPolicy policy))
            {
                return policy;
            }

            throw new InvalidParameterException($"{policyString} is not a valid UnhandledExceptionPolicy");
        }
    }
}
