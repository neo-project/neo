// Copyright (C) 2015-2024 The Neo Project.
//
// AnsiStringStyle.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.CommandLine.Rendering
{
    internal sealed class AnsiStringStyle
    {
        public AnsiStringStyle()
        {
            Color = AnsiColor.Default;
            Background = AnsiBackgroundColor.Default;
        }

        public AnsiColor Color { get; set; }
        public AnsiBackgroundColor Background { get; set; }
        public AnsiStyle? Style { get; set; }
    }
}
