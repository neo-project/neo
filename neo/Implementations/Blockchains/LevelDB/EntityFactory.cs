using System;
using Neo.Implementations.Blockchains.Utilities;


namespace Neo.Implementations.Blockchains.LevelDB
{
    public class EntityFactory : AbstractEntityFactory
    {
        public override AbstractDB Open(String path, AbstractOptions options)
        {
            return DB.Open(path, (Options)options);
        }

        public override AbstractOptions newOptions()
        {
            return new Options();
        }

        public override AbstractReadOptions newReadOptions()
        {
            return new ReadOptions();
        }

        public override AbstractReadOptions getDefaultReadOptions()
        {
            return ReadOptions.Default;
        }

        public override AbstractWriteBatch newWriteBatch()
        {
            return new WriteBatch();
        }

        public override AbstractWriteOptions getDefaultWriteOptions()
        {
            return WriteOptions.Default;
        }

        public override AbstractWriteOptions newWriteOptions()
        {
            return new WriteOptions();
        }
    }
}
