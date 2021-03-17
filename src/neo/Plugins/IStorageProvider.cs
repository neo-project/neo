using Neo.Persistence;

namespace Neo.Plugins
{
    /// <summary>
    /// A provider used to create <see cref="IStore"/> instances.
    /// </summary>
    public interface IStorageProvider
    {
        /// <summary>
        /// Creates a new instance of the <see cref="IStore"/> interface.
        /// </summary>
        /// <param name="path">The path of the database.</param>
        /// <returns>The created <see cref="IStore"/> instance.</returns>
        IStore GetStore(string path);
    }
}
