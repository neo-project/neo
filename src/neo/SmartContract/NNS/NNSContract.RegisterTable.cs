using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.Nns
{
    partial class NnsContract
    {
        //set the operator of the name, only can called by the current owner
        [ContractMethod(0_03000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.ByteArray, ContractParameterType.Hash160 }, ParameterNames = new[] { "name", "operator" })]
        public StackItem SetOperator(ApplicationEngine engine, Array args)
        {
            byte[] tokenId = args[0].GetSpan().ToArray();
            UInt160 @operator = new UInt160(args[1].GetSpan());
            UInt160 owner = GetOwner(engine.Snapshot, tokenId);
            if (!InteropService.Runtime.CheckWitnessInternal(engine, owner)) return false;

            DomainState domainInfo = GetDomainInfo(engine.Snapshot, tokenId, true);
            if (domainInfo is null || domainInfo.IsExpired(engine.Snapshot)) return false;
            domainInfo.Operator = @operator;
            return true;
        }

        protected internal void Mint(ApplicationEngine engine, UInt160 account, byte[] tokenId, uint ttl)
        {
            base.Mint(engine, account, tokenId);
            DomainState domainInfo = GetDomainInfo(engine.Snapshot, tokenId);
            domainInfo.Operator = account;
            domainInfo.TimeToLive = ttl;
        }
    }
}
