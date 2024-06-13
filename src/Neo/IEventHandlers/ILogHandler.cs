// Copyright (C) 2015-2024 The Neo Project.
//
// ILogHandler.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.SmartContract;

namespace Neo.IEventHandlers;

public interface ILogHandler
{
    /// <summary>
    /// The handler of Log event from the <see cref="ApplicationEngine"/>.
    /// Triggered when a contract calls System.Runtime.Log.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="logEventArgs">The arguments <see cref="LogEventArgs"/> of the log.</param>
    void ApplicationEngine_Log_Handler(object sender, LogEventArgs logEventArgs);
}
