# Copyright (C) 2015-2025 The Neo Project.
#
# neo/rpc.py file belongs to the neo project and is free
# software distributed under the MIT software license, see the
# accompanying file LICENSE in the main directory of the
# repository or http://www.opensource.org/licenses/mit-license.php
# for more details.
#
# Redistribution and use in source and binary forms with or without
# modifications are permitted.

import requests
import base64

from neo import UInt160
from neo.contract import ContractParameter, GAS_CONTRACT_HASH, NEO_CONTRACT_HASH


class RpcError(Exception):
    def __init__(self, code: int, message: str):
        self.code = code
        self.message = message


class RpcClient:
    def __init__(self, endpoint: str):
        self._endpoint = endpoint
        self._id = 0

    def _send(self, method: str, params: list):
        self._id += 1
        req = {
            "jsonrpc": "2.0",
            "id": self._id,
            "method": method,
            "params": params
        }

        endpoint = self._endpoint
        if not endpoint.startswith("http") and not endpoint.startswith("https"):
            endpoint = f"http://{endpoint}"

        rsp = requests.post(endpoint, json=req).json()
        if 'error' in rsp:
            raise RpcError(rsp['error']['code'], rsp['error']['message'])
        return rsp['result'] if 'result' in rsp else None

    def get_block(self, block_hash_or_index: str | int, verbose: bool = False) -> dict:
        return self._send("getblock", [block_hash_or_index, verbose])

    def get_block_count(self) -> int:
        return self._send("getblockcount", [])

    def get_block_index(self) -> int:
        return self._send("getblockcount", []) - 1

    def get_block_hash(self, index: int) -> str:
        return self._send("getblockhash", [index])

    def get_block_header(self, block_hash_or_index: str | int, verbose: bool = False) -> dict:
        return self._send("getblockheader", [block_hash_or_index, verbose])

    def get_committee(self) -> list:
        return self._send("getcommittee", [])

    def get_wallet_balance(self, address: str | UInt160) -> dict:
        address = str(address) if isinstance(address, UInt160) else address
        return self._send("getwalletbalance", [address])

    def get_neo_balance(self, account: str | UInt160) -> int:
        account = str(account) if isinstance(account, UInt160) else account
        result = self.invoke_function(NEO_CONTRACT_HASH, "balanceOf", [
                                      ContractParameter(type="Hash160", value=account)])
        return int(result['stack'][0]['value'])

    def get_gas_balance(self, account: str | UInt160) -> int:
        account = str(account) if isinstance(account, UInt160) else account
        result = self.invoke_function(GAS_CONTRACT_HASH, "balanceOf",
                                      [ContractParameter(type="Hash160", value=account)])
        return int(result['stack'][0]['value'])

    def invoke_function(self, script_hash: str | UInt160, method: str, args: list[ContractParameter] = []) -> any:
        script_hash = str(script_hash) if isinstance(script_hash, UInt160) else script_hash
        return self._send("invokefunction", [script_hash, method, [arg.to_dict() for arg in args]])

    def send_raw_tx(self, raw_tx: bytes):
        return self._send("sendrawtransaction", [base64.b64encode(raw_tx).decode('utf-8')])

    def get_mempool(self, include_unverified: bool = False) -> dict:
        return self._send("getrawmempool", [include_unverified])

    def get_application_log(self, tx_hash: str, trigger_type: str = "") -> dict:
        """
        From Plugin ApplicationLogs.
        """
        return self._send("getapplicationlog", [tx_hash, trigger_type])
