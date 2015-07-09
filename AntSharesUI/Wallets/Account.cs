using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AntShares.Wallets
{
    [Table("Account")]
    internal class Account
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity), Key]
        public int Id { get; set; }

        [Column(TypeName = "Binary"), Index(IsUnique = true), MaxLength(20), Required]
        public byte[] ScriptHash { get; set; }

        [MaxLength(547), Required]
        public byte[] RedeemScript { get; set; }

        [MaxLength(512), Required]
        public byte[] PrivateKeyEncrypted { get; set; }
    }
}
