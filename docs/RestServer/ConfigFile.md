## Table

| Name | Type | Description |
| :--- | :---: | :--- |
|**Network**|_uint32_|_Network you would like the `RestServer` to be enabled on._|
|**BindAddress**|_string_|_Ip address of the interface you want to bind too._|
|**Port**|_uint32_|_Port number to bind too._|
|**KeepAliveTimeout**|_uint32_|_Time to keep the request alive, in seconds._|
|**SslCertFile**|_string_|_Is the path and file name of a certificate file, relative to the directory that contains the node's executable files._|
|**SslCertPassword**|_string_|_Is the password required to access the `X.509` certificate data._|
|**TrustedAuthorities**|_StringArray_|_Tumbprints of the of the last certificate authority in the chain._|
|**EnableBasicAuthentication**|_boolean_|_enables basic authentication._|
|**RestUser**|_string_|_Basic authentication's `username`._|
|**RestPass**|_string_|_Basic authentication's `password`._|
|**EnableCors**|_boolean_|_Enables Cross-origin resource sharing (`CORS`). Note by default it enables `*` any origin._|
|**AllowOrigins**|_StringArray_|_A list of the origins to allow. Note needs to add origins for basic auth to work with `CORS`._|
|**DisableControllers**|_StringArray_|_A list of `controllers` to be disabled. Requires restart of the node, if changed._|
|**EnableCompression**|_boolean_|_Enables `GZip` data compression._|
|**CompressionLevel**|_enum_|_Compression level. Values can be `Fastest`, `Optimal`, `NoCompression` or `SmallestSize`_|
|**EnableForwardedHeaders**|_boolean_|_Enables response/request headers for proxy forwarding. (data center usage)_|
|**EnableSwagger**|_boolean_|_Enables `Swagger` with `Swagger UI` for the rest services._|
|**MaxPageSize**|_uint32_|_Max page size for searches on `Ledger`/`Contracts` route._|
|**MaxConcurrentConnections**|_int64_|_Max allow concurrent HTTP connections._|
|**MaxInvokeGas**|_int64_|_Max gas to be invoked on the `Neo` virtual machine._|

## Default "Config.json" file
```json
{
  "PluginConfiguration": {
    "Network": 860833102,
    "BindAddress": "127.0.0.1",
    "Port": 10339,
    "KeepAliveTimeout": 120,
    "SslCertFile": "",
    "SslCertPassword": "",
    "TrustedAuthorities": [],
    "EnableBasicAuthentication": false,
    "RestUser": "",
    "RestPass": "",
    "EnableCors": true,
    "AllowOrigins": [],
    "DisableControllers": [ "WalletController" ],
    "EnableCompression": true,
    "CompressionLevel": "SmallestSize",
    "EnableForwardedHeaders": false,
    "EnableSwagger": true,
    "MaxPageSize": 50,
    "MaxConcurrentConnections": 40,
    "MaxTransactionFee": 10000000,
    "MaxInvokeGas": 20000000,
    "WalletSessionTimeout": 120
  }
}
```
