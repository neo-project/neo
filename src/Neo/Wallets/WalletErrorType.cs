// Copyright (C) 2015-2024 The Neo Project.
//
// WalletErrorType.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Wallets;

public enum WalletErrorType
{
    OpenWalletError,
    CreateWalletError,
    MigrateAccountError,
    LoadWalletError,
    CreateAccountError,
    InvalidPrivateKey,
    ChangePasswordError,
    InsufficientFunds,
    InvalidTransaction,
    AccountLocked,
    TransactionCreationError,
    ExecutionFault,
    FormatError,
    InvalidOperation,
    FileAlreadyExists,
    ArgumentException,
    ContractNotFound,
    ContractError,
    VerificationFailed,
    UnsupportedOperation,
    PasswordIncorrect,
    UnsupportedWalletFormat,
    ArgumentNull,
    UnknownError
}
