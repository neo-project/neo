using Neo.SDK.RPC.Model;

namespace Neo.SDK.RPC
{

    /// <summary>
    /// Wrappar of NEO APIs
    /// </summary>
    public interface IRpcClient
    {
        /// <summary>
        /// Queries global assets (NEO, GAS, and etc.) of the account, according to the account address.
        /// </summary>
        GetAccountState GetAccountState(string address);

        /// <summary>
        /// Broadcasts a transaction over the NEO network.
        /// </summary>
        bool SendRawTransaction(string rawTransaction);

        /// <summary>
        /// Returns information of the unspent UTXO assets (e.g. NEO, GAS) at the specified address.
        /// provided by the plugin RpcSystemAssetTracker
        /// </summary>
        GetUnspents GetUnspents(string address);

        /// <summary>
        /// Returns the balance of all NEP-5 assets in the specified address.
        /// </summary>
        GetNep5Balances GetNep5Balances(string address);

        /// <summary>
        /// Returns claimable GAS information of the specified address.
        /// </summary>
        GetClaimable GetClaimable(string address);

        /// <summary>
        /// Returns the result after calling a smart contract at scripthash with the given parameters.
        /// This method is provided by the plugin RpcWallet.
        /// This RPC call does not affect the blockchain in any way.
        /// </summary>
        InvokeRet Invoke(string address, Stack[] stacks);

        /// <summary>
        /// Returns the result after calling a smart contract at scripthash with the given operation and parameters.
        /// This method is provided by the plugin RpcWallet.
        /// This RPC call does not affect the blockchain in any way.
        /// </summary>
        InvokeRet InvokeFunction(string address, string function, Stack[] stacks);

        /// <summary>
        /// Returns the result after passing a script through the VM.
        /// This method is provided by the plugin RpcWallet.
        /// This RPC call does not affect the blockchain in any way.
        /// </summary>
        InvokeRet InvokeScript(string script);

    }
}
