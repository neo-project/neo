## RestServer
In this section you will learn about `RestServer` plugin and how it works.

See [config.json](ConfigFile.md) for information about the configurations.

## Dependencies
- **Microsoft.AspNetCore.JsonPatch.dll** `Required`
- **Microsoft.AspNetCore.Mvc.NewtonsoftJson.dll** `Required`
- **Microsoft.AspNetCore.Mvc.Versioning.ApiExplorer.dll** `Required`
- **Microsoft.AspNetCore.Mvc.Versioning.dll** `Required`
- **Microsoft.OpenApi.dll** `Required`
- **Newtonsoft.Json.Bson.dll** `Required`
- **Newtonsoft.Json.dll** `Required`
- **System.ServiceProcess.ServiceController.dll** `Required`
- **Microsoft.AspNetCore.Mvc.Versioning.dll** `Required`
- **Microsoft.AspNetCore.Mvc.Versioning.dll** `Required`
- **Microsoft.AspNetCore.Mvc.Versioning.dll** `Required`
- **Microsoft.OpenApi.dll** `Swagger`
- **Swashbuckle.AspNetCore.Swagger.dll** `Swagger`
- **Swashbuckle.AspNetCore.SwaggerGen.dll** `Swagger`
- **Swashbuckle.AspNetCore.SwaggerUI.dll** `Swagger`
- **Swashbuckle.AspNetCore.Newtonsoft.dll** `Swagger`
- **RestServer.xml** `Swagger UI`

These files go in the same directory as the `RestServer.dll`. In neo-cli
`plugins/RestServer/` folder.

## Response Headers
| Name | Value(s) | Description |
| :---: | --- | :--- |
|**server**|_neo-cli/3.6.0 RestServer/3.6.0_|_`neo-cli` and `RestServer` version._|

Custom headers can be added by [Neo RestServer Plugins](Addons.md).

## JSON Serializer
`RestServer` uses custom NewtonSoft JSON Converters to serialize controller action
responses and `route` parameters.

**One Way Binding** - `Write` only.
- `Neo.SmartContract.ContractState`
- `Neo.SmartContract.NefFile`
- `Neo.SmartContract.MethodToken`
- `Neo.SmartContract.Native.TrimmedBlock`
- `Neo.SmartContract.Manifest.ContractAbi`
- `Neo.SmartContract.Manifest.ContractGroup`
- `Neo.SmartContract.Manifest.ContractManifest`
- `Neo.SmartContract.Manifest.ContractPermission`
- `Neo.SmartContract.Manifest.ContractPermissionDescriptor`
- `Neo.Network.P2P.Payloads.Block`
- `Neo.Network.P2P.Payloads.Header`
- `Neo.Network.P2P.Payloads.Signer`
- `Neo.Network.P2P.Payloads.TransactionAttribute`
- `Neo.Network.P2P.Payloads.Transaction`
- `Neo.Network.P2P.Payloads.Witness`

**Two Way Binding** - `Read` & `Write`
- `System.Guid`
- `System.ReadOnlyMemory<T>`
- `Neo.BigDecimal`
- `Neo.UInt160`
- `Neo.UInt256`
- `Neo.Cryptography.ECC.ECPoint`
- `Neo.VM.Types.Array`
- `Neo.VM.Types.Boolean`
- `Neo.VM.Types.Buffer`
- `Neo.VM.Types.ByteString`
- `Neo.VM.Types.Integer`
- `Neo.VM.Types.InteropInterface`
- `Neo.VM.Types.Null`
- `Neo.VM.Types.Map`
- `Neo.VM.Types.Pointer`
- `Neo.VM.Types.StackItem`
- `Neo.VM.Types.Struct`

## Remote Endpoints
Parametes `{hash}` can be any Neo N3 address or scripthash; `{address}` can be any Neo N3 address **only**; `{number}` and `{index}` can be any _**uint32**_.

**Parameter Examples**
- `{hash}` - _0xef4073a0f2b305a38ec4050e4d3d28bc40ea63f5_ **or** _NiHURyS83nX2mpxtA7xq84cGxVbHojj5Wc_
- `{address}` - _NiHURyS83nX2mpxtA7xq84cGxVbHojj5Wc_
- `{number}` - _1_
- `{index}` - _2500000_

**Paths**
- Utils
  - `[GET]` `/api/v1/utils/{hash}/address`
  - `[GET]` `/api/v1/utils/{address}/scripthash`
  - `[GET]` `/api/v1/utils/{hash}/{address}/validate`
- Node
  - `[GET]` `/api/v1/node/peers`
  - `[GET]` `/api/v1/node/plugins`
  - `[GET]` `/api/v1/node/settings`
- Ledger
  - `[GET]` `/api/v1/ledger/neo/accounts`
  - `[GET]` `/api/v1/ledger/gas/accounts`
  - `[GET]` `/api/v1/ledger/blocks?page={number}&size={number}`
  - `[GET]` `/api/v1/ledger/blocks/height`
  - `[GET]` `/api/v1/ledger/blocks/{index}`
  - `[GET]` `/api/v1/ledger/blocks/{index}/header`
  - `[GET]` `/api/v1/ledger/blocks/{index}/witness`
  - `[GET]` `/api/v1/ledger/blocks/{index}/transactions?page={number}&size={number}`
  - `[GET]` `/api/v1/ledger/transactions/{hash}`
  - `[GET]` `/api/v1/ledger/transactions/{hash}/witnesses`
  - `[GET]` `/api/v1/ledger/transactions/{hash}/signers`
  - `[GET]` `/api/v1/ledger/transactions/{hash}/atributes`
  - `[GET]` `/api/v1/ledger/memorypool?page={number}&size={number}`
  - `[GET]` `/api/v1/ledger/memorypool/verified?page={number}&size={number}`
  - `[GET]` `/api/v1/ledger/memorypool/unverified?page={number}&size={number}`
  - `[GET]` `/api/v1/ledger/memorypool/count`
- Tokens
  - `[GET]` `/api/v1/tokens/balanceof/{address}`
  - NFTs
    - `[GET]` `/api/v1/tokens/nep-11?page={number}&size={number}`
    - `[GET]` `/api/v1/tokens/nep-11/count`
    - `[GET]` `/api/v1/tokens/nep-11/{hash}/balanceof/{address}`
  - NEP-17
    - `[GET]` `/api/v1/tokens/nep-17?page={number}&size={number}`
    - `[GET]` `/api/v1/tokens/nep-17/count`
    - `[GET]` `/api/v1/tokens/nep-17/{hash}/balanceof/{address}`
- Contracts
  - `[GET]` `/api/v1/contracts?page={number}&size={number}`
  - `[GET]` `/api/v1/contracts/count`
  - `[GET]` `/api/v1/contracts/{hash}`
  - `[GET]` `/api/v1/contracts/{hash}/abi`
  - `[GET]` `/api/v1/contracts/{hash}/manifest`
  - `[GET]` `/api/v1/contracts/{hash}/nef`
  - `[GET]` `/api/v1/contracts/{hash}/storage`
- Wallet
  - `[POST]` `/api/v1/wallet/open`
  - `[POST]` `/api/v1/wallet/create`
  - `[POST]` `/api/v1/wallet/{session}/address/create`
  - `[GET]` `/api/v1/wallet/{session}/address/list`
  - `[GET]` `/api/v1/wallet/{session}/asset/list`
  - `[GET]` `/api/v1/wallet/{session}/balance/list`
  - `[POST]` `/api/v1/wallet/{session}/changepassword`
  - `[GET]` `/api/v1/wallet/{session}/close`
  - `[GET]` `/api/v1/wallet/{session}/delete/{address}`
  - `[GET]` `/api/v1/wallet/{session}/export/{address}`
  - `[GET]` `/api/v1/wallet/{session}/export`
  - `[GET]` `/api/v1/wallet/{session}/gas/unclaimed`
  - `[GET]` `/api/v1/wallet/{session}/key/list`
  - `[POST]` `/api/v1/wallet/{session}/import`
  - `[POST]` `/api/v1/wallet/{session}/import/multisigaddress`
  - `[POST]` `/api/v1/wallet/{session}/transfer`
