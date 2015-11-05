using Microsoft.Data.Entity;
using Microsoft.Data.Sqlite;

namespace AntShares.Implementations.Wallets.EntityFramework
{
    internal class WalletDataContext : DbContext
    {
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<Key> Keys { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<UnspentCoin> UnspentCoins { get; set; }

        private readonly string filename;

        public WalletDataContext(string filename)
        {
            this.filename = filename;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            SqliteConnectionStringBuilder sb = new SqliteConnectionStringBuilder();
            sb.DataSource = filename;
            optionsBuilder.UseSqlite(sb.ToString());
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Account>().HasKey(p => p.PublicKeyHash);
            modelBuilder.Entity<Account>().Property(p => p.PrivateKeyEncrypted).HasColumnType("VarBinary").HasMaxLength(96).IsRequired();
            modelBuilder.Entity<Account>().Property(p => p.PublicKeyHash).HasColumnType("Binary").HasMaxLength(20).IsRequired();
            modelBuilder.Entity<Contract>().HasKey(p => p.ScriptHash);
            modelBuilder.Entity<Contract>().Index(p => p.PublicKeyHash);
            modelBuilder.Entity<Contract>().HasOne(p => p.Account).WithMany().ForeignKey(p => p.PublicKeyHash).WillCascadeOnDelete();
            modelBuilder.Entity<Contract>().Property(p => p.RedeemScript).HasColumnType("VarBinary").HasMaxLength(1024).IsRequired();
            modelBuilder.Entity<Contract>().Property(p => p.ScriptHash).HasColumnType("Binary").HasMaxLength(20).IsRequired();
            modelBuilder.Entity<Contract>().Property(p => p.PublicKeyHash).HasColumnType("Binary").HasMaxLength(20).IsRequired();
            modelBuilder.Entity<Key>().HasKey(p => p.Name);
            modelBuilder.Entity<Key>().Property(p => p.Name).HasColumnType("VarChar").HasMaxLength(20).IsRequired();
            modelBuilder.Entity<Key>().Property(p => p.Value).HasColumnType("VarBinary").HasMaxLength(32).IsRequired();
            modelBuilder.Entity<Transaction>().HasKey(p => p.Hash);
            modelBuilder.Entity<Transaction>().Index(p => p.Type);
            modelBuilder.Entity<Transaction>().Property(p => p.Hash).HasColumnType("Binary").HasMaxLength(32).IsRequired();
            modelBuilder.Entity<Transaction>().Property(p => p.Type).IsRequired();
            modelBuilder.Entity<Transaction>().Property(p => p.RawData).HasColumnType("VarBinary").IsRequired();
            modelBuilder.Entity<UnspentCoin>().ToTable("Unspent").Ignore(p => p.Index).HasKey(p => new { p.TxId, p._Index });
            modelBuilder.Entity<UnspentCoin>().Index(p => p.AssetId);
            modelBuilder.Entity<UnspentCoin>().Index(p => p.ScriptHash);
            modelBuilder.Entity<UnspentCoin>().Property(p => p.TxId).HasColumnType("Binary").HasMaxLength(32).IsRequired();
            modelBuilder.Entity<UnspentCoin>().Property(p => p._Index).HasColumnName("Index").IsRequired();
            modelBuilder.Entity<UnspentCoin>().Property(p => p.AssetId).HasColumnType("Binary").HasMaxLength(32).IsRequired();
            modelBuilder.Entity<UnspentCoin>().Property(p => p.ScriptHash).HasColumnType("Binary").HasMaxLength(20).IsRequired();
        }
    }
}
