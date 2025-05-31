// Copyright (C) 2015-2025 The Neo Project.
//
// CheckpointStorageSettingsModel.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Collections.Generic;

namespace Neo.Build.Core.Models.Storage
{
    public class CheckpointStorageSettingsModel : JsonModel
    {
        public ICollection<CheckpointSettingsModel> Checkpoints { get; set; } = [];
    }
}
