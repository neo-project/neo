// Copyright (C) 2015-2024 The Neo Project.
//
// INotifyHandler.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.SmartContract;

namespace Neo.IEventHandlers;

public interface INotifyHandler
{
    /// <summary>
    /// The handler of Notify event from <see cref="ApplicationEngine"/>
    /// Triggered when a contract calls System.Runtime.Notify.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="notifyEventArgs">The arguments of the notification.</param>
    void ApplicationEngine_Notify_Handler(object sender, NotifyEventArgs notifyEventArgs);
}
