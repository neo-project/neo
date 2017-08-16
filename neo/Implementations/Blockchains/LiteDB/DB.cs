using System;
using System.Collections;
using Neo.Implementations.Blockchains.Utilities;
using LiteDB;

namespace Neo.Implementations.Blockchains.LiteDB
{
    internal class DB : AbstractDB
    {
        LiteDatabase db;

        internal DB(string name)
        {
            db = new LiteDatabase($"{name}/LiteDb.db");
        }

        public override void Dispose()
        {
            db.Dispose();
        }

        private LiteCollection<SliceKeyValue> getSlices()
        {
            return db.GetCollection<SliceKeyValue>("slices");
        }

        public override Slice Get(AbstractReadOptions options, Slice key)
        {
            LiteCollection<SliceKeyValue> slices = getSlices();
            IEnumerable iEnum = slices.Find(x => x.Key.Equals(key));
            IEnumerator it = iEnum.GetEnumerator();
            it.MoveNext();
            SliceKeyValue entry = (SliceKeyValue)it.Current;
            if (entry == null)
            {
                throw new DBException("not found");
            }
            return entry.Value;
        }

        public override AbstractSnapshot GetSnapshot()
        {
            throw new NotImplementedException();
        }

        public override AbstractIterator NewIterator(AbstractReadOptions options)
        {
            LiteCollection<SliceKeyValue> slices = getSlices();
            return new SliceKeyValueIterator(slices);
        }

        public override void Put(AbstractWriteOptions options, Slice key, Slice value)
        {
            getSlices().Insert(new SliceKeyValue(key, value));

        }


        public override bool TryGet(AbstractReadOptions options, Slice key, out Slice value)
        {
            LiteCollection<SliceKeyValue> slices = getSlices();
            IEnumerable iEnum = slices.Find(x => x.Key.Equals(key));
            IEnumerator it = iEnum.GetEnumerator();
            it.MoveNext();
            SliceKeyValue entry = (SliceKeyValue)it.Current;
            if (entry == null)
            {
                value = default(Slice);
                return false;
            }
            else
            {
                value = entry.Value;
                return true;
            }
        }

        public override void Write(AbstractWriteOptions options, AbstractWriteBatch write_batch)
        {
            WriteBatch wb = (WriteBatch)write_batch;
			getSlices().InsertBulk(wb.asSliceKeyValueList());
        }
    }
}