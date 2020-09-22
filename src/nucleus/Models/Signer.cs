using Neo.Cryptography.ECC;

namespace Neo.Models
{
    public class Signer
    {
        public UInt160 Account;
        public WitnessScope Scopes;
        public UInt160[] AllowedContracts;
        public ECPoint[] AllowedGroups;
    }
}
