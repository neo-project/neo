# Copyright (C) 2015-2025 The Neo Project.
#
# testcases/basics/gas_rpc_transfer.py file belongs to the neo project and is free
# software distributed under the MIT software license, see the
# accompanying file LICENSE in the main directory of the
# repository or http://www.opensource.org/licenses/mit-license.php
# for more details.
#
# Redistribution and use in source and binary forms with or without
# modifications are permitted.

import base64

from neo import UInt160, CallFlags
from neo.contract import GAS_CONTRACT_HASH, ScriptBuilder
from testcases.testing import Testing


# Operation: this case creates a valid transaction, transfer 0.1 GAS from one account to another.
# and then check GAS balance and the transaction execution result.
# Expect Result: The transaction execution is OK, and GAS transfered as expected.
class GasRpcTransfer(Testing):
    def __init__(self):
        super().__init__()

    def run_test(self):
        # Step 1: wait for creating first block
        block_index = self.wait_next_block(1)
        self.logger.info(f"Current block index: {block_index}")

        # Step 2: Build the transfer script
        u160_0 = UInt160.from_string(self.env.hash160_0)
        u160_d0 = UInt160.from_string(self.env.hash160_d0)
        amount = 1_0000000
        script = ScriptBuilder().emit_dynamic_call(
            script_hash=GAS_CONTRACT_HASH,
            method='transfer',
            call_flags=(CallFlags.STATES | CallFlags.ALLOW_CALL | CallFlags.ALLOW_NOTIFY),
            args=[u160_0, u160_d0, amount, None],  # transfer(from, to, 0.1 GAS, data)
        ).to_bytes()

        # Step 3: get destination account GAS balance
        gas_balance = self.client.get_gas_balance(u160_d0)
        self.logger.info(f"Destination account GAS balance: {gas_balance}")

        # Step 4: create a transaction
        tx = self.make_tx(script, 1_0000000, 1_0000000, block_index+10)  # 0.1 GAS system fee, 0.1 GAS network fee

        # Step 5: send the transaction to the network
        tx_hash = self.client.send_raw_tx(tx.to_array())
        assert isinstance(tx_hash, dict), f"Expected dict, got {tx_hash}"
        assert 'hash' in tx_hash, f"Expected hash in tx_hash, got {tx_hash}"
        tx_id = tx_hash['hash']
        self.logger.info(f"Transaction sent: {tx_id}")

        # Step 6: check the mempool
        mempool = self.client.get_mempool(include_unverified=True)
        self.logger.info(f"Mempool: {mempool}")

        # The tx maybe have been executed, so not assert this.
        # assert tx_id in mempool['verified'], f"Expected tx_id in mempool['verified'], got {mempool}"
        assert tx_id not in mempool['unverified'], f"Expected tx_id not in mempool['unverified'], got {mempool}"

        # Step 7: wait for the next block
        block_index = self.client.get_block_index()
        self.wait_next_block(block_index)

        # Step 8: check the gas balance
        from_balance = self.client.get_gas_balance(self.env.hash160_0)
        # `hash160_0` is committee member, GAS balance changed after block executed.
        # So not check GAS balance of `hash160_0`
        self.logger.info(f"Gas balance of from account: {from_balance}")

        to_balance = self.client.get_gas_balance(self.env.hash160_d0)
        self.logger.info(f"Gas balance of destination account: {to_balance}")
        assert to_balance == gas_balance + amount, f"to_balance:{to_balance} != {gas_balance} + {amount}"

        # Step 19: check the application log
        application_log = self.client.get_application_log(tx_id)
        self.logger.info(f"Application log: {application_log}")
        assert 'txid' in application_log and tx_id == application_log['txid']
        assert 'executions' in application_log and len(application_log['executions']) == 1

        # Check the execution
        execution = application_log['executions'][0]
        assert 'trigger' in execution and execution['trigger'] == 'Application'
        assert 'vmstate' in execution and execution['vmstate'] == 'HALT'
        assert 'exception' in execution and execution['exception'] is None
        assert 'stack' in execution and len(execution['stack']) == 1

        # Check the stack
        stack_item = execution['stack'][0]
        assert 'type' in stack_item and stack_item['type'] == 'Boolean'
        assert 'value' in stack_item and stack_item['value'] == True

        # Check the notifications
        assert 'notifications' in execution and len(execution['notifications']) == 1
        notification = execution['notifications'][0]
        assert 'contract' in notification and notification['contract'] == GAS_CONTRACT_HASH
        assert 'eventname' in notification and notification['eventname'] == 'Transfer'
        assert 'state' in notification

        state = notification['state']
        assert 'type' in state and state['type'] == 'Array'
        assert 'value' in state and len(state['value']) == 3

        # Check the state
        from_address = state['value'][0]
        assert 'type' in from_address and from_address['type'] == 'ByteString'
        assert 'value' in from_address and from_address['value'] == base64.b64encode(u160_0.to_array()).decode('utf-8')

        to_address = state['value'][1]
        assert 'type' in to_address and to_address['type'] == 'ByteString'
        assert 'value' in to_address and to_address['value'] == base64.b64encode(u160_d0.to_array()).decode('utf-8')

        transfered = state['value'][2]
        assert 'type' in transfered and transfered['type'] == 'Integer'
        assert 'value' in transfered
        assert transfered['value'] == str(amount), f"transfered:{transfered['value']} != {amount}"


# Run with: python3 -B -m testcases.basics.gas_rpc_transfer
if __name__ == "__main__":
    test = GasRpcTransfer()
    test.run()
