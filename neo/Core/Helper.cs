using Neo.Cryptography;
using Neo.SmartContract;
using Neo.VM;
using Neo.IO.Caching;
using Neo.Cryptography.ECC;
using Neo.Wallets;
using System;
using System.IO;
using System.Linq;

namespace Neo.Core
{
    /// <summary>
    /// 包含一系列签名与验证的扩展方法
    /// </summary>
    public static class Helper
    {
        public static byte[] GetHashData(this IVerifiable verifiable)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                verifiable.SerializeUnsigned(writer);
                writer.Flush();
                return ms.ToArray();
            }
        }

        /// <summary>
        /// 根据传入的账户信息，对可签名的对象进行签名
        /// </summary>
        /// <param name="verifiable">要签名的数据</param>
        /// <param name="key">用于签名的账户</param>
        /// <returns>返回签名后的结果</returns>
        public static byte[] Sign(this IVerifiable verifiable, KeyPair key)
        {
            using (key.Decrypt())
            {
                return Crypto.Default.Sign(verifiable.GetHashData(), key.PrivateKey, key.PublicKey.EncodePoint(false).Skip(1).ToArray());
            }
        }

        public static UInt160 ToScriptHash(this byte[] script)
        {
            return new UInt160(Crypto.Default.Hash160(script));
        }


        internal static StateMachine GetVerificationStateMachine()
        {
            Blockchain bc = Blockchain.Default;

            DataCache<UInt160, AccountState> accounts = bc.CreateCache<UInt160, AccountState>();
            DataCache<ECPoint, ValidatorState> validators = bc.CreateCache<ECPoint, ValidatorState>();
            DataCache<UInt256, AssetState> assets = bc.CreateCache<UInt256, AssetState>();
            DataCache<UInt160, ContractState> contracts = bc.CreateCache<UInt160, ContractState>();
            DataCache<StorageKey, StorageItem> storages = bc.CreateCache<StorageKey, StorageItem>();
            StateMachine machine = new StateMachine(accounts, validators, assets, contracts, storages);

            return machine;
        }

        internal static CachedScriptTable GetScriptTable()
        {
            DataCache<UInt160, ContractState> contracts = Blockchain.Default.CreateCache<UInt160, ContractState>();
            CachedScriptTable table = new CachedScriptTable(contracts);
            return table;
        }


        internal static bool IsStandardVerification(byte[] verification)
        {
            //these lengths are standard verification script lengths
            int verification_len_1 = 35;
            int verification_len_2 = 241;

            if(verification.Length == verification_len_1 || verification.Length == verification_len_2) {
                return true;
            };

            return false;
        }

        internal static bool VerifyScripts(this IVerifiable verifiable)
        {
            UInt160[] hashes;
            try
            {
                hashes = verifiable.GetScriptHashesForVerifying();
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            if (hashes.Length != verifiable.Scripts.Length) return false;


            for (int i = 0; i < hashes.Length; i++)
            {
                byte[] verification = verifiable.Scripts[i].VerificationScript;
                if (verification.Length == 0)
                {
                    using (ScriptBuilder sb = new ScriptBuilder())
                    {
                        sb.EmitAppCall(hashes[i].ToArray());
                        verification = sb.ToArray();
                    }
                }
                else
                {
                    if (hashes[i] != verification.ToScriptHash()) return false;
                }

                // these are the default application engine items
                // to use when verifying a 'standard' verification script
                InteropService state_reader = StateReader.Default;
                IScriptTable script_table = Blockchain.Default;
                Fixed8 verification_gas = Fixed8.Zero;

                // check to see if it is a standard verification script
                // if not, we will use a different state reader, script table, and gas
                bool is_standard = IsStandardVerification(verification);

                if( !is_standard) {
                    // use a statereader with read (possibly write) access to blockchain data
                    state_reader = GetVerificationStateMachine();
    
                    // use cached script table rather than blockchain
                    script_table = GetScriptTable();

                    // give the verification contract a fixed amount of gas to use
                    // this could be set to any value
                    verification_gas = Fixed8.One * 10;                    
                }


                ApplicationEngine engine = new ApplicationEngine(TriggerType.Verification, verifiable, script_table, state_reader, verification_gas);
                engine.LoadScript(verification, false);
                engine.LoadScript(verifiable.Scripts[i].InvocationScript, true);

                // execute the script.  if it halts in a bad state return false
                if (!engine.Execute()) return false;

                // if execution is successful, we check the evaluation stack
                // if the length of the evaluation stack is 1, and the item evaluates to true
                // that means that verification of this script has succeeded
                // and a transfer of assets will occur
                if (engine.EvaluationStack.Count != 1 || !engine.EvaluationStack.Pop().GetBoolean()) return false;

            }
            return true;
        }
    }
}
