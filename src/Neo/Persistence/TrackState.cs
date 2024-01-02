// Copyright (C) 2015-2024 The Neo Project.
//
// TrackState.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Persistence
{
    /// <summary>
    /// Represents the state of a cached entry.
    /// </summary>
    public enum TrackState : byte
    {
        /// <summary>
        /// Indicates that the entry has been loaded from the underlying storage, but has not been modified.
        /// </summary>
        None,

        /// <summary>
        /// Indicates that this is a newly added record.
        /// </summary>
        Added,

        /// <summary>
        /// Indicates that the entry has been loaded from the underlying storage, and has been modified.
        /// </summary>
        Changed,

        /// <summary>
        /// Indicates that the entry should be deleted from the underlying storage when committing.
        /// </summary>
        Deleted,

        /// <summary>
        /// Indicates that the entry was not found in the underlying storage.
        /// </summary>
        NotFound
    }
}
