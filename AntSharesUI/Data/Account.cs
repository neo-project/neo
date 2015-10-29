using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AntShares.Data
{
    internal class Account
    {
        [MaxLength(96), Required]
        public byte[] PrivateKeyEncrypted { get; set; }

        [Column(TypeName = "Binary"), Key, MaxLength(20)]
        public byte[] PublicKeyHash { get; set; }
    }
}
