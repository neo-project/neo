## Neo Core Events

### 1. Block Committing Event

**Event Name** `Committing`

**Handler Interface:** `ICommittingHandler`

This event is triggered when a transaction is in the process of being committed to the blockchain. Implementing the `ICommittingHandler` interface allows you to define custom actions that should be executed during this phase.

### 2. Block Committed Event

**Event Name** `Committed`

**Handler Interface:** `ICommittedHandler`

This event occurs after a transaction has been successfully committed to the blockchain. By implementing the `ICommittedHandler` interface, you can specify custom actions to be performed after the transaction is committed.

### 3. Logging Event
**Event Name** `Logging`

**Handler Interface:** `ILoggingHandler`

This event is related to logging within the blockchain system. Implement the `ILoggingHandler` interface to define custom logging behaviors for different events and actions that occur in the blockchain.

### 4. General Log Event
**Event Name** `Log`

**Handler Interface:** `ILogHandler`

This event pertains to general logging activities. The `ILogHandler` interface allows you to handle logging for specific actions or errors within the blockchain system.

### 5. Notification Event
**Event Name** `Notify`

**Handler Interface:** `INotifyHandler`

This event is triggered when a notification needs to be sent. By implementing the `INotifyHandler` interface, you can specify custom actions for sending notifications when certain events occur within the blockchain system.

### 6. Service Added Event
**Event Name** `ServiceAdded`

**Handler Interface:** `IServiceAddedHandler`

This event occurs when a new service is added to the blockchain system. Implement the `IServiceAddedHandler` interface to define custom actions that should be executed when a new service is added.

### 7. Transaction Added Event
**Event Name** `TransactionAdded`

**Handler Interface:** `ITransactionAddedHandler`

This event is triggered when a new transaction is added to the blockchain system. By implementing the `ITransactionAddedHandler` interface, you can specify custom actions to be performed when a new transaction is added.

### 8. Transaction Removed Event
**Event Name** `TransactionRemoved`

**Handler Interface:** `ITransactionRemovedHandler`

This event occurs when a transaction is removed from the blockchain system. Implement the `ITransactionRemovedHandler` interface to define custom actions that should be taken when a transaction is removed.

### 9. Wallet Changed Event
**Event Name** `WalletChanged`

**Handler Interface:** `IWalletChangedHandler`

This event is triggered when changes occur in the wallet, such as balance updates or new transactions. By implementing the `IWalletChangedHandler` interface, you can specify custom actions to be taken when there are changes in the wallet.

### 10. Remote Node MessageReceived Event
**Event Name** `MessageReceived`

**Handler Interface:** `IMessageReceivedHandler`

This event is triggered when a new message is received from a peer remote node.
