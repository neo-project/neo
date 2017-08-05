using System;
namespace Neo.Implementations.Blockchains.Utilities
{
    public abstract class AbstractEntityFactory
	{
		public abstract AbstractDB Open(String path, AbstractOptions options);

		public abstract AbstractOptions newOptions();

		public abstract AbstractReadOptions newReadOptions();

		public abstract AbstractReadOptions getDefaultReadOptions();

		public abstract AbstractWriteBatch newWriteBatch();

		public abstract AbstractWriteOptions getDefaultWriteOptions();

		public abstract AbstractWriteOptions newWriteOptions();
    }
}
