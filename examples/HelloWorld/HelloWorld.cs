using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;

using System.ComponentModel;

namespace HelloWorld;

[DisplayName("HelloWorld")]
[ManifestExtra("Author", "code-dev")]
[ManifestExtra("Description", "A simple `hello world` contract")]
[ManifestExtra("Email", "core@neo.org")]
[ManifestExtra("Version", "0.0.1")]
[ContractSourceCode("https://github.com/neo-project/examples/HelloWorld")]
[ContractPermission("*", "*")]
public class HelloWorld : SmartContract
{
    [Safe]
    public static string SayHello()
    {
        return $"Hello, World!";
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
