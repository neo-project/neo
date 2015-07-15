using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AntShares.Wallets
{
    [Table("Unspent")]
    internal class UnspentCoin
    {
        [Column(Order = 0, TypeName = "Binary"), Key, MaxLength(32)]
        public byte[] TxId { get; set; }

        [Column("Index", Order = 1), Key]
        public short _Index { get; set; }
        [NotMapped]
        public ushort Index
        {
            get
            {
                return (ushort)_Index;
            }
            set
            {
                _Index = (short)value;
            }
        }

        [Column(TypeName = "Binary"), Index, MaxLength(32), Required]
        public byte[] AssetId { get; set; }

        public long Value { get; set; }

        [Column(TypeName = "Binary"), Index, MaxLength(20), Required]
        public byte[] ScriptHash { get; set; }
    }
}
