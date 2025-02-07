// Copyright (C) 2015-2025 The Neo Project.
//
// TestMSBuildEngine.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Build.Framework;
using System;
using System.Collections;

namespace Neo.Build.Tests.MSBuild
{
    internal class TestMSBuildEngine : IBuildEngine
    {
        public bool ContinueOnError => false;

        public int LineNumberOfTaskNode => 0;

        public int ColumnNumberOfTaskNode => 0;

        public string ProjectFileOfTaskNode => string.Empty;

        public BuildErrorEventArgs[] ErrorLog => _buildErrorEvents;

        private BuildErrorEventArgs[] _buildErrorEvents = [];

        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs)
        {
            throw new NotImplementedException();
        }

        public void LogCustomEvent(CustomBuildEventArgs e)
        {
            throw new NotImplementedException();
        }

        public void LogErrorEvent(BuildErrorEventArgs e)
        {
            _buildErrorEvents = [.. _buildErrorEvents, e];
        }

        public void LogMessageEvent(BuildMessageEventArgs e)
        {
            throw new NotImplementedException();
        }

        public void LogWarningEvent(BuildWarningEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
