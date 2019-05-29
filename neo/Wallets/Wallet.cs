using Neo.Cryptography;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using ECPoint = Neo.Cryptography.ECC.ECPoint;

namespace Neo.Wallets
{
    public abstract class Wallet
    {
        private static readonly Random rand = new Random();

        public abstract string Name { get; }
        public abstract Version Version { get; }

        public abstract bool Contains(UInt160 scriptHash);
        public abstract WalletAccount CreateAccount(byte[] privateKey);
        public abstract WalletAccount CreateAccount(Contract contract, KeyPair key = null);
        public abstract WalletAccount CreateAccount(UInt160 scriptHash);
        public abstract bool DeleteAccount(UInt160 scriptHash);
        public abstract WalletAccount GetAccount(UInt160 scriptHash);
        public abstract IEnumerable<WalletAccount> GetAccounts();

        public WalletAccount CreateAccount()
        {
            byte[] privateKey = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(privateKey);
            }
            WalletAccount account = CreateAccount(privateKey);
            Array.Clear(privateKey, 0, privateKey.Length);
            return account;
        }

        public WalletAccount CreateAccount(Contract contract, byte[] privateKey)
        {
            if (privateKey == null) return CreateAccount(contract);
            return CreateAccount(contract, new KeyPair(privateKey));
        }

        public void FillTransaction(Transaction tx, UInt160 sender = null)
        {
            if (tx.Nonce == 0)
                tx.Nonce = (uint)rand.Next();
            if (tx.ValidUntilBlock == 0)
                using (Snapshot snapshot = Blockchain.Singleton.GetSnapshot())
                    tx.ValidUntilBlock = snapshot.Height + Transaction.MaxValidUntilBlockIncrement;
            tx.CalculateFees();
            UInt160[] accounts = sender is null ? GetAccounts().Where(p => !p.Lock && !p.WatchOnly).Select(p => p.ScriptHash).ToArray() : new[] { sender };
            BigInteger fee = tx.Gas + tx.NetworkFee;
            using (Snapshot snapshot = Blockchain.Singleton.GetSnapshot())
                foreach (UInt160 account in accounts)
                {
                    BigInteger balance = NativeContract.GAS.BalanceOf(snapshot, account);
                    if (balance >= fee)
                    {
                        tx.Sender = account;
                        return;
                    }
                }
            throw new InvalidOperationException();
        }

        public WalletAccount GetAccount(ECPoint pubkey)
        {
            return GetAccount(Contract.CreateSignatureRedeemScript(pubkey).ToScriptHash());
        }

        public BigDecimal GetAvailable(UInt160 asset_id)
        {
            UInt160[] accounts = GetAccounts().Where(p => !p.WatchOnly).Select(p => p.ScriptHash).ToArray();
            return GetBalance(asset_id, accounts);
        }

        public BigDecimal GetBalance(UInt160 asset_id, params UInt160[] accounts)
        {
            byte[] script;
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitPush(0);
                foreach (UInt160 account in accounts)
                {
                    sb.EmitAppCall(asset_id, "balanceOf", account);
                    sb.Emit(OpCode.ADD);
                }
                sb.EmitAppCall(asset_id, "decimals");
                script = sb.ToArray();
            }
            ApplicationEngine engine = ApplicationEngine.Run(script, control: new GasControl(20000000L * accounts.Length));
            if (engine.State.HasFlag(VMState.FAULT))
                return new BigDecimal(0, 0);
            byte decimals = (byte)engine.ResultStack.Pop().GetBigInteger();
            BigInteger amount = engine.ResultStack.Pop().GetBigInteger();
            return new BigDecimal(amount, decimals);
        }

        public static byte[] GetPrivateKeyFromNEP2(string nep2, string passphrase, int N = 16384, int r = 8, int p = 8)
        {
            if (nep2 == null) throw new ArgumentNullException(nameof(nep2));
            if (passphrase == null) throw new ArgumentNullException(nameof(passphrase));
            byte[] data = nep2.Base58CheckDecode();
            if (data.Length != 39 || data[0] != 0x01 || data[1] != 0x42 || data[2] != 0xe0)
                throw new FormatException();
            byte[] addresshash = new byte[4];
            Buffer.BlockCopy(data, 3, addresshash, 0, 4);
            byte[] derivedkey = SCrypt.DeriveKey(Encoding.UTF8.GetBytes(passphrase), addresshash, N, r, p, 64);
            byte[] derivedhalf1 = derivedkey.Take(32).ToArray();
            byte[] derivedhalf2 = derivedkey.Skip(32).ToArray();
            byte[] encryptedkey = new byte[32];
            Buffer.BlockCopy(data, 7, encryptedkey, 0, 32);
            byte[] prikey = XOR(encryptedkey.AES256Decrypt(derivedhalf2), derivedhalf1);
            Cryptography.ECC.ECPoint pubkey = Cryptography.ECC.ECCurve.Secp256r1.G * prikey;
            UInt160 script_hash = Contract.CreateSignatureRedeemScript(pubkey).ToScriptHash();
            string address = script_hash.ToAddress();
            if (!Encoding.ASCII.GetBytes(address).Sha256().Sha256().Take(4).SequenceEqual(addresshash))
                throw new FormatException();
            return prikey;
        }

        public static byte[] GetPrivateKeyFromWIF(string wif)
        {
            if (wif == null) throw new ArgumentNullException();
            byte[] data = wif.Base58CheckDecode();
            if (data.Length != 34 || data[0] != 0x80 || data[33] != 0x01)
                throw new FormatException();
            byte[] privateKey = new byte[32];
            Buffer.BlockCopy(data, 1, privateKey, 0, privateKey.Length);
            Array.Clear(data, 0, data.Length);
            return privateKey;
        }

        public virtual WalletAccount Import(X509Certificate2 cert)
        {
            byte[] privateKey;
            using (ECDsa ecdsa = cert.GetECDsaPrivateKey())
            {
                privateKey = ecdsa.ExportParameters(true).D;
            }
            WalletAccount account = CreateAccount(privateKey);
            Array.Clear(privateKey, 0, privateKey.Length);
            return account;
        }

        public virtual WalletAccount Import(string wif)
        {
            byte[] privateKey = GetPrivateKeyFromWIF(wif);
            WalletAccount account = CreateAccount(privateKey);
            Array.Clear(privateKey, 0, privateKey.Length);
            return account;
        }

        public virtual WalletAccount Import(string nep2, string passphrase)
        {
            byte[] privateKey = GetPrivateKeyFromNEP2(nep2, passphrase);
            WalletAccount account = CreateAccount(privateKey);
            Array.Clear(privateKey, 0, privateKey.Length);
            return account;
        }

        public Transaction MakeTransaction(IEnumerable<TransactionAttribute> attributes, TransferOutput[] outputs, UInt160 from = null)
        {
            uint nonce = (uint)rand.Next();
            var totalPay = outputs.GroupBy(p => p.AssetId, (k, g) => (k, g.Select(p => p.Value.Value).Sum())).ToArray();
            UInt160[] accounts;
            if (from is null)
            {
                accounts = GetAccounts().Where(p => !p.Lock && !p.WatchOnly).Select(p => p.ScriptHash).ToArray();
            }
            else
            {
                if (!Contains(from)) return null;
                accounts = new[] { from };
            }
            TransactionAttribute[] attr = attributes?.ToArray() ?? new TransactionAttribute[0];
            using (Snapshot snapshot = Blockchain.Singleton.GetSnapshot())
            {
                uint validUntilBlock = snapshot.Height + Transaction.MaxValidUntilBlockIncrement;
                foreach (UInt160 account in accounts)
                {
                    Transaction tx = MakeTransaction(snapshot, 0, nonce, totalPay, outputs, account, validUntilBlock, attr);
                    if (tx != null) return tx;
                }
            }
            return null;
        }

        private Transaction MakeTransaction(Snapshot snapshot, byte version, uint nonce, (UInt160, BigInteger)[] totalPay, TransferOutput[] outputs, UInt160 sender, uint validUntilBlock, TransactionAttribute[] attributes)
        {
            BigInteger balance_gas = BigInteger.Zero;
            foreach (var (assetId, amount) in totalPay)
                using (ScriptBuilder sb = new ScriptBuilder())
                {
                    sb.EmitAppCall(assetId, "balanceOf", sender);
                    ApplicationEngine engine = ApplicationEngine.Run(sb.ToArray());
                    if (engine.State.HasFlag(VMState.FAULT)) return null;
                    BigInteger balance = engine.ResultStack.Peek().GetBigInteger();
                    if (balance < amount) return null;
                    if (assetId.Equals(NativeContract.GAS.Hash))
                    {
                        balance_gas = balance - amount;
                        if (balance_gas.Sign <= 0) return null;
                    }
                }
            byte[] script;
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                foreach (var output in outputs)
                {
                    sb.EmitAppCall(output.AssetId, "transfer", sender, output.ScriptHash, output.Value.Value);
                    sb.Emit(OpCode.THROWIFNOT);
                }
                script = sb.ToArray();
            }
            Transaction tx = new Transaction
            {
                Version = version,
                Nonce = nonce,
                Script = script,
                Sender = sender,
                ValidUntilBlock = validUntilBlock,
                Attributes = attributes
            };
            try
            {
                tx.CalculateFees();
            }
            catch (InvalidOperationException)
            {
                return null;
            }
            BigInteger fee = tx.Gas + tx.NetworkFee;
            if (balance_gas == BigInteger.Zero)
                balance_gas = NativeContract.GAS.BalanceOf(snapshot, sender);
            if (balance_gas < fee) return null;
            return tx;
        }

        public bool Sign(ContractParametersContext context)
        {
            WalletAccount account = GetAccount(context.ScriptHash);
            if (account?.HasKey != true) return false;
            KeyPair key = account.GetKey();
            byte[] signature = context.Verifiable.Sign(key);
            return context.AddSignature(account.Contract, key.PublicKey, signature);
        }

        public abstract bool VerifyPassword(string password);

        private static byte[] XOR(byte[] x, byte[] y)
        {
            if (x.Length != y.Length) throw new ArgumentException();
            return x.Zip(y, (a, b) => (byte)(a ^ b)).ToArray();
        }
    }
}
