using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AntShares.Wallets
{
    [Table("Key")]
    internal class Key
    {
        public const string MasterKey = "MasterKey";
        public const string IV = "IV";

        [Column(TypeName = "VarChar"), Key, MaxLength(20)]
        public string Name { get; set; }

        [Column(TypeName = "VarBinary"), MaxLength(32), Required]
        public byte[] Value { get; set; }
    }
}
