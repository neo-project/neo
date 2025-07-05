# Secure Sign Plugin

## Purpose

The Secure Sign Plugin (SignClient) is a Neo blockchain plugin that provides secure ExtensiblePayload and Block signing capabilities through a gRPC-based sign service. This plugin enables:

- **Secure Key Management**: Private keys are stored and managed by a separate sign service, not within the Neo node itself. The private keys should be protected by some mechanisms(like Intel SGX or AWS Nitro Enclave)
- **Multi Transport Layers Support**: Supports both TCP and Vsock connections for different deployment scenarios

## How to enable plugin `SignClient`

Users can enable plugin `SignClient` by installing it or compiling it manually.

### Install by `neo-cli`

1. **Start the Signing Service**: Ensure, your sign service is running and accessible. You can select a sign service implementation or implement a sign service on your own.
2. **Download the Plugin**: The SignClient plugin should be installed. You can run `neo-cli` then execute `help install` to get help abort how to install plugin.
3. **Configure the Plugin**: Create or modify the `SignClient.json` configuration file in the `neo-cli` binary directory (_`Plugins/SignClient`_).
4. **Start `neo-cli`**: Start/Restart `neo-cli` if needed.

### Compile Manually

The .Net SDK needs to be installed before compiling it.

1. **Clone the Repository**: 
   ```bash
   git clone https://github.com/neo-project/neo
   cd neo
   donet build
   ```

2. **Copy to `neo-cli` folder**: Copy the built plugin to the `neo-cli` binary directory.
- Step 0. Find the `.dll` files. For example:
  - The `neo-cli` compile products should exist in `./bin/Neo.CLI/net{dotnet-version}/`(i.e. `neo-cli` binary directory, ./bin/Neo.CLI/net9.0/).
  - The plugin `SignClient` should exist in `./bin/Neo.Plugins.SignClient/{dotnet-version}/`(i.e. `SignClient` binary directory, ./bin/Neo.Network.RpcClient/9.0/).
- Step 1. Copy files `Google.Protobuf.dll Grpc.Core.Api.dll Grpc.Net.Client.dll Grpc.Net.Common.dll `(These files should exist in folder `Neo.Plugins.SignClient`) to the `neo-cli` binary directory.
- Step 2. `mkdir -p Plugins/SignClient` in the `neo-cli` binary directory. Then copy file `SignClient.dll` from the plugin `SignClient` binary directory to `Plugins/SignClient`.
- Step 3. Create a `SignClient.json` file `Plugins/SignClient` directory according to the next section.
- Step 4. Start the `neo-cli`.


## Configuration

### Basic Configuration

Create a `SignClient.json` file in `Plugins/SignClient` directory:

```json
{
  "PluginConfiguration": {
    "Name": "SignClient",
    "Endpoint": "http://127.0.0.1:9991"
  }
}
```

### Configuration Parameters

- **Name**: The name of the sign client (default: "SignClient")
- **Endpoint**: The endpoint of the sign service
  - TCP: `http://host:port` or `https://host:port`
  - VSock: `vsock://contextId:port` (Linux only, for AWS Nitro Enclaves)

### Connection Types

#### TCP Connection
```json
{
  "PluginConfiguration": {
    "Name": "SignClient",
    "Endpoint": "http://127.0.0.1:9991"
  }
}
```

#### VSock Connection (AWS Nitro Enclaves)
```json
{
  "PluginConfiguration": {
    "Name": "SignClient",
    "Endpoint": "vsock://2345:9991"
  }
}
```

## Sign Service Implementation Guide

The SignClient plugin communicates with a sign service using gRPC.
The service must implement the following interface defined in `proto/servicepb.proto`:

### Service Interface

```protobuf
service SecureSign {
    rpc SignExtensiblePayload(SignExtensiblePayloadRequest) returns (SignExtensiblePayloadResponse) {}
    rpc SignBlock(SignBlockRequest) returns (SignBlockResponse) {}
    rpc GetAccountStatus(GetAccountStatusRequest) returns (GetAccountStatusResponse) {}
}
```

### Methods

#### SignExtensiblePayload

Signs extensible payloads for the specified script hashes.

**Request**:
- `payload`: The extensible payload to sign
- `script_hashes`: List of script hashes (UInt160) that need signatures
- `network`: Network ID

**Response**:
- `signs`: List of account signs corresponding to each script hash

**Implementation Notes**:
- The service should check if it has private keys for the requested script hashes.
- For multi-signature accounts, return all available signatures.
- Return appropriate account status for each script hash.
- If a feature not support(for example, multi-signature account), it should return grpc error code `Unimplemented`.
- If the `payload` or `script_hashes` is not provided or invalid, it should return grpc error code `InvalidArgument`.

#### SignBlock

Signs a block with the specified public key.

**Request**:
- `block`: The block header and transaction hashes
- `public_key`: The public key to sign with (compressed or uncompressed)
- `network`: Network ID

**Response**:
- `signature`: The signature bytes

**Implementation Notes**:
- The service should verify it has the private key corresponding to the public key.
- Sign the block header data according to Neo's block signing specification.
- If the `block` or `public_key` is not provided or invalid, it should return grpc error code `InvalidArgument`.

#### GetAccountStatus

Retrieves the status of an account for the specified public key.

**Request**:
- `public_key`: The public key to check (compressed or uncompressed)

**Response**:
- `status`: Account status enum value

**Implementation Notes**:
- If the `public_key` is not provided or invalid, it should return grpc error code `InvalidArgument`.

**Account Status Values**:
- `NoSuchAccount`: Account doesn't exist
- `NoPrivateKey`: Account exists but no private key available
- `Single`: Single-signature account with private key available
- `Multiple`: Multi-signature account with private key available
- `Locked`: Account is locked and cannot sign

## Usage Examples

### Console Commands

The plugin provides a console command to check account status:

```bash
get account status <hexPublicKey>
```

Example:
```bash
get account status 026ff03b949241ce1dadd43519e6960e0a85b41a69a05c328103aa2bce1594ca16
```

## Troubleshooting

### Common Issues

1. **"No signer service is connected"**
   - Check if the sign service is running
   - Verify the endpoint configuration
   - Check network connectivity

2. **"Invalid vsock endpoint"**
   - Ensure VSock is only used on Linux
   - Verify the VSock address format: `vsock://contextId:port`

3. **"Failed to get account status"**
   - Check if the public key format is correct
   - Verify the sign service has the requested account
