# Copyright (C) 2015-2025 The Neo Project.
#
# testcases/testing.py file belongs to the neo project and is free
# software distributed under the MIT software license, see the
# accompanying file LICENSE in the main directory of the
# repository or http://www.opensource.org/licenses/mit-license.php
# for more details.
#
# Redistribution and use in source and binary forms with or without
# modifications are permitted.

import hashlib
import random
import logging
import time

from cryptography.hazmat.primitives import hashes
from cryptography.hazmat.primitives.asymmetric import ec
from cryptography.hazmat.primitives.asymmetric.utils import decode_dss_signature
from cryptography.hazmat.backends import default_backend

from neo import *
from neo.contract import ScriptBuilder
from neo.rpc import RpcClient
from env import Env

logging.basicConfig(level=logging.INFO)

TX_VERSION_V0 = 0


class Testing:

    def __init__(self):
        self.env = Env()
        self.client = RpcClient(self.env.rpc_endpoint)
        self.logger = logging.getLogger(__name__)

    def wait_next_block(self, current_block_index: int, wait_while: str = '', max_wait_seconds: int = 5*60):
        start_time = time.time()
        while True:
            block_index = self.client.get_block_index()
            if block_index > current_block_index:
                break
            if time.time() - start_time > max_wait_seconds:
                raise TimeoutError(f"Timeout waiting for next block of {current_block_index} after {max_wait_seconds}s")
            time.sleep(2)

            elapsed = time.time() - start_time
            self.logger.info(f"Waiting {elapsed:.2f}s for next block of {current_block_index} while {wait_while}")

        elapsed = time.time() - start_time
        self.logger.info(f"Waited {elapsed:.2f}s for next block of {current_block_index} while {wait_while}")
        return block_index

    def sign(self, private_key: str, data: bytes) -> bytes:
        sha256 = hashlib.sha256(data).digest()
        sign_data = self.env.network.to_bytes(4, 'little') + sha256

        sk = ec.derive_private_key(int(private_key, 16), ec.SECP256R1(), default_backend())
        der = sk.sign(sign_data, ec.ECDSA(hashes.SHA256()))
        (r, s) = decode_dss_signature(der)
        return r.to_bytes(32, 'big') + s.to_bytes(32, 'big')

    def make_witness(self, sign: bytes, compressed_public_key: bytes) -> Witness:
        invocation = ScriptBuilder().emit_push_bytes(sign).to_bytes()

        verification = ScriptBuilder().emit_push_bytes(compressed_public_key) \
            .emit_syscall("System.Crypto.CheckSig") \
            .to_bytes()

        return Witness(invocation_script=invocation, verification_script=verification)

    def make_tx(self, script: bytes, system_fee: int, network_fee: int, valid_until_block: int):
        account0 = UInt160.from_string(self.env.hash160_0)
        tx = Transaction(
            version=TX_VERSION_V0,
            nonce=random.randint(0, 0xFFFFFFFF),
            system_fee=system_fee,
            network_fee=network_fee,
            valid_until_block=valid_until_block,
            signers=[Signer(account=account0, scope=WitnessScope.CALLED_BY_ENTRY)],
            attributes=[],
            script=script,
            witnesses=[],
            protocol_magic=self.env.network,
        )

        with BinaryWriter() as writer:
            tx.serialize_unsigned(writer)
            sign = self.sign(self.env.private_key0,  writer.to_array())

        tx.witnesses = [self.make_witness(sign, bytes.fromhex(self.env.public_key0))]
        return tx

    def run(self):
        self.pre_test()
        try:
            self.run_test()
        finally:
            self.post_test()

    def pre_test(self):
        pass

    def run_test(self):
        pass

    def post_test(self):
        pass
