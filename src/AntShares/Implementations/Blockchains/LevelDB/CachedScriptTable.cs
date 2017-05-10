using AntShares.Core;
using AntShares.VM;

namespace AntShares.Implementations.Blockchains.LevelDB
{
    internal class CachedScriptTable : IScriptTable
    {
        private DbCache<UInt160, ContractState> contracts;

        public CachedScriptTable(DbCache<UInt160, ContractState> contracts)
        {
            this.contracts = contracts;
        }

        byte[] IScriptTable.GetScript(byte[] script_hash)
        {
            return contracts[new UInt160(script_hash)].Code.Script;
        }
    }
}
