using System;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;

using System.ComponentModel;
using Neo;
using Neo.SmartContract.Framework.Services;

namespace Inscription
{
    [DisplayName("SampleInscription")]
    [ManifestExtra("Author", "core-dev")]
    [ManifestExtra("Description", "A sample inscription contract.")]
    [ManifestExtra("Email", "core@neo.org")]
    [ManifestExtra("Version", "0.0.1")]
    [ContractSourceCode("https://github.com/neo-project/examples/Inscription")]
    [ContractPermission("*", "*")]
    public class SampleInscription : SmartContract
    {
        // Event for logging inscriptions
        [DisplayName("InscriptionAdded")]
        public static event Action<UInt160, string> InscriptionAdded;

        // Method to store an inscription
        public static void AddInscription(UInt160 address, string inscription)
        {
            if (!Runtime.CheckWitness(address))
                throw new Exception("Unauthorized: Caller is not the address owner");

            Storage.Put(Storage.CurrentContext, address, inscription);
            InscriptionAdded(address, inscription);
        }

        // Method to read an inscription
        [Safe]
        public static string GetInscription(UInt160 address)
        {
            return Storage.Get(Storage.CurrentContext, address);
        }

        [DisplayName("_deploy")]
        public static void OnDeployment(object data, bool update)
        {
            if (update)
            {
                // Add logic for fixing contract on update
                return;
            }
            // Add logic here for 1st time deployed
        }

        // TODO: Allow ONLY contract owner to call update
        public static bool Update(ByteString nefFile, string manifest)
        {
            ContractManagement.Update(nefFile, manifest);
            return true;
        }

        // TODO: Allow ONLY contract owner to call destroy
        public static bool Destroy()
        {
            ContractManagement.Destroy();
            return true;
        }
    }
}
