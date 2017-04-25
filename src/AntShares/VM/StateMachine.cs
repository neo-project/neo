using AntShares.Core;
using AntShares.Cryptography.ECC;
using AntShares.IO.Caching;
using System.Collections.Generic;
using System.Linq;

namespace AntShares.VM
{
    internal class StateMachine : StateReader
    {
        private CloneCache<UInt160, AccountState> accounts;

        public StateMachine(DataCache<UInt160, AccountState> accounts)
        {
            this.accounts = new CloneCache<UInt160, AccountState>(accounts);
            Register("AntShares.Account.SetVotes", Account_SetVotes);
        }

        public void Commit()
        {
            accounts.Commit();
        }

        private HashSet<UInt160> _hashes_for_verifying = null;
        private HashSet<UInt160> GetScriptHashesForVerifying(ExecutionEngine engine)
        {
            if (_hashes_for_verifying == null)
            {
                IVerifiable container = (IVerifiable)engine.ScriptContainer;
                _hashes_for_verifying = new HashSet<UInt160>(container.GetScriptHashesForVerifying());
            }
            return _hashes_for_verifying;
        }

        protected override bool Blockchain_GetAccount(ExecutionEngine engine)
        {
            UInt160 hash = new UInt160(engine.EvaluationStack.Pop().GetByteArray());
            engine.EvaluationStack.Push(StackItem.FromInterface(accounts[hash]));
            return true;
        }

        private bool Account_SetVotes(ExecutionEngine engine)
        {
            AccountState account = engine.EvaluationStack.Pop().GetInterface<AccountState>();
            if (account == null) return false;
            account = accounts[account.ScriptHash];
            if (account.IsFrozen) return false;
            if (!account.Balances.ContainsKey(Blockchain.SystemShare.Hash) || account.Balances[Blockchain.SystemShare.Hash].Equals(Fixed8.Zero))
                return false;
            if (!GetScriptHashesForVerifying(engine).Contains(account.ScriptHash)) return false;
            account = accounts.GetAndChange(account.ScriptHash);
            account.Votes = engine.EvaluationStack.Pop().GetArray().Select(p => ECPoint.DecodePoint(p.GetByteArray(), ECCurve.Secp256r1)).ToArray();
            return true;
        }
    }
}
