#!/usr/bin/env python3
"""Independent, dependency-free storage and VM-initializer vectors. No keys or RPC."""
import copy
import hashlib
import json
from pathlib import Path
import sys


def hash160(value):
    return hashlib.new("ripemd160", hashlib.sha256(value).digest()).digest()


def integer_bytes(value):
    if value == 0:
        return b""
    width = (value.bit_length() + 8) // 8
    return value.to_bytes(width, "little", signed=True)


def serialize(value):
    if value is None:
        return b"\x00"
    if isinstance(value, int):
        data = integer_bytes(value)
        return b"\x21" + bytes([len(data)]) + data
    if isinstance(value, bytes):
        assert len(value) < 253
        return b"\x28" + bytes([len(value)]) + value
    assert isinstance(value, list) and len(value) < 253
    return b"\x40" + bytes([len(value)]) + b"".join(map(serialize, value))


def initialize(value):
    if value is None:
        return b"\x0b"
    if isinstance(value, int):
        if -1 <= value <= 16:
            return bytes([0x10 + value])
        for opcode, width in enumerate((1, 2, 4, 8, 16, 32)):
            if -(1 << (8 * width - 1)) <= value < (1 << (8 * width - 1)):
                return bytes([opcode]) + value.to_bytes(width, "little", signed=True)
        raise ValueError("VM Integer overflow")
    if isinstance(value, bytes):
        assert len(value) <= 255
        return b"\x0c" + bytes([len(value)]) + value
    assert isinstance(value, list)
    return b"\xc2" if not value else b"".join(initialize(v) for v in reversed(value)) + initialize(len(value)) + b"\xc0"


def vectors():
    address = lambda n: bytes([n]) * 20
    binding = lambda n: [address(n), bytes([n]) * 32]
    service = hash160(b"\x38" + initialize(address(0)) + initialize(0) + initialize(b"AccountManagement"))
    assert service[::-1].hex() == "d9421d07adf206e9dc4be746a02e8e087fa61741"
    account = hash160(b"NeoSmartAccount\x01" + (123).to_bytes(4, "little") + service + address(1) + bytes(32))
    proxy = hash160(initialize(account) + b"\x11\xc0\x15" + initialize(b"verify") + initialize(service) + bytes.fromhex("41627d5b52"))
    fresh = [2, account, proxy, address(1), address(2), binding(3), binding(4), 0, 0, None, None, None, None, 0]
    pending = copy.deepcopy(fresh)
    pending[9:13] = [[*binding(5), 10, 86400010, 0], [address(0), bytes(32), 10, 86400010, 0],
                   [address(6), 10, 86400010, 0], [address(7), 10, 604800010, 0]]
    exhausted = copy.deepcopy(fresh)
    exhausted[8] = (1 << 64) - 1
    fallback = copy.deepcopy(fresh)
    fallback[4], fallback[5], fallback[8] = address(0), None, 1
    return {"schema": "smartaccount-state-v2-vectors", "networkMagic": 123,
            "accountId": "0x" + account[::-1].hex(), "accountAddress": "0x" + proxy[::-1].hex(),
            "vectors": [{"name": name, "serializedState": serialize(state).hex(), "initializer": initialize(state).hex()}
                        for name, state in zip(("fresh", "all-intents", "exhausted-epoch", "native-witness-fallback"),
                                               (fresh, pending, exhausted, fallback))]}


if __name__ == "__main__":
    destination = Path(__file__).with_name("smartaccount-state-v2.json")
    output = json.dumps(vectors(), indent=2) + "\n"
    if sys.argv[1:] == ["--check"]:
        assert destination.read_text() == output, "State fixture drift"
    else:
        assert not sys.argv[1:], "Only --check is supported"
        destination.write_text(output)
