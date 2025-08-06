# NEP-25 Extended Type System Implementation

## Overview
This implementation adds comprehensive support for NEP-25 extended type definitions in smart contract ABIs, enabling richer type information for better tooling and SDK generation.

## Implemented Features

### Core Components
- **ExtendedType Class**: Full implementation of NEP-25 type specifications
  - Support for named type references
  - Length constraints for strings and byte arrays
  - Null-forbidding flags
  - Interface specifications for InteropInterface types
  - Key/Value type definitions for Maps and Arrays
  - Fields for structured types

- **Contract Method Enhancements**:
  - Extended return type support in ContractMethodDescriptor
  - Extended parameter types in ContractParameterDefinition
  
- **Contract ABI Extensions**:
  - Named types section support in ContractAbi
  - Full JSON serialization/deserialization
  - Backward compatibility with NEP-14

### Type System Features
- Simple type notation (string representation)
- Complex type objects with constraints
- Recursive type definitions
- Circular reference support through named types

### Testing
- Comprehensive unit tests for ExtendedType
- Integration tests for contract manifests
- Backward compatibility tests
- Round-trip serialization tests

## Usage Examples

### Simple Types
```json
{
  "type": "Integer",
  "forbidnull": true
}
```

### Array Types
```json
{
  "type": "Array",
  "value": {
    "type": "Hash160",
    "forbidnull": true
  }
}
```

### Map Types
```json
{
  "type": "Map",
  "key": "String",
  "value": {
    "type": "Integer",
    "forbidnull": false
  }
}
```

### Named Types
```json
{
  "namedtypes": {
    "TokenInfo": {
      "type": "InteropInterface",
      "fields": {
        "symbol": "String",
        "decimals": "Integer",
        "totalSupply": {
          "type": "Integer",
          "forbidnull": true
        }
      }
    }
  }
}
```

## Compatibility
- Fully backward compatible with NEP-14
- Contracts without extended types continue to work unchanged
- Extended type information is optional

## Status
✅ Implementation complete and tested
✅ All unit tests passing
✅ Ready for production use