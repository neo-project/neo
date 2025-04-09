// Copyright (C) 2015-2025 The Neo Project.
//
// Program.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.CLI;
using Serilog; // Keep for Log.Fatal/Information during startup/shutdown
using System;

namespace Neo
{
    static class Program
    {
        static void Main(string[] args)
        {
            // Serilog configuration is now handled by MainService
            // Initialize logger to a temporary bootstrap logger until MainService configures it.
            // This captures early startup errors if MainService creation fails.
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Warning() // Log warnings and errors during bootstrap
                .WriteTo.Console()
                .CreateLogger();

            Log.Information("Program.Main started. Creating MainService...");

            try
            {
                // MainService will handle settings loading, logger configuration, and execution.
                var mainService = new MainService();
                mainService.Run(args); // Pass args to MainService to handle
            }
            catch (Exception ex)
            {
                // Catch exceptions during MainService creation or Run setup
                Log.Fatal(ex, "Fatal exception during MainService initialization or execution.");
                Environment.Exit(1); // Exit with error code
            }
            finally
            {
                // Ensure final logs are flushed if MainService didn't do it.
                // MainService.Run should ideally handle its own CloseAndFlush.
                Log.Information("Program.Main exiting.");
                Log.CloseAndFlush();
            }
        }
    }
}
