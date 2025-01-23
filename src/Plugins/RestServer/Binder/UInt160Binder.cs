// Copyright (C) 2015-2025 The Neo Project.
//
// UInt160Binder.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Threading.Tasks;

namespace Neo.Plugins.RestServer.Binder
{
    internal class UInt160Binder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            _ = bindingContext ?? throw new ArgumentNullException(nameof(bindingContext));

            if (bindingContext.BindingSource == BindingSource.Path ||
                bindingContext.BindingSource == BindingSource.Query)
            {
                var modelName = bindingContext.ModelName;

                // Try to fetch the value of the argument by name
                var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);

                if (valueProviderResult == ValueProviderResult.None)
                    return Task.CompletedTask;

                bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);

                var value = valueProviderResult.FirstValue;

                // Check if the argument value is null or empty
                if (string.IsNullOrEmpty(value))
                    return Task.CompletedTask;

                var model = RestServerUtility.ConvertToScriptHash(value, RestServerPlugin.NeoSystem!.Settings);
                bindingContext.Result = ModelBindingResult.Success(model);
            }
            return Task.CompletedTask;
        }
    }
}
