// Copyright (C) 2015-2024 The Neo Project.
//
// ISnapshot.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Json;
using System;
using System.Linq;

namespace Neo.Persistence
{
    /// <summary>
    /// This interface provides methods for reading, writing, and committing from/to snapshot.
    /// </summary>
    public interface ISnapshot : IDisposable, IReadOnlyStore
    {
        /// <summary>
        /// Commits all changes in the snapshot to the database.
        /// </summary>
        void Commit();

        /// <summary>
        /// Deletes an entry from the snapshot.
        /// </summary>
        /// <param name="key">The key of the entry.</param>
        void Delete(byte[] key);

        /// <summary>
        /// Puts an entry to the snapshot.
        /// </summary>
        /// <param name="key">The key of the entry.</param>
        /// <param name="value">The data of the entry.</param>
        void Put(byte[] key, byte[] value);

        /// <summary>
        /// Load data from json
        ///
        /// Expected data (in base64):
        /// 
        /// - "key":"value"
        /// - "prefix": { "key":"value" }
        /// </summary>
        /// <param name="json">Json Object</param>
        public void LoadFromJson(JObject json)
        {
            foreach (var entry in json.Properties)
            {
                if (entry.Value is JString str)
                {
                    // "key":"value" in base64

                    Put(Convert.FromBase64String(entry.Key), Convert.FromBase64String(str.Value));
                }
                else if (entry.Value is JObject obj)
                {
                    // "prefix": { "key":"value" }  in base64

                    foreach (var subEntry in obj.Properties)
                    {
                        if (subEntry.Value is JString subStr)
                        {
                            Put(Convert.FromBase64String(entry.Key).Concat(Convert.FromBase64String(subEntry.Key)).ToArray(),
                                Convert.FromBase64String(subStr.Value));
                        }
                    }
                }
            }
        }
    }
}
