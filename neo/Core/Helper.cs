using Neo.Cryptography;
using Neo.Implementations.Blockchains.LevelDB;
using Neo.SmartContract;
using Neo.VM;
using Neo.IO.Caching;
using Neo.Cryptography.ECC
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

            // set up a state machine with access to accounts, storages, etc
            // to be used by verification contracts
            StateMachine verificationMachine = GetVerificationStateMachine();

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

                // give the verification contract a fixed amount of gas to use
                // this could be set to any value
                Fixed8 verificationGas = Fixed8.One * 10;

                ApplicationEngine engine = new ApplicationEngine(TriggerType.Verification, verifiable, Blockchain.Default, verificationMachine, verificationGas);
                engine.LoadScript(verification, false);
                engine.LoadScript(verifiable.Scripts[i].InvocationScript, true);
                if (!engine.Execute()) return false;

                if (engine.EvaluationStack.Count != 1 || !engine.EvaluationStack.Pop().GetBoolean()) return false;

                // we will allow verification contracts the ability to alter the storages states
                // so that, for example, a verification contract could look up an address in storage
                // and determine if it can withdraw from an account
                // after that operation, the verification code will need to write back to storage
                // to indicate that, for example, account A has withdrawn X amount

                verificationMachine.CommitStorages();

            }
            return true;
        }
    }
}
