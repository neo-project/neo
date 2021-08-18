// Copyright (C) 2015-2021 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Persistence;

namespace Neo.Plugins
{
    /// <summary>
    /// A provider used to create <see cref="IStore"/> instances.
    /// </summary>
    public interface IStorageProvider
    {
        /// <summary>
        /// Creates a new instance of the <see cref="IStore"/> interface.
        /// </summary>
        /// <param name="path">The path of the database.</param>
        /// <returns>The created <see cref="IStore"/> instance.</returns>
        IStore GetStore(string path);
    }
}
