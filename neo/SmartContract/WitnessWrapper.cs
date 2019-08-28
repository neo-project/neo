using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using System.Linq;

namespace Neo.SmartContract
{
    internal class WitnessWrapper
    {
        public byte[] VerificationScript;

        public static WitnessWrapper[] Create(IVerifiable verifiable, Snapshot snapshot)
        {
            WitnessWrapper[] wrappers = verifiable.Witnesses.Select(p => new WitnessWrapper
            {
                VerificationScript = p.VerificationScript
            }).ToArray();
            if (wrappers.Any(p => p.VerificationScript.Length == 0))
            {
                UInt160[] hashes = verifiable.GetScriptHashesForVerifying(snapshot);
                for (int i = 0; i < wrappers.Length; i++)
                    if (wrappers[i].VerificationScript.Length == 0)
                        wrappers[i].VerificationScript = snapshot.Contracts[hashes[i]].Script;
            }
            return wrappers;
        }
    }
}
