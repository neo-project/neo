using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;

namespace Neo.SmartContract
{
    /// <summary>
    /// A helper class related to smart contract.
    /// </summary>
    public static class Helper
    {
        /// <summary>
        /// The maximum GAS that can be consumed when <see cref="VerifyWitnesses"/> is called.
        /// </summary>
        public const long MaxVerificationGas = 1_50000000;

        /// <summary>
        /// Calculates the verification fee for a signature address.
        /// </summary>
        /// <returns>The calculated cost.</returns>
        public static long SignatureContractCost() =>
            ApplicationEngine.OpCodePrices[OpCode.PUSHDATA1] * 2 +
            ApplicationEngine.OpCodePrices[OpCode.SYSCALL] +
            ApplicationEngine.CheckSigPrice;

        /// <summary>
        /// Calculates the verification fee for a multi-signature address.
        /// </summary>
        /// <param name="m">The minimum number of correct signatures that need to be provided in order for the verification to pass.</param>
        /// <param name="n">The number of public keys in the account.</param>
        /// <returns>The calculated cost.</returns>
        public static long MultiSignatureContractCost(int m, int n)
        {
            long fee = ApplicationEngine.OpCodePrices[OpCode.PUSHDATA1] * (m + n);
            using (ScriptBuilder sb = new())
                fee += ApplicationEngine.OpCodePrices[(OpCode)sb.EmitPush(m).ToArray()[0]];
            using (ScriptBuilder sb = new())
                fee += ApplicationEngine.OpCodePrices[(OpCode)sb.EmitPush(n).ToArray()[0]];
            fee += ApplicationEngine.OpCodePrices[OpCode.SYSCALL];
            fee += ApplicationEngine.CheckSigPrice * n;
            return fee;
        }

        /// <summary>
        /// Check the correctness of the script and ABI.
        /// </summary>
        /// <param name="script">The script of the contract.</param>
        /// <param name="abi">The ABI of the contract.</param>
        public static void Check(byte[] script, ContractAbi abi)
        {
            Check(new Script(script, true), abi);
        }

        /// <summary>
        /// Check the correctness of the script and ABI.
        /// </summary>
        /// <param name="script">The script of the contract.</param>
        /// <param name="abi">The ABI of the contract.</param>
        /// <remarks>Note: The <see cref="Script"/> passed to this method should be constructed with strict mode.</remarks>
        public static void Check(this Script script, ContractAbi abi)
        {
            foreach (ContractMethodDescriptor method in abi.Methods)
                script.GetInstruction(method.Offset);
            abi.GetMethod(string.Empty, 0); // Trigger the construction of ContractAbi.methodDictionary to check the uniqueness of the method names.
            _ = abi.Events.ToDictionary(p => p.Name); // Check the uniqueness of the event names.
        }

        /// <summary>
        /// Computes the hash of a deployed contract.
        /// </summary>
        /// <param name="sender">The sender of the transaction that deployed the contract.</param>
        /// <param name="nefCheckSum">The checksum of the nef file of the contract.</param>
        /// <param name="name">The name of the contract.</param>
        /// <returns>The hash of the contract.</returns>
        public static UInt160 GetContractHash(UInt160 sender, uint nefCheckSum, string name)
        {
            using var sb = new ScriptBuilder();
            sb.Emit(OpCode.ABORT);
            sb.EmitPush(sender);
            sb.EmitPush(nefCheckSum);
            sb.EmitPush(name);

            return sb.ToArray().ToScriptHash();
        }

        /// <summary>
        /// Gets the script hash of the specified <see cref="ExecutionContext"/>.
        /// </summary>
        /// <param name="context">The specified <see cref="ExecutionContext"/>.</param>
        /// <returns>The script hash of the context.</returns>
        public static UInt160 GetScriptHash(this ExecutionContext context)
        {
            return context.GetState<ExecutionContextState>().ScriptHash;
        }

        /// <summary>
        /// Determines whether the specified contract is a multi-signature contract.
        /// </summary>
        /// <param name="script">The script of the contract.</param>
        /// <returns><see langword="true"/> if the contract is a multi-signature contract; otherwise, <see langword="false"/>.</returns>
        public static bool IsMultiSigContract(this byte[] script)
        {
            return IsMultiSigContract(script, out _, out _, null);
        }

        /// <summary>
        /// Determines whether the specified contract is a multi-signature contract.
        /// </summary>
        /// <param name="script">The script of the contract.</param>
        /// <param name="m">The minimum number of correct signatures that need to be provided in order for the verification to pass.</param>
        /// <param name="n">The number of public keys in the account.</param>
        /// <returns><see langword="true"/> if the contract is a multi-signature contract; otherwise, <see langword="false"/>.</returns>
        public static bool IsMultiSigContract(this byte[] script, out int m, out int n)
        {
            return IsMultiSigContract(script, out m, out n, null);
        }

        /// <summary>
        /// Determines whether the specified contract is a multi-signature contract.
        /// </summary>
        /// <param name="script">The script of the contract.</param>
        /// <param name="m">The minimum number of correct signatures that need to be provided in order for the verification to pass.</param>
        /// <param name="points">The public keys in the account.</param>
        /// <returns><see langword="true"/> if the contract is a multi-signature contract; otherwise, <see langword="false"/>.</returns>
        public static bool IsMultiSigContract(this byte[] script, out int m, out ECPoint[] points)
        {
            List<ECPoint> list = new();
            if (IsMultiSigContract(script, out m, out _, list))
            {
                points = list.ToArray();
                return true;
            }
            else
            {
                points = null;
                return false;
            }
        }

        private static bool IsMultiSigContract(byte[] script, out int m, out int n, List<ECPoint> points)
        {
            m = 0; n = 0;
            int i = 0;
            if (script.Length < 42) return false;
            switch (script[i])
            {
                case (byte)OpCode.PUSHINT8:
                    m = script[++i];
                    ++i;
                    break;
                case (byte)OpCode.PUSHINT16:
                    m = BinaryPrimitives.ReadUInt16LittleEndian(script.AsSpan(++i));
                    i += 2;
                    break;
                case byte b when b >= (byte)OpCode.PUSH1 && b <= (byte)OpCode.PUSH16:
                    m = b - (byte)OpCode.PUSH0;
                    ++i;
                    break;
                default:
                    return false;
            }
            if (m < 1 || m > 1024) return false;
            while (script[i] == (byte)OpCode.PUSHDATA1)
            {
                if (script.Length <= i + 35) return false;
                if (script[++i] != 33) return false;
                points?.Add(ECPoint.DecodePoint(script.AsSpan(i + 1, 33), ECCurve.Secp256r1));
                i += 34;
                ++n;
            }
            if (n < m || n > 1024) return false;
            switch (script[i])
            {
                case (byte)OpCode.PUSHINT8:
                    if (script.Length <= i + 1 || n != script[++i]) return false;
                    ++i;
                    break;
                case (byte)OpCode.PUSHINT16:
                    if (script.Length < i + 3 || n != BinaryPrimitives.ReadUInt16LittleEndian(script.AsSpan(++i))) return false;
                    i += 2;
                    break;
                case byte b when b >= (byte)OpCode.PUSH1 && b <= (byte)OpCode.PUSH16:
                    if (n != b - (byte)OpCode.PUSH0) return false;
                    ++i;
                    break;
                default:
                    return false;
            }
            if (script.Length != i + 5) return false;
            if (script[i++] != (byte)OpCode.SYSCALL) return false;
            if (BinaryPrimitives.ReadUInt32LittleEndian(script.AsSpan(i)) != ApplicationEngine.System_Crypto_CheckMultisig)
                return false;
            return true;
        }

        /// <summary>
        /// Determines whether the specified contract is a signature contract.
        /// </summary>
        /// <param name="script">The script of the contract.</param>
        /// <returns><see langword="true"/> if the contract is a signature contract; otherwise, <see langword="false"/>.</returns>
        public static bool IsSignatureContract(this byte[] script)
        {
            if (script.Length != 40) return false;
            if (script[0] != (byte)OpCode.PUSHDATA1
                || script[1] != 33
                || script[35] != (byte)OpCode.SYSCALL
                || BinaryPrimitives.ReadUInt32LittleEndian(script.AsSpan(36)) != ApplicationEngine.System_Crypto_CheckSig)
                return false;
            return true;
        }

        /// <summary>
        /// Determines whether the specified contract is a standard contract. A standard contract is either a signature contract or a multi-signature contract.
        /// </summary>
        /// <param name="script">The script of the contract.</param>
        /// <returns><see langword="true"/> if the contract is a standard contract; otherwise, <see langword="false"/>.</returns>
        public static bool IsStandardContract(this byte[] script)
        {
            return script.IsSignatureContract() || script.IsMultiSigContract();
        }

        /// <summary>
        /// Convert the <see cref="StackItem"/> to an <see cref="IInteroperable"/>.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="IInteroperable"/>.</typeparam>
        /// <param name="item">The <see cref="StackItem"/> to convert.</param>
        /// <returns>The converted <see cref="IInteroperable"/>.</returns>
        public static T ToInteroperable<T>(this StackItem item) where T : IInteroperable, new()
        {
            T t = new();
            t.FromStackItem(item);
            return t;
        }

        /// <summary>
        /// Computes the hash of the specified script.
        /// </summary>
        /// <param name="script">The specified script.</param>
        /// <returns>The hash of the script.</returns>
        public static UInt160 ToScriptHash(this byte[] script)
        {
            return new UInt160(Crypto.Hash160(script));
        }

        /// <summary>
        /// Computes the hash of the specified script.
        /// </summary>
        /// <param name="script">The specified script.</param>
        /// <returns>The hash of the script.</returns>
        public static UInt160 ToScriptHash(this ReadOnlySpan<byte> script)
        {
            return new UInt160(Crypto.Hash160(script));
        }

        /// <summary>
        /// Verifies the witnesses of the specified <see cref="IVerifiable"/>.
        /// </summary>
        /// <param name="verifiable">The <see cref="IVerifiable"/> to be verified.</param>
        /// <param name="settings">The <see cref="ProtocolSettings"/> to be used for the verification.</param>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="gas">The maximum GAS that can be used.</param>
        /// <returns><see langword="true"/> if the <see cref="IVerifiable"/> is verified as valid; otherwise, <see langword="false"/>.</returns>
        public static bool VerifyWitnesses(this IVerifiable verifiable, ProtocolSettings settings, DataCache snapshot, long gas)
        {
            if (gas < 0) return false;
            if (gas > MaxVerificationGas) gas = MaxVerificationGas;

            UInt160[] hashes;
            try
            {
                hashes = verifiable.GetScriptHashesForVerifying(snapshot);
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            if (hashes.Length != verifiable.Witnesses.Length) return false;
            for (int i = 0; i < hashes.Length; i++)
            {
                if (!verifiable.VerifyWitness(settings, snapshot, hashes[i], verifiable.Witnesses[i], gas, out long fee))
                    return false;
                gas -= fee;
            }
            return true;
        }

        internal static bool VerifyWitness(this IVerifiable verifiable, ProtocolSettings settings, DataCache snapshot, UInt160 hash, Witness witness, long gas, out long fee)
        {
            fee = 0;
            Script invocationScript;
            try
            {
                invocationScript = new Script(witness.InvocationScript, true);
            }
            catch (BadScriptException)
            {
                return false;
            }
            using (ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Verification, verifiable, snapshot?.CreateSnapshot(), null, settings, gas))
            {
                if (witness.VerificationScript.Length == 0)
                {
                    ContractState cs = NativeContract.ContractManagement.GetContract(snapshot, hash);
                    if (cs is null) return false;
                    ContractMethodDescriptor md = cs.Manifest.Abi.GetMethod("verify", -1);
                    if (md?.ReturnType != ContractParameterType.Boolean) return false;
                    engine.LoadContract(cs, md, CallFlags.ReadOnly);
                }
                else
                {
                    if (NativeContract.IsNative(hash)) return false;
                    if (hash != witness.ScriptHash) return false;
                    Script verificationScript;
                    try
                    {
                        verificationScript = new Script(witness.VerificationScript, true);
                    }
                    catch (BadScriptException)
                    {
                        return false;
                    }
                    engine.LoadScript(verificationScript, initialPosition: 0, configureState: p =>
                    {
                        p.CallFlags = CallFlags.ReadOnly;
                        p.ScriptHash = hash;
                    });
                }

                engine.LoadScript(invocationScript, configureState: p => p.CallFlags = CallFlags.None);

                if (engine.Execute() == VMState.FAULT) return false;
                if (!engine.ResultStack.Peek().GetBoolean()) return false;
                fee = engine.GasConsumed;
            }
            return true;
        }
    }
}
