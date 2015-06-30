using AntShares.Properties;
using LevelDB;
using System;

namespace AntShares.Core
{
    internal class LevelDBBlockchain : Blockchain, IDisposable
    {
        private DB db;

        public LevelDBBlockchain()
        {
            db = DB.Open(Settings.Default.DataDirectoryPath);
        }

        public void Dispose()
        {
            if (db != null)
            {
                db.Dispose();
                db = null;
            }
        }
    }
}
