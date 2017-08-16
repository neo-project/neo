using Neo.Implementations.Blockchains.Utilities;
using System.Collections.Generic;

namespace Neo.Implementations.Blockchains.LiteDB
{
    public class WriteBatch : AbstractWriteBatch
    {

        Dictionary<Slice, Slice> batch = new Dictionary<Slice, Slice>();

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
            batch.Add(key, value);
        }

        public List<SliceKeyValue> asSliceKeyValueList()
        {
            List<SliceKeyValue> list = new List<SliceKeyValue>();

            foreach (KeyValuePair<Slice, Slice> entry in batch)
            {
                list.Add(new SliceKeyValue(entry.Key, entry.Value));
            }

            return list;
        }
    }
}
