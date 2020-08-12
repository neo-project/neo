using Neo.Cryptography;
using Neo.IO;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.VM;
using System;

namespace Neo.Wallets
{
    public static class Helper
    {
        public static long CalculateNetworkFee(StoreView snapshot, Transaction tx, Func<UInt160, byte[]> getAccountWitnessScript)
        {
            UInt160[] hashes = tx.GetScriptHashesForVerifying(snapshot);

            // base size for transaction: includes const_header + signers + attributes + script + hashes
            int size = Transaction.HeaderSize + tx.Signers.GetVarSize() + tx.Attributes.GetVarSize() + tx.Script.GetVarSize() + IO.Helper.GetVarSize(hashes.Length);
            long networkFee = 0;
            foreach (UInt160 hash in hashes)
            {
                byte[] witness_script = getAccountWitnessScript(hash);
                if (witness_script is null)
                {
                    var contract = snapshot.Contracts.TryGet(hash);
                    if (contract is null) continue;

                    // Empty invocation and verification scripts
                    size += Array.Empty<byte>().GetVarSize() * 2;

                    // Check verify cost
                    ContractMethodDescriptor verify = contract.Manifest.Abi.GetMethod("verify");
                    if (verify is null) throw new ArgumentException($"The smart contract {contract.ScriptHash} haven't got verify method");
                    ContractMethodDescriptor init = contract.Manifest.Abi.GetMethod("_initialize");
                    using ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Verification, tx, snapshot.Clone());
                    ExecutionContext context = engine.LoadScript(contract.Script, CallFlags.None, verify.Offset);
                    if (init != null) engine.LoadContext(context.Clone(init.Offset), false);
                    engine.LoadScript(Array.Empty<byte>(), CallFlags.None);
                    if (engine.Execute() == VMState.FAULT) throw new ArgumentException($"Smart contract {contract.ScriptHash} verification fault.");
                    if (engine.ResultStack.Count != 1 || !engine.ResultStack.Pop().GetBoolean()) throw new ArgumentException($"Smart contract {contract.ScriptHash} returns false.");

                    networkFee += engine.GasConsumed;
                }
                else if (witness_script.IsSignatureContract())
                {
                    size += 67 + witness_script.GetVarSize();
                    networkFee += ApplicationEngine.OpCodePrices[OpCode.PUSHDATA1] + ApplicationEngine.OpCodePrices[OpCode.PUSHDATA1] + ApplicationEngine.OpCodePrices[OpCode.PUSHNULL] + ApplicationEngine.ECDsaVerifyPrice;
                }
                else if (witness_script.IsMultiSigContract(out int m, out int n))
                {
                    int size_inv = 66 * m;
                    size += IO.Helper.GetVarSize(size_inv) + size_inv + witness_script.GetVarSize();
                    networkFee += ApplicationEngine.OpCodePrices[OpCode.PUSHDATA1] * m;
                    using (ScriptBuilder sb = new ScriptBuilder())
                        networkFee += ApplicationEngine.OpCodePrices[(OpCode)sb.EmitPush(m).ToArray()[0]];
                    networkFee += ApplicationEngine.OpCodePrices[OpCode.PUSHDATA1] * n;
                    using (ScriptBuilder sb = new ScriptBuilder())
                        networkFee += ApplicationEngine.OpCodePrices[(OpCode)sb.EmitPush(n).ToArray()[0]];
                    networkFee += ApplicationEngine.OpCodePrices[OpCode.PUSHNULL] + ApplicationEngine.ECDsaVerifyPrice * n;
                }
                else
                {
                    //We can support more contract types in the future.
                }
            }
            networkFee += size * NativeContract.Policy.GetFeePerByte(snapshot);
            return networkFee;
        }

        public static byte[] Sign(this IVerifiable verifiable, KeyPair key)
        {
            return Crypto.Sign(verifiable.GetHashData(), key.PrivateKey, key.PublicKey.EncodePoint(false)[1..]);
        }

        public static string ToAddress(this UInt160 scriptHash)
        {
            Span<byte> data = stackalloc byte[21];
            data[0] = ProtocolSettings.Default.AddressVersion;
            scriptHash.ToArray().CopyTo(data[1..]);
            return Base58.Base58CheckEncode(data);
        }

        public static UInt160 ToScriptHash(this string address)
        {
            byte[] data = address.Base58CheckDecode();
            if (data.Length != 21)
                throw new FormatException();
            if (data[0] != ProtocolSettings.Default.AddressVersion)
                throw new FormatException();
            return new UInt160(data.AsSpan(1));
        }

        internal static byte[] XOR(byte[] x, byte[] y)
        {
            if (x.Length != y.Length) throw new ArgumentException();
            byte[] r = new byte[x.Length];
            for (int i = 0; i < r.Length; i++)
                r[i] = (byte)(x[i] ^ y[i]);
            return r;
        }
    }
}
