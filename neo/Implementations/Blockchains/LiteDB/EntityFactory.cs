using System;
using Neo.Implementations.Blockchains.Utilities;


namespace Neo.Implementations.Blockchains.LiteDB
{
    public class EntityFactory : AbstractEntityFactory
    {
        public override AbstractDB Open(String path, AbstractOptions options)
        {
            return new DB(path);
        }

        public override AbstractOptions newOptions()
        {
            throw new NotImplementedException();
        }

        public override AbstractReadOptions newReadOptions()
        {
            throw new NotImplementedException();
        }

        public override AbstractReadOptions getDefaultReadOptions()
        {
            throw new NotImplementedException();
        }

        public override AbstractWriteBatch newWriteBatch()
        {
            throw new NotImplementedException();
        }

        public override AbstractWriteOptions getDefaultWriteOptions()
        {
            throw new NotImplementedException();
        }

        public override AbstractWriteOptions newWriteOptions()
        {
            throw new NotImplementedException();
        }
    }
}
