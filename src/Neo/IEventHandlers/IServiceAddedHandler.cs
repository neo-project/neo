// Copyright (C) 2015-2025 The Neo Project.
//
// IServiceAddedHandler.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.IEventHandlers
{
    public interface IServiceAddedHandler
    {
        /// <summary>
        /// The handler of ServiceAdded event from the <see cref="NeoSystem"/>.
        /// Triggered when a service is added to the <see cref="NeoSystem"/>.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="service">The service added.</param>
        void NeoSystem_ServiceAdded_Handler(object sender, object service);
    }
}
