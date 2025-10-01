# Copyright (C) 2015-2025 The Neo Project.
#
# neo/contract.py file belongs to the neo project and is free
# software distributed under the MIT software license, see the
# accompanying file LICENSE in the main directory of the
# repository or http://www.opensource.org/licenses/mit-license.php
# for more details.
#
# Redistribution and use in source and binary forms with or without
# modifications are permitted.

import hashlib
from typing import Self
from dataclasses import asdict, dataclass

from neo3.core.types import UInt160
from neo3.contracts.callflags import CallFlags

NEO_CONTRACT_HASH = "0xef4073a0f2b305a38ec4050e4d3d28bc40ea63f5"
GAS_CONTRACT_HASH = "0xd2a4cff31913016155e38e474a2c06d08be276cf"


@dataclass
class ContractParameter:
    type: str
    value: any

    def to_dict(self):
        return asdict(self)


def syscall_code(syscall_name: str) -> int:
    # first 4 bytes of the sha256 hash of the syscall name
    return int.from_bytes(hashlib.sha256(syscall_name.encode('utf-8')).digest()[:4], 'little')


class ScriptBuilder:
    def __init__(self):
        self._script = bytearray()

    def emit(self, opcode: int, operand: bytes = b'') -> Self:
        if opcode < 0 or opcode > 0xFF:
            raise ValueError(f"Invalid opcode: {opcode}")

        self._script.append(opcode & 0xFF)
        if len(operand) > 0:
            self._script.extend(operand)
        return self

    def emit_push_int(self, item: int) -> Self:
        if item >= -1 and item <= 16:
            self.emit(0x10 + item)  # PUSHM1, PUSH0, ..., PUSH16
        elif item >= -0x80 and item <= 0x7F:
            self.emit(0x00)  # PUSHINT8
            self._script.extend(item.to_bytes(1, 'little', signed=True))
        elif item >= -0x8000 and item <= 0x7FFF:
            self.emit(0x01)  # PUSHINT16
            self._script.extend(item.to_bytes(2, 'little', signed=True))
        elif item >= -0x80000000 and item <= 0x7FFFFFFF:
            self.emit(0x02)  # PUSHINT32
            self._script.extend(item.to_bytes(4, 'little', signed=True))
        elif item >= -0x8000000000000000 and item <= 0x7FFFFFFFFFFFFFFF:
            self.emit(0x03)  # PUSHINT64
            self._script.extend(item.to_bytes(8, 'little', signed=True))
        elif item >= -0x80000000000000000000000000000000 and item <= 0x7FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF:
            self.emit(0x04)  # PUSHINT128
            self._script.extend(item.to_bytes(16, 'little', signed=True))
        elif item >= -0x8000000000000000000000000000000000000000000000000000000000000000 and \
                item <= 0x7FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF:
            self.emit(0x05)  # PUSHINT256
            self._script.extend(item.to_bytes(32, 'little', signed=True))
        else:
            raise ValueError(f"Item too large: {item}")
        return self

    def emit_push_bytes(self, item: bytes | bytearray) -> Self:
        if len(item) < 0x100:
            self.emit(0x0C)  # PUSHDATA1
            self._script.extend(len(item).to_bytes(1, 'little'))
        elif len(item) < 0x10000:
            self.emit(0x0D)  # PUSHDATA2
            self._script.extend(len(item).to_bytes(2, 'little'))
        elif len(item) < 0x100000000:
            self.emit(0x0E)  # PUSHDATA4
            self._script.extend(len(item).to_bytes(4, 'little'))
        else:
            raise ValueError(f"Item too large: {len(item)}")
        self._script.extend(item)
        return self

    def emit_push(self, item: any) -> Self:
        if item is None:
            self.emit(0x0B)  # PUSHNULL
        elif isinstance(item, bool):
            self.emit(0x08 if item else 0x09)  # PUSHT, PUSHF
        elif isinstance(item, bytes) or isinstance(item, bytearray):
            self.emit_push_bytes(item)
        elif isinstance(item, int):
            self.emit_push_int(item)
        elif isinstance(item, str):
            self.emit_push_bytes(item.encode('utf-8'))
        elif isinstance(item, list) or isinstance(item, tuple):
            self.emit_push_array(item)
        elif hasattr(item, 'to_array'):
            item = item.to_array()
            if not isinstance(item, bytes):
                raise ValueError(f"Unsupported item type: {type(item)}")
            self.emit_push_bytes(item)
        else:  # TODO: ContractParameter
            raise ValueError(f"Unsupported item type: {type(item)}")
        return self

    def emit_push_array(self, array: list | tuple) -> Self:
        if len(array) == 0:
            self.emit(0xC2)  # NEWARRAY0
            return self

        for item in reversed(array):  # in reverse order
            self.emit_push(item)
        self.emit_push_int(len(array))
        self.emit(0xC0)  # PACK
        return self

    def emit_dynamic_call(
            self, script_hash: str | UInt160, method: str, call_flags: int | CallFlags, args: list | tuple = []) -> Self:
        self.emit_push_array(args)
        self.emit_push_int(call_flags if isinstance(call_flags, int) else call_flags.value)
        self.emit_push(method)
        self.emit_push(UInt160.from_string(script_hash) if isinstance(script_hash, str) else script_hash)
        self.emit_syscall('System.Contract.Call')
        return self

    def emit_syscall(self, syscall: int | str, args: list | tuple = []) -> Self:
        for item in reversed(args):  # in reverse order
            self.emit_push(item)
        self.emit(0x41)  # SYSCALL

        code = syscall_code(syscall) if isinstance(syscall, str) else syscall
        self._script.extend(code.to_bytes(4, 'little'))
        return self

    def to_bytes(self):
        return bytes(self._script)
