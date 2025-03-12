// Copyright (C) 2015-2025 The Neo Project.
//
// AppEngineOptionsModel.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Interfaces;
using Neo.Build.Core.SmartContract;

namespace Neo.Build.Core.Models.SmartContract
{
    public class AppEngineOptionsModel : JsonModel, IConvertToObject<ApplicationEngineSettings>
    {
        public long MaxGas { get; set; }

        public ApplicationEngineSettings ToObject() =>
            new()
            {
                MaxGas = MaxGas,
            };
    }
}
