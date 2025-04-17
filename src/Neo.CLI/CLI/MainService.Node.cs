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
                        var payload = PingPayload.Create(NativeContract.Ledger.CurrentIndex(NeoSystem.StoreView));
                        NeoSystem.LocalNode.Tell(Message.Create(MessageCommand.Ping, payload));
                        await Task.Delay(NeoSystem.GetTimePerBlock() / 4, cancellationToken);
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

            var broadcast = CreateBroadcastTask(cancel.Token);
            var task = Task.Run(async () => await RunDisplayLoop(cancel.Token), cancel.Token);

            WaitForExit(cancel, task, broadcast);
        }

        private class DisplayState
        {
            public DateTime StartTime { get; set; }
            public DateTime LastRefresh { get; set; }
            public uint LastHeight { get; set; }
            public uint LastHeaderHeight { get; set; }
            public int LastTxPoolSize { get; set; }
            public int LastConnectedCount { get; set; }
            public int MaxLines { get; set; }
            public const int RefreshInterval = 1000;

            public DisplayState()
            {
                StartTime = DateTime.Now;
                LastRefresh = DateTime.MinValue;
                LastHeight = 0;
                LastHeaderHeight = 0;
                LastTxPoolSize = 0;
                LastConnectedCount = 0;
                MaxLines = 0;
            }
        }

        private class StateShower
        {
            private readonly MainService _mainService;
            public DisplayState DisplayState { get; set; }
            public Dictionary<int, string> LineBuffer { get; set; }
            public Dictionary<int, ConsoleColor> ColorBuffer { get; set; }

            public StateShower(MainService mainService)
            {
                _mainService = mainService;
                DisplayState = new DisplayState();
                LineBuffer = new Dictionary<int, string>();
                ColorBuffer = new Dictionary<int, ConsoleColor>();
            }

            public void RenderDisplay()
            {
                var originalColor = Console.ForegroundColor;
                var boxWidth = Math.Min(70, Console.WindowWidth - 2);
                var linesWritten = 0;

                try
                {
                    linesWritten = RenderTitleBox(boxWidth, linesWritten);
                    linesWritten = RenderTimeAndUptime(boxWidth, linesWritten);
                    linesWritten = RenderBlockchainAndResources(boxWidth, linesWritten);
                    linesWritten = RenderTransactionAndNetwork(boxWidth, linesWritten);
                    linesWritten = RenderSyncProgress(boxWidth, linesWritten);
                    linesWritten = RenderFooter(boxWidth, linesWritten);

                    DisplayState.MaxLines = Math.Max(DisplayState.MaxLines, linesWritten);
                    FlushDisplayToConsole(boxWidth, originalColor);
                }
                catch (Exception ex)
                {
                    HandleRenderError(ex);
                }
                finally
                {
                    Console.ForegroundColor = originalColor;
                }
            }

            private int RenderTitleBox(int boxWidth, int linesWritten)
            {
                var horizontalLine = new string('─', boxWidth - 2);
                LineBuffer[linesWritten] = "┌" + horizontalLine + "┐";
                ColorBuffer[linesWritten++] = ConsoleColor.DarkGreen;

                string[] largeText = ["           NEO NODE STATUS             "];
                var textWidth = largeText.Max(s => s.Length);
                var contentWidthForTitle = boxWidth - 2;
                var textPadding = (contentWidthForTitle - textWidth) / 2;
                var leftTextPad = new string(' ', textPadding > 0 ? textPadding : 0);

                foreach (var line in largeText)
                {
                    var centeredLine = leftTextPad + line;
                    var finalPaddedLine = centeredLine.PadRight(contentWidthForTitle);
                    if (finalPaddedLine.Length > contentWidthForTitle)
                        finalPaddedLine = finalPaddedLine[..contentWidthForTitle];

                    LineBuffer[linesWritten] = "│" + finalPaddedLine + "│";
                    ColorBuffer[linesWritten++] = ConsoleColor.DarkGreen;
                }

                LineBuffer[linesWritten] = "├" + horizontalLine + "┤";
                ColorBuffer[linesWritten++] = ConsoleColor.DarkGray;
                return linesWritten;
            }

            private int RenderTimeAndUptime(int boxWidth, int linesWritten)
            {
                var now = DateTime.Now;
                var uptime = now - DisplayState.StartTime;
                var time = $" Current Time: {now:yyyy-MM-dd HH:mm:ss}   Uptime: {uptime.Days}d {uptime.Hours:D2}h {uptime.Minutes:D2}m {uptime.Seconds:D2}s";
                var contentWidth = boxWidth - 2;
                var paddedTime = time.PadRight(contentWidth);
                if (paddedTime.Length > contentWidth)
                {
                    paddedTime = paddedTime[..(contentWidth - 3)] + "...";
                }
                else
                {
                    paddedTime = paddedTime[..contentWidth];
                }

                LineBuffer[linesWritten] = "│" + paddedTime + "│";
                ColorBuffer[linesWritten++] = ConsoleColor.Gray;
                return linesWritten;
            }

            private int RenderBlockchainAndResources(int boxWidth, int linesWritten)
            {
                var totalHorizontal = boxWidth - 3;
                var leftSectionWidth = totalHorizontal / 2;
                var rightSectionWidth = totalHorizontal - leftSectionWidth;
                linesWritten = RenderSplitLine(leftSectionWidth, rightSectionWidth, linesWritten, "┬");

                const string blockchainHeader = " BLOCKCHAIN STATUS";
                const string resourcesHeader = " SYSTEM RESOURCES";
                linesWritten = RenderSectionHeaders(blockchainHeader, resourcesHeader, leftSectionWidth, rightSectionWidth, linesWritten);
                linesWritten = RenderSplitLine(leftSectionWidth, rightSectionWidth, linesWritten, "┼");

                // Blockchain content
                return RenderBlockchainContent(leftSectionWidth, rightSectionWidth, linesWritten);
            }

            private int RenderSectionHeaders(string leftHeader, string rightHeader, int leftSectionWidth, int rightSectionWidth, int linesWritten)
            {
                string leftHeaderFormatted, rightHeaderFormatted;
                if (leftHeader.Length > leftSectionWidth)
                    leftHeaderFormatted = leftHeader[..(leftSectionWidth - 3)] + "...";
                else
                    leftHeaderFormatted = leftHeader.PadRight(leftSectionWidth);
                if (rightHeader.Length > rightSectionWidth)
                    rightHeaderFormatted = rightHeader[..(rightSectionWidth - 3)] + "...";
                else
                    rightHeaderFormatted = rightHeader.PadRight(rightSectionWidth);
                LineBuffer[linesWritten] = "│" + leftHeaderFormatted + "│" + rightHeaderFormatted + "│";
                ColorBuffer[linesWritten++] = ConsoleColor.White;
                return linesWritten;
            }

            private int RenderBlockchainContent(int leftSectionWidth, int rightSectionWidth, int linesWritten)
            {
                var currentIndex = NativeContract.Ledger.CurrentIndex(_mainService.NeoSystem.StoreView);
                var headerHeight = _mainService.NeoSystem.HeaderCache.Last?.Index ?? currentIndex;
                var memoryUsage = GC.GetTotalMemory(false) / (1024 * 1024);
                var cpuUsage = GetCpuUsage(DateTime.Now - DisplayState.StartTime);

                var height = $" Block Height:   {currentIndex,10}";
                var memory = $" Memory Usage:   {memoryUsage,10} MB";
                string leftCol1, rightCol1;
                if (height.Length > leftSectionWidth)
                    leftCol1 = height[..(leftSectionWidth - 3)] + "...";
                else
                    leftCol1 = height.PadRight(leftSectionWidth);
                if (memory.Length > rightSectionWidth)
                    rightCol1 = memory[..(rightSectionWidth - 3)] + "...";
                else
                    rightCol1 = memory.PadRight(rightSectionWidth);
                LineBuffer[linesWritten] = "│" + leftCol1 + "│" + rightCol1 + "│";
                ColorBuffer[linesWritten++] = ConsoleColor.Cyan;

                var header = $" Header Height:  {headerHeight,10}";
                var cpu = $" CPU Usage:      {cpuUsage,10:F1} %";
                string leftCol2, rightCol2;
                if (header.Length > leftSectionWidth)
                    leftCol2 = header[..(leftSectionWidth - 3)] + "...";
                else
                    leftCol2 = header.PadRight(leftSectionWidth);
                if (cpu.Length > rightSectionWidth)
                    rightCol2 = cpu[..(rightSectionWidth - 3)] + "...";
                else
                    rightCol2 = cpu.PadRight(rightSectionWidth);
                LineBuffer[linesWritten] = "│" + leftCol2 + "│" + rightCol2 + "│";
                ColorBuffer[linesWritten++] = ConsoleColor.Cyan;
                return linesWritten;
            }

            private int RenderSplitLine(int leftSectionWidth, int rightSectionWidth, int linesWritten,
                 string middleSplitter, string leftSplitter = "├", string rightSplitter = "┤")
            {
                var halfLine1 = new string('─', leftSectionWidth);
                var halfLine2 = new string('─', rightSectionWidth);
                LineBuffer[linesWritten] = leftSplitter + halfLine1 + middleSplitter + halfLine2 + rightSplitter;
                ColorBuffer[linesWritten++] = ConsoleColor.DarkGray;
                return linesWritten;
            }

            private int RenderTransactionAndNetwork(int boxWidth, int linesWritten)
            {
                var totalHorizontal = boxWidth - 3;
                var leftSectionWidth = totalHorizontal / 2;
                var rightSectionWidth = totalHorizontal - leftSectionWidth;

                // split line
                linesWritten = RenderSplitLine(leftSectionWidth, rightSectionWidth, linesWritten, "┼");

                // section headers
                const string txPoolHeader = " TRANSACTION POOL";
                const string networkHeader = " NETWORK STATUS";
                linesWritten = RenderSectionHeaders(txPoolHeader, networkHeader, leftSectionWidth, rightSectionWidth, linesWritten);
                linesWritten = RenderSplitLine(leftSectionWidth, rightSectionWidth, linesWritten, "┼");

                // Content rows
                linesWritten = RenderTransactionContent(leftSectionWidth, rightSectionWidth, linesWritten);
                linesWritten = RenderNetworkContent(leftSectionWidth, rightSectionWidth, linesWritten);
                return RenderSplitLine(leftSectionWidth, rightSectionWidth, linesWritten, "┴", "└", "┘");
            }

            private int RenderTransactionContent(int leftSectionWidth, int rightSectionWidth, int linesWritten)
            {
                var txPoolSize = _mainService.NeoSystem.MemPool.Count;
                var verifiedTxCount = _mainService.NeoSystem.MemPool.VerifiedCount;
                // var unverifiedTxCount = _mainService.NeoSystem.MemPool.UnVerifiedCount;
                var connectedCount = _mainService.LocalNode.ConnectedCount;
                var unconnectedCount = _mainService.LocalNode.UnconnectedCount;

                var totalTx = $" Total Txs:      {txPoolSize,10}";
                var connected = $" Connected:      {connectedCount,10}";
                string leftCol3, rightCol3;
                if (totalTx.Length > leftSectionWidth)
                    leftCol3 = totalTx[..(leftSectionWidth - 3)] + "...";
                else
                    leftCol3 = totalTx.PadRight(leftSectionWidth);
                if (connected.Length > rightSectionWidth)
                    rightCol3 = connected[..(rightSectionWidth - 3)] + "...";
                else
                    rightCol3 = connected.PadRight(rightSectionWidth);
                LineBuffer[linesWritten] = "│" + leftCol3 + "│" + rightCol3 + "│";
                ColorBuffer[linesWritten++] = GetColorForValue(txPoolSize, 100, 500);

                var verified = $" Verified Txs:   {verifiedTxCount,10}";
                var unconnected = $" Unconnected:    {unconnectedCount,10}";
                string leftCol4, rightCol4;
                if (verified.Length > leftSectionWidth)
                    leftCol4 = verified[..(leftSectionWidth - 3)] + "...";
                else
                    leftCol4 = verified.PadRight(leftSectionWidth);
                if (unconnected.Length > rightSectionWidth)
                    rightCol4 = unconnected[..(rightSectionWidth - 3)] + "...";
                else
                    rightCol4 = unconnected.PadRight(rightSectionWidth);
                LineBuffer[linesWritten] = "│" + leftCol4 + "│" + rightCol4 + "│";
                ColorBuffer[linesWritten++] = ConsoleColor.Green;
                return linesWritten;
            }

            private int RenderNetworkContent(int leftSectionWidth, int rightSectionWidth, int linesWritten)
            {
                var unverifiedTxCount = _mainService.NeoSystem.MemPool.UnVerifiedCount;
                var maxPeerBlockHeight = _mainService.GetMaxPeerBlockHeight();

                var unverified = $" Unverified Txs: {unverifiedTxCount,10}";
                var maxHeight = $" Max Block Height: {maxPeerBlockHeight,8}";
                string leftCol5, rightCol5;
                if (unverified.Length > leftSectionWidth)
                    leftCol5 = unverified[..(leftSectionWidth - 3)] + "...";
                else
                    leftCol5 = unverified.PadRight(leftSectionWidth);
                if (maxHeight.Length > rightSectionWidth)
                    rightCol5 = maxHeight[..(rightSectionWidth - 3)] + "...";
                else
                    rightCol5 = maxHeight.PadRight(rightSectionWidth);
                LineBuffer[linesWritten] = "│" + leftCol5 + "│" + rightCol5 + "│";
                ColorBuffer[linesWritten++] = ConsoleColor.Yellow;

                return linesWritten;
            }

            private int RenderSyncProgress(int boxWidth, int linesWritten)
            {
                var currentIndex = NativeContract.Ledger.CurrentIndex(_mainService.NeoSystem.StoreView);
                var maxPeerBlockHeight = _mainService.GetMaxPeerBlockHeight();

                if (currentIndex < maxPeerBlockHeight && maxPeerBlockHeight > 0)
                {
                    LineBuffer[linesWritten] = ProgressBar(currentIndex, maxPeerBlockHeight, boxWidth);
                    ColorBuffer[linesWritten++] = ConsoleColor.Yellow;
                }
                return linesWritten;
            }

            private int RenderFooter(int boxWidth, int linesWritten)
            {
                var footerPosition = Math.Min(Console.WindowHeight - 2, linesWritten + 1);
                footerPosition = Math.Max(linesWritten, footerPosition);

                if (footerPosition < Console.WindowHeight - 1)
                {
                    var footerMsg = "Press any key to exit | Refresh: every 1 second or on blockchain change";
                    var footerMaxWidth = Console.WindowWidth - 2;
                    if (footerMsg.Length > footerMaxWidth)
                        footerMsg = footerMsg[..(footerMaxWidth - 3)] + "...";

                    LineBuffer[footerPosition] = footerMsg;
                    ColorBuffer[footerPosition] = ConsoleColor.DarkGreen;
                    linesWritten += 1;
                }
                return linesWritten;
            }

            private void FlushDisplayToConsole(int boxWidth, ConsoleColor originalColor)
            {
                Console.SetCursorPosition(0, 0);
                var linesToRender = Math.Min(DisplayState.MaxLines, Console.WindowHeight - 1);

                for (var i = 0; i < linesToRender; i++)
                {
                    if (i >= Console.WindowHeight) break;

                    Console.SetCursorPosition(0, i);
                    Console.Write(new string(' ', Console.WindowWidth));
                    Console.SetCursorPosition(0, i);

                    var lineContent = LineBuffer.TryGetValue(i, out var content) ? content : string.Empty;
                    var color = ColorBuffer.TryGetValue(i, out var lineColor) ? lineColor : originalColor;

                    var lineToWrite = lineContent;
                    if (lineToWrite.Length < boxWidth)
                        lineToWrite += new string(' ', boxWidth - lineToWrite.Length);
                    else if (lineToWrite.Length > boxWidth)
                        lineToWrite = lineToWrite[..boxWidth];

                    Console.ForegroundColor = color;
                    Console.Write(lineToWrite);
                }

                for (var i = linesToRender; i < DisplayState.MaxLines; i++)
                {
                    if (i >= Console.WindowHeight) break;
                    Console.SetCursorPosition(0, i);
                    Console.Write(new string(' ', Console.WindowWidth));
                }
            }

            public bool ShouldRefreshDisplay()
            {
                var now = DateTime.Now;
                var state = DisplayState;
                var timeSinceRefresh = (now - state.LastRefresh).TotalMilliseconds;

                var height = NativeContract.Ledger.CurrentIndex(_mainService.NeoSystem.StoreView);
                var headerHeight = _mainService.NeoSystem.HeaderCache.Last?.Index ?? height;
                var txPoolSize = _mainService.NeoSystem.MemPool.Count;
                var connectedCount = _mainService.LocalNode.ConnectedCount;

                return timeSinceRefresh > DisplayState.RefreshInterval ||
                       height != state.LastHeight ||
                       headerHeight != state.LastHeaderHeight ||
                       txPoolSize != state.LastTxPoolSize ||
                       connectedCount != state.LastConnectedCount;
            }

            public static bool ValidateConsoleWindow()
            {
                if (Console.WindowHeight < 23 || Console.WindowWidth < 70)
                {
                    Console.SetCursorPosition(0, 0);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write(new string(' ', Console.BufferWidth));
                    Console.SetCursorPosition(0, 0);
                    Console.WriteLine("Console window too small (Need at least 70x23 visible)...");
                    return false;
                }
                return true;
            }

            public void UpdateDisplayState()
            {
                DisplayState.LastRefresh = DateTime.Now;
                DisplayState.LastHeight = NativeContract.Ledger.CurrentIndex(_mainService.NeoSystem.StoreView);
                DisplayState.LastHeaderHeight = _mainService.NeoSystem.HeaderCache.Last?.Index ?? DisplayState.LastHeight;
                DisplayState.LastTxPoolSize = _mainService.NeoSystem.MemPool.Count;
                DisplayState.LastConnectedCount = _mainService.LocalNode.ConnectedCount;
            }

            private static void HandleRenderError(Exception ex)
            {
                try
                {
                    Console.Clear();
                    Console.WriteLine($"Render error: {ex.Message}\nStack: {ex.StackTrace}");
                }
                catch { }
            }

            private static double GetCpuUsage(TimeSpan uptime)
            {
                try
                {
                    var currentProcess = Process.GetCurrentProcess();
                    // Ensure uptime is not zero to avoid division by zero
                    if (uptime.TotalMilliseconds > 0 && Environment.ProcessorCount > 0)
                    {
                        var cpuUsage = Math.Round(currentProcess.TotalProcessorTime.TotalMilliseconds /
                            (Environment.ProcessorCount * uptime.TotalMilliseconds) * 100, 1);
                        if (cpuUsage < 0) cpuUsage = 0; // Clamp negative values if system reports oddities
                        if (cpuUsage > 100) cpuUsage = 100;
                        return cpuUsage;
                    }
                }
                catch { /* Ignore CPU usage calculation errors */ }

                return 0;
            }

            private static string ProgressBar(uint height, uint maxPeerBlockHeight, int boxWidth)
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
                return progressText.PadRight(maxWidth);
            }
        }

        private async Task RunDisplayLoop(CancellationToken cancellationToken)
        {
            var stateShower = new StateShower(this);
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (stateShower.ShouldRefreshDisplay())
                    {
                        if (!StateShower.ValidateConsoleWindow())
                        {
                            await Task.Delay(500, cancellationToken);
                            continue;
                        }

                        stateShower.UpdateDisplayState();
                        stateShower.RenderDisplay();
                    }

                    await Task.Delay(100, cancellationToken);
                }
                catch (TaskCanceledException) { break; }
                catch (Exception ex)
                {
                    await HandleDisplayError(ex, cancellationToken);
                }
            }
        }

        private static void WaitForExit(CancellationTokenSource cancel, Task task, Task broadcast)
        {
            Console.ReadKey(true);
            cancel.Cancel();
            try { Task.WaitAll(task, broadcast); } catch { }
            Console.WriteLine();
            Console.CursorVisible = true;
            Console.ResetColor();
            Console.Clear();
        }

        private static async Task HandleDisplayError(Exception ex, CancellationToken cancellationToken)
        {
            try
            {
                Console.Clear();
                Console.WriteLine($"Display error: {ex.Message}\nStack: {ex.StackTrace}");
                await Task.Delay(1000, cancellationToken);
            }
            catch { }
        }

        // /// <summary>
        // /// Returns an appropriate console color based on latency value
        // /// </summary>
        // private static ConsoleColor GetColorForLatency(double latency)
        // {
        //     if (latency < 100) return ConsoleColor.Green;
        //     if (latency < 300) return ConsoleColor.DarkGreen;
        //     if (latency < 1000) return ConsoleColor.Yellow;
        //     if (latency < 3000) return ConsoleColor.DarkYellow;
        //     return ConsoleColor.Red;
        // }

        /// <summary>
        /// Returns an appropriate console color based on a value's proximity to thresholds
        /// </summary>
        private static ConsoleColor GetColorForValue(int value, int lowThreshold, int highThreshold)
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
