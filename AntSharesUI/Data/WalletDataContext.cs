using System.Data.Entity;

namespace AntShares.Data
{
    internal class WalletDataContext : DbContext
    {
        public DbSet<Account> Accounts { get; set; }

        public DbSet<Contract> Contracts { get; set; }

        public DbSet<Key> Keys { get; set; }

        public DbSet<Transaction> Transactions { get; set; }

        public DbSet<UnspentCoin> UnspentCoins { get; set; }

        static WalletDataContext()
        {
            Database.SetInitializer(new CreateDatabaseIfNotExists<WalletDataContext>());
        }

        public WalletDataContext(string connectionString)
            : base(connectionString)
        {
        }
    }
}
