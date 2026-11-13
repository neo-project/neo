#!/usr/bin/env python3
"""Independent ABI 2 signing domain vectors. Uses no node assemblies, keys or RPC."""
import hashlib
import json
from pathlib import Path
import sys


def vectors():
    prefix = b"NeoSmartAccount/UserOperation\x02" + (0x12345678).to_bytes(4, "little")
    prefix += bytes.fromhex("8480150c393799ced53e53c5fa7e7d22f0c38d39b7b422d102c3f42832c94c0a40e1132eb5f46821")
    unsigned = bytes.fromhex("400628148480150c393799ced53e53c5fa7e7d22f0c38d3928087472616e7366657240022814b7b422d102c3f42832c94c0a40e1132eb5f4682121012a210021060068e5cf8b012800")
    cases = []
    for epoch, nonce in [(0, 0), (1, 0), (0, 1), (1, 1), (7, 11), ((1 << 64) - 1, (1 << 64) - 1)]:
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
