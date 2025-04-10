// Copyright (C) 2015-2025 The Neo Project.
//
// MainService.Node.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Neo.ConsoleService;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.CLI
{
    partial class MainService
    {
        /// <summary>
        /// Process "show pool" command
        /// </summary>
        [ConsoleCommand("show pool", Category = "Node Commands", Description = "Show the current state of the mempool")]
        private void OnShowPoolCommand(bool verbose = false)
        {
            int verifiedCount, unverifiedCount;
            if (verbose)
            {
                NeoSystem.MemPool.GetVerifiedAndUnverifiedTransactions(
                    out IEnumerable<Transaction> verifiedTransactions,
                    out IEnumerable<Transaction> unverifiedTransactions);
                ConsoleHelper.Info("Verified Transactions:");
                foreach (Transaction tx in verifiedTransactions)
                    Console.WriteLine($" {tx.Hash} {tx.GetType().Name} {tx.NetworkFee} GAS_NetFee");
                ConsoleHelper.Info("Unverified Transactions:");
                foreach (Transaction tx in unverifiedTransactions)
                    Console.WriteLine($" {tx.Hash} {tx.GetType().Name} {tx.NetworkFee} GAS_NetFee");

                verifiedCount = verifiedTransactions.Count();
                unverifiedCount = unverifiedTransactions.Count();
            }
            else
            {
                verifiedCount = NeoSystem.MemPool.VerifiedCount;
                unverifiedCount = NeoSystem.MemPool.UnVerifiedCount;
            }
            Console.WriteLine($"total: {NeoSystem.MemPool.Count}, verified: {verifiedCount}, unverified: {unverifiedCount}");
        }

        private Task CreateBroadcastTask(CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        NeoSystem.LocalNode.Tell(Message.Create(MessageCommand.Ping, PingPayload.Create(NativeContract.Ledger.CurrentIndex(NeoSystem.StoreView))));
                        await Task.Delay(NeoSystem.Settings.TimePerBlock / 4, cancellationToken);
                    }
                    catch (TaskCanceledException) { break; }
                    catch { await Task.Delay(500, cancellationToken); }
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Process "show state" command
        /// </summary>
        [ConsoleCommand("show state", Category = "Node Commands", Description = "Show the current state of the node")]
        private void OnShowStateCommand()
        {
            var cancel = new CancellationTokenSource();
            Console.CursorVisible = false;
            Console.Clear();

            // Background task to send ping messages to peers
            var broadcast = CreateBroadcastTask(cancel.Token);

            // Display task
            var task = Task.Run(async () =>
            {
                var maxLines = 0;
                var startTime = DateTime.Now;
                var refreshInterval = 1000; // Slower refresh to reduce flickering
                var lastRefresh = DateTime.MinValue;
                var lastHeight = 0u;
                var lastHeaderHeight = 0u;
                var lastTxPoolSize = 0;
                var lastConnectedCount = 0;
                var lineBuffer = new Dictionary<int, string>();
                var colorBuffer = new Dictionary<int, ConsoleColor>();

                while (!cancel.Token.IsCancellationRequested)
                {
                    try
                    {
                        // Only refresh data if time elapsed or significant changes
                        var now = DateTime.Now;
                        var timeSinceRefresh = (now - lastRefresh).TotalMilliseconds;

                        // Capture data (do this frequently)
                        var height = NativeContract.Ledger.CurrentIndex(NeoSystem.StoreView);
                        var headerHeight = NeoSystem.HeaderCache.Last?.Index ?? height;

                        // Get maximum block height from connected peers
                        var maxPeerBlockHeight = GetMaxPeerBlockHeight();

                        var uptime = now - startTime;
                        var memoryUsage = GC.GetTotalMemory(false) / (1024 * 1024);
                        double cpuUsage = 0;
                        try
                        {
                            var currentProcess = Process.GetCurrentProcess();
                            // Ensure uptime is not zero to avoid division by zero
                            if (uptime.TotalMilliseconds > 0 && Environment.ProcessorCount > 0)
                            {
                                cpuUsage = Math.Round(currentProcess.TotalProcessorTime.TotalMilliseconds /
                                    (Environment.ProcessorCount * uptime.TotalMilliseconds) * 100, 1);
                                if (cpuUsage < 0) cpuUsage = 0; // Clamp negative values if system reports oddities
                                if (cpuUsage > 100) cpuUsage = 100;
                            }
                        }
                        catch { /* Ignore CPU usage calculation errors */ }
                        var txPoolSize = NeoSystem.MemPool.Count;
                        var verifiedTxCount = NeoSystem.MemPool.VerifiedCount;
                        var unverifiedTxCount = NeoSystem.MemPool.UnVerifiedCount;
                        var connectedCount = LocalNode.ConnectedCount;
                        var unconnectedCount = LocalNode.UnconnectedCount;

                        // Check if we need to refresh screen based on changes or time
                        var needRefresh = timeSinceRefresh > refreshInterval ||
                                          height != lastHeight ||
                                          headerHeight != lastHeaderHeight ||
                                          txPoolSize != lastTxPoolSize ||
                                          connectedCount != lastConnectedCount;

                        if (!needRefresh)
                        {
                            await Task.Delay(100, cancel.Token);
                            continue;
                        }

                        // Make sure the console VISIBLE WINDOW is available and has a reasonable size
                        if (Console.WindowHeight < 23 || Console.WindowWidth < 70) // Check WINDOW size, reduced height
                        {
                            // Wait and try again if console is too small
                            Console.SetCursorPosition(0, 0);
                            Console.ForegroundColor = ConsoleColor.Red;
                            // Clear previous message potentially
                            Console.Write(new string(' ', Console.BufferWidth));
                            Console.SetCursorPosition(0, 0);
                            Console.WriteLine("Console window too small (Need at least 70x23 visible)...");
                            await Task.Delay(500, cancel.Token);
                            continue;
                        }

                        // Update last values
                        lastRefresh = now;
                        lastHeight = height;
                        lastHeaderHeight = headerHeight;
                        lastTxPoolSize = txPoolSize;
                        lastConnectedCount = connectedCount;

                        // Save console state and create new buffers
                        var originalColor = Console.ForegroundColor;
                        var linesWritten = 0;
                        lineBuffer.Clear();
                        colorBuffer.Clear();

                        // Title box (calculate width based on VISIBLE console size)
                        var boxWidth = Math.Min(70, Console.WindowWidth - 2); // Use WindowWidth
                        var horizontalLine = new string('─', boxWidth - 2);

                        lineBuffer[linesWritten] = "┌" + horizontalLine + "┐";
                        colorBuffer[linesWritten++] = ConsoleColor.DarkGreen;

                        // Simple smaller text for NEO NODE
                        string[] largeText = ["           NEO NODE STATUS             "];
                        var textWidth = largeText.Max(s => s.Length);
                        var contentWidthForTitle = boxWidth - 2; // Width inside the │...│
                        var textPadding = (contentWidthForTitle - textWidth) / 2;
                        var leftTextPad = new string(' ', textPadding > 0 ? textPadding : 0);

                        foreach (var line in largeText)
                        {
                            // Create the centered line segment
                            var centeredLine = leftTextPad + line;
                            // Pad the result to the full content width
                            var finalPaddedLine = centeredLine.PadRight(contentWidthForTitle);
                            // Truncate just in case padding calculation had off-by-one on odd widths
                            if (finalPaddedLine.Length > contentWidthForTitle) finalPaddedLine = finalPaddedLine.Substring(0, contentWidthForTitle);

                            lineBuffer[linesWritten] = "│" + finalPaddedLine + "│";
                            colorBuffer[linesWritten++] = ConsoleColor.DarkGreen;
                        }

                        // Separator below ASCII title - Use full horizontal line
                        lineBuffer[linesWritten] = "├" + horizontalLine + "┤";
                        colorBuffer[linesWritten++] = ConsoleColor.DarkGray;

                        // Current time and uptime line
                        var timeStr = $" Current Time: {now:yyyy-MM-dd HH:mm:ss}   Uptime: {uptime.Days}d {uptime.Hours:D2}h {uptime.Minutes:D2}m {uptime.Seconds:D2}s";
                        var contentWidth = boxWidth - 2; // Use contentWidth for padding here
                        var paddedTime = timeStr.PadRight(contentWidth);
                        if (paddedTime.Length > contentWidth) paddedTime = paddedTime.Substring(0, contentWidth - 3) + "...";
                        else paddedTime = paddedTime.Substring(0, contentWidth);
                        lineBuffer[linesWritten] = "│" + paddedTime + "│";
                        colorBuffer[linesWritten++] = ConsoleColor.Gray;

                        // Calculate section widths directly based on boxWidth
                        var totalHorizontal = boxWidth - 3; // Total space for dashes and content, excluding 3 vertical bars │..│..│
                        var leftSectionWidth = totalHorizontal / 2;
                        var rightSectionWidth = totalHorizontal - leftSectionWidth;
                        var halfLine1 = new string('─', leftSectionWidth);
                        var halfLine2 = new string('─', rightSectionWidth);

                        // Separator between header and sections
                        lineBuffer[linesWritten] = "├" + halfLine1 + "┬" + halfLine2 + "┤";
                        colorBuffer[linesWritten++] = ConsoleColor.DarkGray;

                        // BLOCKCHAIN SECTION & SYSTEM RESOURCES header row
                        var blockchainHeader = " BLOCKCHAIN STATUS";
                        var resourcesHeader = " SYSTEM RESOURCES";
                        string leftHeader, rightHeader;
                        if (blockchainHeader.Length > leftSectionWidth)
                            leftHeader = blockchainHeader[..(leftSectionWidth - 3)] + "...";
                        else
                            leftHeader = blockchainHeader.PadRight(leftSectionWidth);
                        if (resourcesHeader.Length > rightSectionWidth)
                            rightHeader = resourcesHeader[..(rightSectionWidth - 3)] + "...";
                        else
                            rightHeader = resourcesHeader.PadRight(rightSectionWidth);
                        lineBuffer[linesWritten] = "│" + leftHeader + "│" + rightHeader + "│";
                        colorBuffer[linesWritten++] = ConsoleColor.White;

                        lineBuffer[linesWritten] = "├" + halfLine1 + "┼" + halfLine2 + "┤";
                        colorBuffer[linesWritten++] = ConsoleColor.DarkGray;

                        // Block heights & Resources rows
                        var heightStr = $" Block Height:   {height,10}";
                        var memoryStr = $" Memory Usage:   {memoryUsage,10} MB";
                        string leftCol1, rightCol1;
                        if (heightStr.Length > leftSectionWidth)
                            leftCol1 = heightStr[..(leftSectionWidth - 3)] + "...";
                        else
                            leftCol1 = heightStr.PadRight(leftSectionWidth);
                        if (memoryStr.Length > rightSectionWidth)
                            rightCol1 = memoryStr[..(rightSectionWidth - 3)] + "...";
                        else
                            rightCol1 = memoryStr.PadRight(rightSectionWidth);
                        lineBuffer[linesWritten] = "│" + leftCol1 + "│" + rightCol1 + "│";
                        colorBuffer[linesWritten++] = ConsoleColor.Cyan;

                        var headerStr = $" Header Height:  {headerHeight,10}";
                        var cpuStr = $" CPU Usage:      {cpuUsage,10:F1} %";
                        string leftCol2, rightCol2;
                        if (headerStr.Length > leftSectionWidth)
                            leftCol2 = headerStr[..(leftSectionWidth - 3)] + "...";
                        else
                            leftCol2 = headerStr.PadRight(leftSectionWidth);
                        if (cpuStr.Length > rightSectionWidth)
                            rightCol2 = cpuStr[..(rightSectionWidth - 3)] + "...";
                        else
                            rightCol2 = cpuStr.PadRight(rightSectionWidth);
                        lineBuffer[linesWritten] = "│" + leftCol2 + "│" + rightCol2 + "│";
                        colorBuffer[linesWritten++] = ConsoleColor.Cyan;

                        // Separator between first and second section groups
                        lineBuffer[linesWritten] = "├" + halfLine1 + "┼" + halfLine2 + "┤";
                        colorBuffer[linesWritten++] = ConsoleColor.DarkGray;

                        // TRANSACTION POOL & NETWORK STATUS header row
                        var txPoolHeader = " TRANSACTION POOL";
                        var networkHeader = " NETWORK STATUS";
                        string leftHeader2, rightHeader2;
                        if (txPoolHeader.Length > leftSectionWidth)
                            leftHeader2 = txPoolHeader[..(leftSectionWidth - 3)] + "...";
                        else
                            leftHeader2 = txPoolHeader.PadRight(leftSectionWidth);
                        if (networkHeader.Length > rightSectionWidth)
                            rightHeader2 = networkHeader[..(rightSectionWidth - 3)] + "...";
                        else
                            rightHeader2 = networkHeader.PadRight(rightSectionWidth);
                        lineBuffer[linesWritten] = "│" + leftHeader2 + "│" + rightHeader2 + "│";
                        colorBuffer[linesWritten++] = ConsoleColor.White;

                        lineBuffer[linesWritten] = "├" + halfLine1 + "┼" + halfLine2 + "┤";
                        colorBuffer[linesWritten++] = ConsoleColor.DarkGray;

                        // Transaction data rows & Network data rows
                        var totalTxStr = $" Total Txs:      {txPoolSize,10}";
                        var connectedStr = $" Connected:      {connectedCount,10}";
                        string leftCol3, rightCol3;
                        if (totalTxStr.Length > leftSectionWidth)
                            leftCol3 = totalTxStr[..(leftSectionWidth - 3)] + "...";
                        else
                            leftCol3 = totalTxStr.PadRight(leftSectionWidth);
                        if (connectedStr.Length > rightSectionWidth)
                            rightCol3 = connectedStr[..(rightSectionWidth - 3)] + "...";
                        else
                            rightCol3 = connectedStr.PadRight(rightSectionWidth);
                        lineBuffer[linesWritten] = "│" + leftCol3 + "│" + rightCol3 + "│";
                        colorBuffer[linesWritten++] = GetColorForValue(txPoolSize, 100, 500);

                        var verifiedStr = $" Verified Txs:   {verifiedTxCount,10}";
                        var unconnectedStr = $" Unconnected:    {unconnectedCount,10}";
                        string leftCol4, rightCol4;
                        if (verifiedStr.Length > leftSectionWidth)
                            leftCol4 = verifiedStr[..(leftSectionWidth - 3)] + "...";
                        else
                            leftCol4 = verifiedStr.PadRight(leftSectionWidth);
                        if (unconnectedStr.Length > rightSectionWidth)
                            rightCol4 = unconnectedStr[..(rightSectionWidth - 3)] + "...";
                        else
                            rightCol4 = unconnectedStr.PadRight(rightSectionWidth);
                        lineBuffer[linesWritten] = "│" + leftCol4 + "│" + rightCol4 + "│";
                        colorBuffer[linesWritten++] = ConsoleColor.Green;

                        var unverifiedStr = $" Unverified Txs: {unverifiedTxCount,10}";
                        var maxHeightStr = $" Max Block Height: {maxPeerBlockHeight,8}";
                        string leftCol5, rightCol5;
                        if (unverifiedStr.Length > leftSectionWidth)
                            leftCol5 = unverifiedStr[..(leftSectionWidth - 3)] + "...";
                        else
                            leftCol5 = unverifiedStr.PadRight(leftSectionWidth);
                        if (maxHeightStr.Length > rightSectionWidth)
                            rightCol5 = maxHeightStr[..(rightSectionWidth - 3)] + "...";
                        else
                            rightCol5 = maxHeightStr.PadRight(rightSectionWidth);
                        lineBuffer[linesWritten] = "│" + leftCol5 + "│" + rightCol5 + "│";
                        colorBuffer[linesWritten++] = ConsoleColor.Yellow;

                        // Bottom of main box
                        lineBuffer[linesWritten] = "└" + halfLine1 + "┴" + halfLine2 + "┘";
                        colorBuffer[linesWritten++] = ConsoleColor.DarkGray;

                        // Add sync progress bar if node is not fully synced
                        if (height < maxPeerBlockHeight && maxPeerBlockHeight > 0)
                        {
                            // Calculate sync percentage
                            var syncPercentage = (double)height / maxPeerBlockHeight * 100;

                            // Create progress bar (width: boxWidth - 20)
                            var progressBarWidth = boxWidth - 25; // Reduce bar width to save space for percentage
                            var filledWidth = (int)Math.Round(progressBarWidth * syncPercentage / 100);
                            if (filledWidth > progressBarWidth) filledWidth = progressBarWidth;

                            var progressFilled = new string('█', filledWidth);
                            var progressEmpty = new string('░', progressBarWidth - filledWidth);

                            // Format with percentage as whole number
                            var percentDisplay = $"{syncPercentage:F2}%";
                            var barDisplay = $"[{progressFilled}{progressEmpty}]";
                            var heightDisplay = $"({height}/{maxPeerBlockHeight})";
                            var progressText = $" Syncing: {barDisplay} {percentDisplay} {heightDisplay}";

                            // Check if we need to truncate the text to fit the line
                            var maxWidth = boxWidth - 2;
                            if (progressText.Length > maxWidth)
                            {
                                // Keep the percentage part and truncate other parts if needed
                                var desiredLength = maxWidth - 3; // for "..."

                                // Try to keep just the sync bar and percentage
                                var shorterText = $" Syncing: {barDisplay} {percentDisplay}";

                                if (shorterText.Length <= desiredLength)
                                {
                                    progressText = shorterText;
                                }
                                else
                                {
                                    // Even the shortened version is too long, need to shrink the bar
                                    var barPartStart = " Syncing: ".Length;
                                    var minBarSize = 10; // Keep at least [████...] so user can see something

                                    var spaceForBar = desiredLength - barPartStart - percentDisplay.Length - 1; // -1 for space
                                    var newBarLength = Math.Max(minBarSize, spaceForBar);

                                    // Create a smaller bar with ... if needed
                                    if (newBarLength < barDisplay.Length)
                                    {
                                        var filledToShow = Math.Min(filledWidth, newBarLength - 5); // -5 for "[...]"
                                        barDisplay = "[" + new string('█', filledToShow) + "...]";
                                    }

                                    progressText = $" Syncing: {barDisplay} {percentDisplay}";

                                    // Final check to ensure we're not still too long
                                    if (progressText.Length > desiredLength)
                                    {
                                        progressText = $" Sync: {percentDisplay}"; // Absolute fallback
                                    }
                                }
                            }

                            // Pad to full width
                            progressText = progressText.PadRight(maxWidth);

                            // Add to buffer
                            lineBuffer[linesWritten] = progressText;
                            colorBuffer[linesWritten++] = ConsoleColor.Yellow;
                        }

                        // Update maxLines for tracking
                        maxLines = Math.Max(maxLines, linesWritten);

                        // Add footer
                        try
                        {
                            // Calculate a safe position for the footer based on WINDOW height
                            var footerPosition = Math.Min(Console.WindowHeight - 2, linesWritten + 1);

                            // Make sure it's within bounds and at least one line below the content
                            footerPosition = Math.Max(linesWritten, footerPosition);

                            if (footerPosition < Console.WindowHeight - 1) // Check against WindowHeight, fixed formatting
                            {
                                // Footer message with auto-truncation to fit screen
                                var footerMsg = "Press any key to exit | Refresh: every 1 second or on blockchain change";
                                var footerMaxWidth = Console.WindowWidth - 2; // Use WindowWidth
                                if (footerMsg.Length > footerMaxWidth)
                                    footerMsg = footerMsg[..(footerMaxWidth - 3)] + "...";

                                lineBuffer[footerPosition] = footerMsg;
                                colorBuffer[footerPosition] = ConsoleColor.DarkGreen;
                            }

                            // Render buffer to screen efficiently
                            Console.SetCursorPosition(0, 0);

                            // Determine actual number of lines to render based on WINDOW height
                            var linesToRender = Math.Min(maxLines, Console.WindowHeight - 1);

                            // Write each line with proper colors and spacing
                            for (var i = 0; i < linesToRender; i++)
                            {
                                try
                                {
                                    // Make sure we don't exceed the console WINDOW height
                                    if (i >= Console.WindowHeight)
                                        break;

                                    // Clear entire line first based on WINDOW width
                                    Console.SetCursorPosition(0, i);
                                    Console.Write(new string(' ', Console.WindowWidth));

                                    // Position cursor to write content
                                    Console.SetCursorPosition(0, i);

                                    // Get content and color
                                    var lineContent = lineBuffer.TryGetValue(i, out var content) ? content : string.Empty;
                                    ConsoleColor color = colorBuffer.TryGetValue(i, out var lineColor) ? lineColor : originalColor;

                                    // Ensure line content does not exceed box width and pad/truncate
                                    var lineToWrite = lineContent;
                                    if (lineToWrite.Length < boxWidth)
                                    {
                                        lineToWrite += new string(' ', boxWidth - lineToWrite.Length);
                                    }
                                    else if (lineToWrite.Length > boxWidth)
                                    {
                                        lineToWrite = lineToWrite[..boxWidth];
                                    }

                                    // Apply color and write content for the box
                                    Console.ForegroundColor = color;
                                    Console.Write(lineToWrite);

                                    // Removed redundant clearing logic here
                                }
                                catch // Catch potential IO errors during console writes
                                {
                                    // Stop rendering this frame if console handle is invalid (e.g., closed window)
                                    break;
                                }
                            }
                            // Add blank lines below content if needed to clear previous footer etc. based on WINDOW height
                            for (var i = linesToRender; i < maxLines; i++)
                            {
                                if (i >= Console.WindowHeight)
                                    break;
                                Console.SetCursorPosition(0, i);
                                Console.Write(new string(' ', Console.WindowWidth));
                            }
                        }
                        catch (Exception ex)
                        {
                            // Handle display errors gracefully
                            try
                            {
                                Console.Clear(); // Attempt to clear console on error
                                Console.WriteLine($"Display error: {ex.Message}\nStack: {ex.StackTrace}");
                                await Task.Delay(1000, cancel.Token); // Pause to show error
                            }
                            catch { cancel.Cancel(); break; } // Exit loop if error handling fails
                        }

                        // Reset color
                        Console.ForegroundColor = originalColor;

                        // Wait before next update
                        await Task.Delay(100, cancel.Token);
                    }
                    catch (TaskCanceledException) { break; }
                    catch (Exception ex)
                    {
                        // Handle display errors gracefully
                        try
                        {
                            Console.Clear();
                            Console.WriteLine($"Outer loop error: {ex.Message}\nStack: {ex.StackTrace}");
                            await Task.Delay(1000, cancel.Token);
                        }
                        catch { break; }
                    }
                }
            });

            // Wait for user input to exit
            Console.ReadKey(true);
            cancel.Cancel();
            try { Task.WaitAll(task, broadcast); } catch { }
            Console.WriteLine();
            Console.CursorVisible = true;
            Console.ResetColor(); // Ensure color is reset on exit
            Console.Clear(); // Clear the status screen on exit
        }

        /// <summary>
        /// Returns an appropriate console color based on latency value
        /// </summary>
        private ConsoleColor GetColorForLatency(double latency)
        {
            if (latency < 100) return ConsoleColor.Green;
            if (latency < 300) return ConsoleColor.DarkGreen;
            if (latency < 1000) return ConsoleColor.Yellow;
            if (latency < 3000) return ConsoleColor.DarkYellow;
            return ConsoleColor.Red;
        }

        /// <summary>
        /// Returns an appropriate console color based on a value's proximity to thresholds
        /// </summary>
        private ConsoleColor GetColorForValue(int value, int lowThreshold, int highThreshold)
        {
            if (value < lowThreshold) return ConsoleColor.Green;
            if (value < highThreshold) return ConsoleColor.Yellow;
            return ConsoleColor.Red;
        }

        private uint GetMaxPeerBlockHeight()
        {
            var nodes = LocalNode.GetRemoteNodes().ToArray();
            if (nodes.Length == 0) return 0;

            return nodes.Select(u => u.LastBlockIndex).Max();
        }
    }
}
