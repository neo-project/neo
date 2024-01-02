using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;

using System.ComponentModel;

namespace Transfer;

[DisplayName("Contract1")]
[ManifestExtra("Author", "<Your Name Or Company Here>")]
[ManifestExtra("Description", "<Description Here>")]
[ManifestExtra("Email", "<Your Public Email Here>")]
[ManifestExtra("Version", "1.0.0.0")]
[ContractSourceCode("https://github.com/cschuchardt88/neo-templates")]
[ContractPermission("*", "*")]
public class Contract1 : SmartContract
{
    [Safe]
    public static string SayHello(string name)
    {
        return $"Hello, {name}";
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
