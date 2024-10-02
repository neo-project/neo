// Copyright (C) 2015-2024 The Neo Project.
//
// CustomAutoPilot.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Akka.TestKit;
using System;

namespace Neo.Plugins.DBFTPlugin.Tests;

internal class CustomAutoPilot(Action<IActorRef, object> action) : AutoPilot
{
    public override AutoPilot Run(IActorRef sender, object message)
    {
        action(sender, message);
        return this;
    }
}
