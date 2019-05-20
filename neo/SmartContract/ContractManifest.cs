namespace Neo.SmartContract
{
    public class ContractManifest
    {
        public ContractManifestGroup Group { get; set; }
        public ContractManifestFeatures Features { get; } = new ContractManifestFeatures();
        public ContractAbi Abi { get; set; }
        public ContractPermission Permissions { get; set; }
    }
}