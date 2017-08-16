using System;
using System.Collections;
using Neo.Implementations.Blockchains.Utilities;
using LiteDB;

namespace Neo.Implementations.Blockchains.LiteDB
{
    public class SliceKeyValueIterator : AbstractIterator
    {
        private LiteCollection<SliceKeyValue> slices;
        private IEnumerable iEnum;
        private IEnumerator it;

        internal SliceKeyValueIterator(LiteCollection<SliceKeyValue> slices)
        {
            this.slices = slices;
            iEnum = slices.FindAll();
            it = iEnum.GetEnumerator(); ;
        }

        public override void Dispose() { }

        public override void Seek(Slice key)
        {
            SeekToFirst();
            Next();

            bool found = false;
            while ((!found) && (Valid()
            ))
            {
                if (Key().Equals(key))
                {
                    found = true;
                }
                else
                {
                    Next();
                }
            }
        }

        private SliceKeyValue getCurrent()
        {
            return (SliceKeyValue)it.Current;
        }

        public override void Next() { it.MoveNext(); }

        public override bool Valid() { return it.Current != null; }

        public override Slice Key() { return getCurrent().Key; }

        public override Slice Value() { return getCurrent().Value; }

        public override void SeekToFirst() { it.Reset(); }
    }
}
