# Copyright (C) 2015-2025 The Neo Project.
#
# env.py file belongs to the neo project and is free
# software distributed under the MIT software license, see the
# accompanying file LICENSE in the main directory of the
# repository or http://www.opensource.org/licenses/mit-license.php
# for more details.
#
# Redistribution and use in source and binary forms with or without
# modifications are permitted.

from dataclasses import dataclass, field


# Hardfork config, all hardforks are enabled in default.
@dataclass
class Hardfork:
    HF_Aspidochelone: int = 1
    HF_Basilisk: int = 1
    HF_Cockatrice: int = 1
    HF_Domovoi: int = 1
    HF_Echidna: int = 1


# It contains the environment variables for the tests.
# If run the tests on the different environment, the default values should be overridden.
# For testing, the RpcServer, DBFT and ApplicationLog plugins must be installed.
@dataclass
class Env:
    # The RpcServer plugin endpoint, the default value is localnet endpoint
    rpc_endpoint: str = "127.0.0.1:10332"

    # The default value is localnet testing network id
    network: int = 1234567890

    # The hardforks, the default value from localnet testing network
    hardforks: Hardfork = field(default_factory=Hardfork)

    # The number of validators, the default value is 7
    validators_count: int = 7

    # The address of the committee member 0. This account must have enough GAS for the tests.
    address0: str = "NSL83LVKbvCpg5gjWC9bfsmERN5kRJSs9d"
    hash160_0: str = "0x90ed8a9ebb0f335f97dac89678485bb722216246"  # Hash160 of address0
    private_key0: str = "0x18dfcb60a696d083d6046f1ac9f099cc49e110b369f41549339dabde765e769b"  # Private key of address0
    public_key0: str = "0285265dc8859d05e1e42a90d6c29a9de15531eac182489743e6a947817d2a9f66"  # Public key of address0

    # The address for transfer destination
    address_d0: str = "NUz6PKTAM7NbPJzkKJFNay3VckQtcDkgWo"
    hash160_d0: str = "0x902e0d38da5e513b6d07c1c55b85e77d3dce8063"  # Hash160 of address_d0
    private_key_d0: str = "0x0101010101010101010101010101010101010101010101010101010101010101"  # Private key of address_d0
    public_key_d0: str = "026ff03b949241ce1dadd43519e6960e0a85b41a69a05c328103aa2bce1594ca16"  # Public key of address_d0
