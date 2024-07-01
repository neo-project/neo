// Copyright (C) 2015-2024 The Neo Project.
//
// CandidateCommand.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.CommandLine;

namespace Neo.Hosting.App.CommandLine.Prompt
{
    internal sealed partial class CandidateCommand : Command
    {
        public CandidateCommand() : base("candidate", "List, register and vote")
        {
            var registerCommand = new RegisterCommand();
            var unregisterCommand = new UnRegisterCommand();
            var voteCommand = new VoteCommand();
            var unvoteCommand = new UnvoteCommand();
            var listCommand = new ListCommand();

            AddCommand(registerCommand);
            AddCommand(unregisterCommand);
            AddCommand(voteCommand);
            AddCommand(unvoteCommand);
            AddCommand(listCommand);
        }
    }
}
