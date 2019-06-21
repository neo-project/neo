using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Neo.Network.P2P.Payloads;
using Neo.SDK.TX;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.VM;

namespace Neo.SDK.SC
{
    public class ContractHelper
    {
        private readonly TransactionHelper txHelper;


        public static void Invoke()
        {
            throw new NotImplementedException();
        }

        public static void MockInvoke()
        {
            throw new NotImplementedException();
        }

        //deploy contract
        public bool DeployContract(string avmFilePath, bool hasStorage, bool isPayable)
        {
            // generate the script and script hash
            FileInfo info = new FileInfo(avmFilePath);
            if (!info.Exists || info.Extension.ToLower() != ".avm" || info.Length >= Transaction.MaxTransactionSize)
                throw new ArgumentException(nameof(avmFilePath));

            byte[] fileScript = File.ReadAllBytes(avmFilePath);
            UInt160 scriptHash = fileScript.ToScriptHash();
            ContractFeatures properties = ContractFeatures.NoProperty;
            if (hasStorage) properties |= ContractFeatures.HasStorage;
            if (isPayable) properties |= ContractFeatures.Payable;

            byte[] script;
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitSysCall(InteropService.Neo_Contract_Create, fileScript, properties);
                script = sb.ToArray();
            }

            // create a transaction
            Transaction tx = txHelper.CreateTransaction(script);

            // sign the transaction
            ContractParametersContext context = new ContractParametersContext(tx);

            // use send raw transaction

        }

        //invoke contract
    }
}
