#!/usr/bin/env python3
"""Independent ABI 2 signing domain vectors. Uses no node assemblies, keys or RPC."""
import hashlib
import json
from pathlib import Path
import sys


def vectors():
    prefix = b"NeoSmartAccount/UserOperation\x02" + (0x12345678).to_bytes(4, "little")
    prefix += bytes.fromhex("4117a67f088e2ea046e74bdce906f2ad071d42d9ac2000a06cb36878e05328fe553c56080033253e")
    unsigned = bytes.fromhex("400628144117a67f088e2ea046e74bdce906f2ad071d42d928087472616e7366657240022814ac2000a06cb36878e05328fe553c56080033253e21012a210021060068e5cf8b012800")
    cases = []
    for epoch, nonce in [(0, 0), (0, 1), (1, 1), (7, 11), ((1 << 64) - 1, (1 << 64) - 1)]:
        domain = prefix + epoch.to_bytes(8, "little") + nonce.to_bytes(8, "little")
        cases.append({"authorityEpoch": str(epoch), "configurationNonce": str(nonce),
                      "domain": domain.hex(), "digest": hashlib.sha256(domain + unsigned).hexdigest()})
    return {"schema": "smartaccount-authorization-v2-vectors", "identityVersion": 1,
            "authorizationVersion": 2, "unsignedOperation": unsigned.hex(), "vectors": cases}


if __name__ == "__main__":
    destination = Path(__file__).with_name("smartaccount-authorization-v2.json")
    output = json.dumps(vectors(), indent=2) + "\n"
    if sys.argv[1:] == ["--check"]:
        assert destination.read_text() == output, "Authorization fixture drift"
    else:
        assert not sys.argv[1:], "Only --check is supported"
        destination.write_text(output)
