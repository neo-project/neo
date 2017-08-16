using System;
using Neo.Implementations.Blockchains.Utilities;

namespace Neo.Implementations.Blockchains.LiteDB
{
    public partial class EntityFactory : AbstractEntityFactory
    {

        public override AbstractDB Open(String path, AbstractOptions options)
        {
            return new DB(path);
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
            return new ReadOptions();
        }

        public override AbstractWriteBatch newWriteBatch()
        {
            return new WriteBatch();

		 }

        public override AbstractWriteOptions getDefaultWriteOptions()
        {
            return new WriteOptions();
        }

        public override AbstractWriteOptions newWriteOptions()
		{
			return new WriteOptions();
        }
    }
}
