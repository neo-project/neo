// Copyright (C) 2015-2024 The Neo Project.
//
// WalletException.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.IO;
using System.Runtime.CompilerServices;

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

public class WalletException : Exception
{
    public WalletErrorType ErrorType { get; }
    public string CustomErrorMessage { get; }

    public WalletException(WalletErrorType errorType, string customErrorMessage = null)
        : base($"{errorType}: {customErrorMessage ?? GetDefaultMessage(errorType)}")
    {
        ErrorType = errorType;
        CustomErrorMessage = customErrorMessage;
    }

    public WalletException(WalletErrorType errorType, string customErrorMessage, Exception innerException)
        : base($"{errorType}: {customErrorMessage ?? GetDefaultMessage(errorType)}", innerException)
    {
        ErrorType = errorType;
        CustomErrorMessage = customErrorMessage;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WalletException FromException(Exception exception)
    {
        if (exception is WalletException walletException)
        {
            return walletException;
        }

        var errorType = exception switch
        {
            FormatException => WalletErrorType.FormatError,
            InvalidOperationException => WalletErrorType.InvalidOperation,
            ArgumentException => WalletErrorType.ArgumentException,
            UnauthorizedAccessException => WalletErrorType.AccountLocked,
            IOException => WalletErrorType.ExecutionFault,
            _ => WalletErrorType.UnknownError
        };

        return new WalletException(errorType, exception.Message, exception);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetDefaultMessage(WalletErrorType errorType)
    {
        return errorType switch
        {
            WalletErrorType.OpenWalletError => "Failed to open the wallet.",
            WalletErrorType.CreateWalletError => "Failed to create the wallet.",
            WalletErrorType.MigrateAccountError => "Failed to migrate the account.",
            WalletErrorType.LoadWalletError => "Failed to load the wallet.",
            WalletErrorType.CreateAccountError => "Failed to create the account.",
            WalletErrorType.InvalidPrivateKey => "Invalid private key provided.",
            WalletErrorType.ChangePasswordError => "Failed to change the password.",
            WalletErrorType.InsufficientFunds => "Insufficient funds in the wallet.",
            WalletErrorType.InvalidTransaction => "Invalid transaction.",
            WalletErrorType.AccountLocked => "The account is locked.",
            WalletErrorType.TransactionCreationError => "Failed to create the transaction.",
            WalletErrorType.ExecutionFault => "Execution fault.",
            WalletErrorType.FormatError => "Format error occurred.",
            WalletErrorType.InvalidOperation => "Invalid operation.",
            WalletErrorType.FileAlreadyExists => "The wallet file already exists.",
            WalletErrorType.ArgumentException => "Invalid argument provided.",
            WalletErrorType.UnknownError => "An unknown error occurred.",
            WalletErrorType.ContractNotFound => "The contract was not found.",
            WalletErrorType.ContractError => "A contract error occurred.",
            WalletErrorType.VerificationFailed => "Verification failed.",
            WalletErrorType.UnsupportedOperation => "Unsupported operation.",
            WalletErrorType.PasswordIncorrect => "Incorrect password.",
            WalletErrorType.UnsupportedWalletFormat => "Unsupported wallet format.",
            _ => "A wallet error occurred."
        };
    }

    public override string Message => CustomErrorMessage ?? base.Message;
}
