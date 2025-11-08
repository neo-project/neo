# StateService

## RPC API

### GetStateRoot
#### Params
|Name|Type|Summary|Required|
|-|-|-|-|
|Index|uint|index|true|
#### Result
StateRoot Object
|Name|Type|Summary|
|-|-|-|
|version|number|version|
|index|number|index|
|roothash|string|version|
|witness|Object|witness from validators|

### GetProof
#### Params
|Name|Type|Summary|Required|
|-|-|-|-|
|RootHash|UInt256|state root|true|
|ScriptHash|UInt160|contract script hash|true|
|Key|base64 string|key|true|
#### Result
Proof in base64 string

### VerifyProof
#### Params
|Name|Type|Summary|
|-|-|-|
|RootHash|UInt256|state root|true|
|Proof|base64 string|proof|true|
#### Result
Value in base64 string

### GetStateheight
#### Result
|Name|Type|Summary|
|-|-|-|
|localrootindex|number|root hash index calculated locally|
|validatedrootindex|number|root hash index verified by validators|

### GetState
#### Params
|Name|Type|Summary|Required|
|-|-|-|-|
|RootHash|UInt256|specify state|true|
|ScriptHash|UInt160|contract script hash|true|
|Key|base64 string|key|true|
#### Result
Value in base64 string or `null`

### FindStates
#### Params
|Name|Type|Summary|Required|
|-|-|-|-|
|RootHash|UInt256|specify state|true|
|ScriptHash|UInt160|contract script hash|true|
|Prefix|base64 string|key prefix|true|
|From|base64 string|start key, default `Empty`|optional|
|Count|number|count of results in one request, default `MaxFindResultItems`|optional|
#### Result
|Name|Type|Summary|
|-|-|-|
|firstProof|string|proof of first value in results|
|lastProof|string|proof of last value in results|
|truncated|bool|whether the results is truncated because of limitation|
|results|array|key-values found|
