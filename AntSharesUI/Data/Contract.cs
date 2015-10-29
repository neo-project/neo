using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AntShares.Data
{
    internal class Contract
    {
        [MaxLength(1024), Required]
        public byte[] RedeemScript { get; set; }

        [Column(TypeName = "Binary"), Key, MaxLength(20)]
        public byte[] ScriptHash { get; set; }

        [Column(TypeName = "Binary"), Index, MaxLength(20), Required]
        public byte[] PublicKeyHash { get; set; }

        [ForeignKey(nameof(PublicKeyHash))]
        public Account Account { get; set; }
    }
}
