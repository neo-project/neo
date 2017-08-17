using System;
using System.Collections.Generic;
using Neo.Implementations.Blockchains.Utilities;

namespace Neo.Implementations.Blockchains.LiteDB
{
    public class SliceKeyValueWriteBatch : AbstractWriteBatch
    {
        private Dictionary<Slice, SliceKeyValue> batch = new Dictionary<Slice, SliceKeyValue>();

        public override void Clear()
        {
            batch.Clear();
        }

        public override void Delete(Slice key)
        {
            batch.Remove(key);
        }

        public override void Put(Slice key, Slice value)
        {
            SliceKeyValue entry = new SliceKeyValue(key, value);
            batch.Add(key, entry);
        }

        public override int Count()
        {
            return batch.Count;
        }
    }
}
