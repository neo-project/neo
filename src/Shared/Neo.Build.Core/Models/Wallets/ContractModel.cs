// Copyright (C) 2015-2025 The Neo Project.
//
// ContractModel.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Interfaces;
using Neo.Build.Core.Json.Converters;
using Neo.SmartContract;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Neo.Build.Core.Models.Wallets
{
    public class ContractModel : JsonModel, IConvertToObject<Contract>
    {
        [JsonConverter(typeof(JsonStringHexFormatConverter))]
        public byte[]? Script { get; set; }

        public ICollection<ContractParameterModel>? Parameters { get; set; }

        public bool Deployed { get; set; }

        public Contract ToObject()
        {
            if (Parameters == null)
                return Contract.Create([], []);

            return Contract.Create([.. Parameters.Select(s => s.Type)], Script);
        }
    }
}
