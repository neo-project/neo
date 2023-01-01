// Copyright (C) 2015-2023 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.SmartContract
{
    /// <summary>
    /// Specify the options to be used during the search.
    /// </summary>
    [Flags]
    public enum FindOptions : byte
    {
        /// <summary>
        /// No option is set. The results will be an iterator of (key, value).
        /// </summary>
        None = 0,

        /// <summary>
        /// Indicates that only keys need to be returned. The results will be an iterator of keys.
        /// </summary>
        KeysOnly = 1 << 0,

        /// <summary>
        /// Indicates that the prefix byte of keys should be removed before return.
        /// </summary>
        RemovePrefix = 1 << 1,

        /// <summary>
        /// Indicates that only values need to be returned. The results will be an iterator of values.
        /// </summary>
        ValuesOnly = 1 << 2,

        /// <summary>
        /// Indicates that values should be deserialized before return.
        /// </summary>
        DeserializeValues = 1 << 3,

        /// <summary>
        /// Indicates that only the field 0 of the deserialized values need to be returned. This flag must be set together with <see cref="DeserializeValues"/>.
        /// </summary>
        PickField0 = 1 << 4,

        /// <summary>
        /// Indicates that only the field 1 of the deserialized values need to be returned. This flag must be set together with <see cref="DeserializeValues"/>.
        /// </summary>
        PickField1 = 1 << 5,

        /// <summary>
        /// This value is only for internal use, and shouldn't be used in smart contracts.
        /// </summary>
        All = KeysOnly | RemovePrefix | ValuesOnly | DeserializeValues | PickField0 | PickField1
    }
}
