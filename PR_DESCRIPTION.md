# Improve Exception Messages Throughout Neo Codebase

## Summary

This PR standardizes and improves exception messages across the entire Neo codebase to provide better developer experience, clearer error reporting, and more professional error handling.

## Changes Made

### **Exception Message Standardization**

- **Consistency**: Established uniform message formats for similar validation types
- **Professionalism**: Removed informal language and improved capitalization
- **Clarity**: Made messages more descriptive while maintaining conciseness
- **Actionability**: Enhanced messages to provide better debugging context

### **Patterns Applied**

1. **Size/Length Validation**: `"{type} size {actual} exceeds maximum allowed size of {max}"`
2. **Type Validation**: `"{type} not supported"` or `"{type} mismatch"`
3. **Parameter Validation**: `"Parameter {name} must be {requirement}"`
4. **Array Validation**: `"Arrays must have the same length"`
5. **Field Validation**: `"{field} must be {requirement}"`
6. **Operation Validation**: `"Operation failed: {reason}"`

### **Files Updated (61 files total)**

#### **Core Cryptography (8 files)**
- `G1Projective.cs`, `G2Projective.cs`, `Fp.cs`, `Scalar.cs`
- `Crypto.cs` - Signature validation messages
- `CryptoLib.BLS12_381.cs` - BLS12-381 operation messages
- `BloomFilter.cs`, `Helper.cs`

#### **Smart Contract System (12 files)**
- `ApplicationEngine.Crypto.cs` - Crypto validation
- `ApplicationEngine.Storage.cs` - Storage operation validation
- `ApplicationEngine.Contract.cs` - Contract method validation
- `ApplicationEngine.Runtime.cs` - Runtime operation validation
- `ContractParameter.cs` - Parameter type validation
- `NefFile.cs` - Script validation
- Native contracts: `ContractManagement.cs`, `OracleContract.cs`, `NeoToken.cs`, `PolicyContract.cs`, `RoleManagement.cs`
- `ContractMethodAttribute.cs`, `ContractMethodMetadata.cs`

#### **MPT Trie System (5 files)**
- `Trie.Put.cs`, `Trie.Get.cs`, `Trie.Delete.cs`, `Trie.Proof.cs`, `Trie.Find.cs`

#### **VM System (3 files)**
- `Script.cs` - Script execution validation
- `Integer.cs` - Integer size validation
- `Map.cs` - Map key size validation

#### **Extensions (5 files)**
- `RandomExtensions.cs` - Random operation validation
- `BinaryWriterExtensions.cs` - String length validation
- `ScriptBuilderExtensions.cs` - Parameter type validation
- `ContractParameterExtensions.cs` - Parameter conversion validation
- `StackItemExtensions.cs` - Stack item conversion validation

#### **Wallets (4 files)**
- `Helper.cs` - Wallet operation validation
- `KeyPair.cs` - Key validation
- `NEP6Wallet.cs` - Wallet validation
- `AssetDescriptor.cs` - Asset validation

#### **Network & CLI (6 files)**
- `Message.cs` - Network message validation
- `MainService.cs` - CLI operation validation
- `ConsoleServiceBase.cs` - Console operation validation
- `CommandTokenizer.cs` - Command parsing validation
- `Helper.cs`, `MainService.Block.cs`, `MainService.Plugins.cs`

#### **Utilities (8 files)**
- `BigDecimal.cs` - Decimal precision validation
- `UInt160.cs`, `UInt256.cs` - Buffer size validation
- `ECPoint.cs` - Point validation
- `SignerManager.cs` - Signer validation
- `Base58.cs`, `Ed25519.cs`, `ECFieldElement.cs`, `UPnP.cs`

#### **Manifest System (3 files)**
- `ContractManifest.cs` - Manifest validation
- `ContractPermission.cs` - Permission validation
- `ContractPermissionDescriptor.cs` - Descriptor validation

## Examples of Improvements

### **Before vs After**

#### **Size Validation**
- **Before**: `"The url bytes size({urlSize}) cannot be greater than {MaxUrlLength}."`
- **After**: `"URL size {urlSize} bytes exceeds maximum allowed size of {MaxUrlLength} bytes."`

#### **Type Validation**
- **Before**: `"Bls12381 operation fault, type:format, error:type mismatch"`
- **After**: `"BLS12-381 type mismatch"`

#### **Parameter Validation**
- **Before**: `"The length of the parameter \`{nameof(by)}\` must be {SizeL}."`
- **After**: `"Parameter {nameof(by)} must have length {SizeL}."`

#### **Array Validation**
- **Before**: `"The lengths of the two arrays must be the same."`
- **After**: `"Arrays must have the same length."`

## Benefits

1. **Improved Developer Experience**: Clearer error messages help developers understand and fix issues faster
2. **Consistency**: Uniform message formats across the codebase
3. **Professionalism**: More formal and professional language
4. **Maintainability**: Easier to maintain and extend exception handling
5. **Debugging**: Better context for troubleshooting issues

## Testing

- All existing functionality remains unchanged
- Exception messages now provide better debugging information
- No breaking changes to public APIs
- Maintains backward compatibility

## Documentation

Added comprehensive documentation in `docs/CLI_EXCEPTION_IMPROVEMENTS.md` detailing the improvements made to CLI exception messages.

## Checklist

- [x] Exception messages are consistent across similar validation types
- [x] Messages are professional and clear
- [x] No oversimplified or overly verbose messages
- [x] Technical accuracy maintained
- [x] No breaking changes introduced
- [x] Documentation updated
- [x] Code follows existing style guidelines
