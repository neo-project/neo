using Neo.Cryptography;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
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
    public abstract class Wallet : IDisposable
    {
        public abstract event EventHandler<WalletTransactionEventArgs> WalletTransaction;

        private static readonly Random rand = new Random();

        public abstract string Name { get; }
        public abstract Version Version { get; }
        public abstract uint WalletHeight { get; }

        public abstract void ApplyTransaction(Transaction tx);
        public abstract bool Contains(UInt160 scriptHash);
        public abstract WalletAccount CreateAccount(byte[] privateKey);
        public abstract WalletAccount CreateAccount(Contract contract, KeyPair key = null);
        public abstract WalletAccount CreateAccount(UInt160 scriptHash);
        public abstract bool DeleteAccount(UInt160 scriptHash);
        public abstract WalletAccount GetAccount(UInt160 scriptHash);
        public abstract IEnumerable<WalletAccount> GetAccounts();
        public abstract IEnumerable<UInt256> GetTransactions();

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

        public virtual void Dispose()
        {
        }

        public WalletAccount GetAccount(ECPoint pubkey)
        {
            return GetAccount(Contract.CreateSignatureRedeemScript(pubkey).ToScriptHash());
        }

        public BigDecimal GetAvailable(UInt160 asset_id)
        {
            byte[] script;
            UInt160[] accounts = GetAccounts().Where(p => !p.WatchOnly).Select(p => p.ScriptHash).ToArray();
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
            ApplicationEngine engine = ApplicationEngine.Run(script, extraGAS: 20000000L * accounts.Length);
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

        public Transaction MakeTransaction(List<TransactionAttribute> attributes, IEnumerable<TransferOutput> outputs, UInt160 from = null)
        {
            var cOutputs = outputs.GroupBy(p => new
            {
                p.AssetId,
                Account = p.ScriptHash
            }, (k, g) => new
            {
                k.AssetId,
                Value = g.Aggregate(BigInteger.Zero, (x, y) => x + y.Value.Value),
                k.Account
            }).ToArray();
            Transaction tx;
            if (attributes == null) attributes = new List<TransactionAttribute>();
            UInt160[] accounts = from == null ? GetAccounts().Where(p => !p.Lock && !p.WatchOnly).Select(p => p.ScriptHash).ToArray() : new[] { from };
            HashSet<UInt160> sAttributes = new HashSet<UInt160>();
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                foreach (var output in cOutputs)
                {
                    var balances = new List<(UInt160 Account, BigInteger Value)>();
                    foreach (UInt160 account in accounts)
                    {
                        byte[] script;
                        using (ScriptBuilder sb2 = new ScriptBuilder())
                        {
                            sb2.EmitAppCall(output.AssetId, "balanceOf", account);
                            script = sb2.ToArray();
                        }
                        ApplicationEngine engine = ApplicationEngine.Run(script);
                        if (engine.State.HasFlag(VMState.FAULT)) return null;
                        balances.Add((account, engine.ResultStack.Pop().GetBigInteger()));
                    }
                    BigInteger sum = balances.Aggregate(BigInteger.Zero, (x, y) => x + y.Value);
                    if (sum < output.Value) return null;
                    if (sum != output.Value)
                    {
                        balances = balances.OrderByDescending(p => p.Value).ToList();
                        BigInteger amount = output.Value;
                        int i = 0;
                        while (balances[i].Value <= amount)
                            amount -= balances[i++].Value;
                        if (amount == BigInteger.Zero)
                            balances = balances.Take(i).ToList();
                        else
                            balances = balances.Take(i).Concat(new[] { balances.Last(p => p.Value >= amount) }).ToList();
                        sum = balances.Aggregate(BigInteger.Zero, (x, y) => x + y.Value);
                    }
                    sAttributes.UnionWith(balances.Select(p => p.Account));
                    for (int i = 0; i < balances.Count; i++)
                    {
                        BigInteger value = balances[i].Value;
                        if (i == 0)
                        {
                            BigInteger change = sum - output.Value;
                            if (change > 0) value -= change;
                        }
                        sb.EmitAppCall(output.AssetId, "transfer", balances[i].Account, output.Account, value);
                        sb.Emit(OpCode.THROWIFNOT);
                    }
                }
                byte[] nonce = new byte[8];
                rand.NextBytes(nonce);
                sb.Emit(OpCode.RET, nonce);
                tx = new Transaction
                {
                    Script = sb.ToArray()
                };
            }
            attributes.AddRange(sAttributes.Select(p => new TransactionAttribute
            {
                Usage = TransactionAttributeUsage.Script,
                Data = p.ToArray()
            }));
            tx.Attributes = attributes.ToArray();
            tx.Witnesses = new Witness[0];
            using (ApplicationEngine engine = ApplicationEngine.Run(tx.Script, tx))
            {
                if (engine.State.HasFlag(VMState.FAULT)) return null;
                tx = new Transaction
                {
                    Script = tx.Script,
                    Gas = Transaction.GetGas(engine.GasConsumed),
                    Attributes = tx.Attributes
                };
            }
            return tx;
        }

        public bool Sign(ContractParametersContext context)
        {
            bool fSuccess = false;
            foreach (UInt160 scriptHash in context.ScriptHashes)
            {
                WalletAccount account = GetAccount(scriptHash);
                if (account?.HasKey != true) continue;
                KeyPair key = account.GetKey();
                byte[] signature = context.Verifiable.Sign(key);
                fSuccess |= context.AddSignature(account.Contract, key.PublicKey, signature);
            }
            return fSuccess;
        }

        public abstract bool VerifyPassword(string password);

        private static byte[] XOR(byte[] x, byte[] y)
        {
            if (x.Length != y.Length) throw new ArgumentException();
            return x.Zip(y, (a, b) => (byte)(a ^ b)).ToArray();
        }
    }
}
