using AntShares.Core;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AntShares.Wallets
{
    [Table("Transaction")]
    internal class Transaction
    {
        [Column(TypeName = "Binary"), Key, MaxLength(32)]
        public byte[] Hash { get; set; }

        [Index]
        public TransactionType Type { get; set; }

        [Required]
        public byte[] RawData { get; set; }
    }
}
