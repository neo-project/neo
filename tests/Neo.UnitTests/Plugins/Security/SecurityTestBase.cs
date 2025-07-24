// Copyright (C) 2015-2025 The Neo Project.
//
// SecurityTestBase.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Plugins.Security;

namespace Neo.UnitTests.Plugins.Security
{
    /// <summary>
    /// Base class for security tests that sets up mock dependencies.
    /// </summary>
    public abstract class SecurityTestBase
    {
        [TestInitialize]
        public virtual void SetUp()
        {
            // Enable test mode to use mock implementations
            ServiceLocator.EnableTestMode();
        }

        [TestCleanup]
        public virtual void TearDown()
        {
            // Reset service locator for test isolation
            ServiceLocator.Reset();
        }

        /// <summary>
        /// Sets up custom mock implementations for specific test scenarios.
        /// </summary>
        protected void SetupCustomMocks(
            IPermissionCacheManager permissionCacheManager = null,
            IThreadSafeStateManager threadSafeStateManager = null)
        {
            ServiceLocator.SetCustomImplementations(
                permissionCacheManager,
                threadSafeStateManager);
        }
    }
}
