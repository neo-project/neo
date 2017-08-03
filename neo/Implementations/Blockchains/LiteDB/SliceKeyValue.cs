using System;
using Neo.Implementations.Blockchains.Utilities;
namespace Neo.Implementations.Blockchains.LiteDB
{
    public class SliceKeyValue
    {
        public Slice Key { get; set; }
        public Slice Value { get; set; }
        public SliceKeyValue(Slice Key, Slice Value)
        {
            this.Key = Key;
            this.Value = Value;
        }
    }
}
