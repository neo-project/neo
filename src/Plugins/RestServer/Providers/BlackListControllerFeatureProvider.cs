// Copyright (C) 2015-2024 The Neo Project.
//
// BlackListControllerFeatureProvider.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using System;
using System.Linq;
using System.Reflection;

namespace Neo.Plugins.RestServer.Providers
{
    internal class BlackListControllerFeatureProvider : ControllerFeatureProvider
    {
        private readonly RestServerSettings _settings;

        public BlackListControllerFeatureProvider()
        {
            _settings = RestServerSettings.Current;
        }

        protected override bool IsController(TypeInfo typeInfo)
        {
            if (typeInfo.IsDefined(typeof(ApiControllerAttribute)) == false) // Rest API
                return false;
            if (_settings.DisableControllers.Any(a => a.Equals(typeInfo.Name, StringComparison.OrdinalIgnoreCase))) // BlackList
                return false;
            return base.IsController(typeInfo); // Default check
        }
    }
}
