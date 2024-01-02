using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using System.ComponentModel;
using Neo;
using Neo.SmartContract.Framework.Services;

namespace Modifier
{
    public class OwnerOnlyAttribute : ModifierAttribute
    {
        readonly UInt160 _owner;

        public OwnerOnlyAttribute(string hex)
        {
            _owner = (UInt160)(byte[])StdLib.Base64Decode(hex);
        }

        public override void Enter()
        {
            if (!Runtime.CheckWitness(_owner)) throw new System.Exception();
        }

        public override void Exit() { }
    }

    [DisplayName("SampleModifier")]
    [ManifestExtra("Author", "core-dev")]
    [ManifestExtra("Description", "A sample contract to demonstrate how to use modifiers")]
    [ManifestExtra("Email", "core@neo.org")]
    [ManifestExtra("Version", "0.0.1")]
    [ContractSourceCode("https://github.com/neo-project/neo/examples/Exception")]
    public class SampleModifier : SmartContract
    {
        [OwnerOnly("AAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
        public static bool Test()
        {
            return true;
        }
    }
}
