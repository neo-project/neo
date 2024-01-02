using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;

using System.ComponentModel;
using System.Numerics;
using Neo;
using Neo.SmartContract;
using Neo.SmartContract.Framework.Services;

namespace ContractCall;

[DisplayName("SampleContractCall")]
[ManifestExtra("Author", "core-dev")]
[ManifestExtra("Description", "A sample contract to demonstrate how to call a contract")]
[ManifestExtra("Email", "core@neo.org")]
[ManifestExtra("Version", "0.0.1")]
[ContractSourceCode("https://github.com/neo-project/neo/examples/ContractCall")]
[ContractPermission("*", "*")]
public class SampleContractCall : SmartContract
{
    [InitialValue("0x13a83e059c2eedd5157b766d3357bc826810905e", ContractParameterType.Hash160)]
    private static readonly UInt160 DummyTarget;

    public static void onNEP17Payment(UInt160 from, BigInteger amount, BigInteger data)
    {
        UInt160 tokenHash = Runtime.CallingScriptHash;
        if (!data.Equals(123)) return;
        UInt160 @this = Runtime.ExecutingScriptHash;
        BigInteger balanceOf = (BigInteger)Contract.Call(tokenHash, "balanceOf", CallFlags.All, @this);
        Contract.Call(DummyTarget, "dummyMethod", CallFlags.All, @this, tokenHash, balanceOf);
    }
}
