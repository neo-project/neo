using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Sqlite;

namespace AntShares.Implementations.Wallets.EntityFramework
{
    internal class WalletDataContext : DbContext
    {
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<Key> Keys { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Coin> Coins { get; set; }

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
            modelBuilder.Entity<Contract>().HasIndex(p => p.PublicKeyHash);
            modelBuilder.Entity<Contract>().HasOne(p => p.Account).WithMany().HasForeignKey(p => p.PublicKeyHash).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Contract>().Property(p => p.Type).HasColumnType("VarChar").HasMaxLength(100).IsRequired();
            modelBuilder.Entity<Contract>().Property(p => p.RawData).HasColumnType("VarBinary").IsRequired();
            modelBuilder.Entity<Contract>().Property(p => p.ScriptHash).HasColumnType("Binary").HasMaxLength(20).IsRequired();
            modelBuilder.Entity<Contract>().Property(p => p.PublicKeyHash).HasColumnType("Binary").HasMaxLength(20).IsRequired();
            modelBuilder.Entity<Key>().HasKey(p => p.Name);
            modelBuilder.Entity<Key>().Property(p => p.Name).HasColumnType("VarChar").HasMaxLength(20).IsRequired();
            modelBuilder.Entity<Key>().Property(p => p.Value).HasColumnType("VarBinary").IsRequired();
            modelBuilder.Entity<Transaction>().HasKey(p => p.Hash);
            modelBuilder.Entity<Transaction>().HasIndex(p => p.Type);
            modelBuilder.Entity<Transaction>().Property(p => p.Hash).HasColumnType("Binary").HasMaxLength(32).IsRequired();
            modelBuilder.Entity<Transaction>().Property(p => p.Type).IsRequired();
            modelBuilder.Entity<Transaction>().Property(p => p.RawData).HasColumnType("VarBinary").IsRequired();
            modelBuilder.Entity<Transaction>().Property(p => p.Height);
            modelBuilder.Entity<Transaction>().Property(p => p.Time).IsRequired();
            modelBuilder.Entity<Coin>().HasKey(p => new { p.TxId, p.Index });
            modelBuilder.Entity<Coin>().HasIndex(p => p.AssetId);
            modelBuilder.Entity<Coin>().HasIndex(p => p.ScriptHash);
            modelBuilder.Entity<Coin>().HasOne(p => p.Contract).WithMany().HasForeignKey(p => p.ScriptHash).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Coin>().Property(p => p.TxId).HasColumnType("Binary").HasMaxLength(32).IsRequired();
            modelBuilder.Entity<Coin>().Property(p => p.Index).IsRequired();
            modelBuilder.Entity<Coin>().Property(p => p.AssetId).HasColumnType("Binary").HasMaxLength(32).IsRequired();
            modelBuilder.Entity<Coin>().Property(p => p.ScriptHash).HasColumnType("Binary").HasMaxLength(20).IsRequired();
            modelBuilder.Entity<Coin>().Property(p => p.State).IsRequired();
        }
    }
}
