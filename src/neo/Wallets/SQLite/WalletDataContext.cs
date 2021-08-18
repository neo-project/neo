// Copyright (C) 2015-2021 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Neo.Wallets.SQLite
{
    internal class WalletDataContext : DbContext
    {
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<Key> Keys { get; set; }

        private readonly string filename;

        public WalletDataContext(string filename)
        {
            this.filename = filename;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            SqliteConnectionStringBuilder sb = new()
            {
                DataSource = filename
            };
            optionsBuilder.UseSqlite(sb.ToString());
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Account>().ToTable(nameof(Account));
            modelBuilder.Entity<Account>().HasKey(p => p.PublicKeyHash);
            modelBuilder.Entity<Account>().Property(p => p.Nep2key).HasColumnType("VarChar").HasMaxLength(byte.MaxValue).IsRequired();
            modelBuilder.Entity<Account>().Property(p => p.PublicKeyHash).HasColumnType("Binary").HasMaxLength(20).IsRequired();
            modelBuilder.Entity<Address>().ToTable(nameof(Address));
            modelBuilder.Entity<Address>().HasKey(p => p.ScriptHash);
            modelBuilder.Entity<Address>().Property(p => p.ScriptHash).HasColumnType("Binary").HasMaxLength(20).IsRequired();
            modelBuilder.Entity<Contract>().ToTable(nameof(Contract));
            modelBuilder.Entity<Contract>().HasKey(p => p.ScriptHash);
            modelBuilder.Entity<Contract>().HasIndex(p => p.PublicKeyHash);
            modelBuilder.Entity<Contract>().HasOne(p => p.Account).WithMany().HasForeignKey(p => p.PublicKeyHash).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Contract>().HasOne(p => p.Address).WithMany().HasForeignKey(p => p.ScriptHash).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Contract>().Property(p => p.RawData).HasColumnType("VarBinary").IsRequired();
            modelBuilder.Entity<Contract>().Property(p => p.ScriptHash).HasColumnType("Binary").HasMaxLength(20).IsRequired();
            modelBuilder.Entity<Contract>().Property(p => p.PublicKeyHash).HasColumnType("Binary").HasMaxLength(20).IsRequired();
            modelBuilder.Entity<Key>().ToTable(nameof(Key));
            modelBuilder.Entity<Key>().HasKey(p => p.Name);
            modelBuilder.Entity<Key>().Property(p => p.Name).HasColumnType("VarChar").HasMaxLength(20).IsRequired();
            modelBuilder.Entity<Key>().Property(p => p.Value).HasColumnType("VarBinary").IsRequired();
        }
    }
}
