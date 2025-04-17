// Copyright (C) 2015-2025 The Neo Project.
//
// FuzzConsensusInvariants.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Plugins.DBFTPlugin.Consensus;
using Neo.Plugins.DBFTPlugin.Messages;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Plugins.DBFTPlugin.Fuzzing.Tests.Core
{
    public partial class FuzzConsensus
    {
        // Track state across multiple fuzzing runs to detect liveness issues
        private static readonly Dictionary<uint, LivenessTracker> _livenessTrackers = new Dictionary<uint, LivenessTracker>();

        // Maximum number of view changes before we consider it a liveness issue
        private const int MaxViewChangesBeforeLivenessIssue = 10;

        private static void CheckInvariants()
        {
            // Invariant 1: Should not finalize block unless conditions met
            int commitCount = _context.CommitPayloads.Count(p => p != null && _context.GetMessage<Commit>(p)?.ViewNumber == _context.ViewNumber);
            bool hasAllTransactions = _context.TransactionHashes.All(h => _context.Transactions.ContainsKey(h));
            bool shouldBeFinalized = commitCount >= _context.M && hasAllTransactions;

            if (_context.BlockSent && !shouldBeFinalized)
            {
                throw new InvalidOperationException("Invariant violated: Block marked sent but commit/transaction conditions not met.");
            }

            // Track liveness for this block height
            TrackLiveness();

            // Invariant 2: View number must be non-negative
            if (_context.ViewNumber < 0)
            {
                throw new InvalidOperationException($"Invariant violated: View number {_context.ViewNumber} is negative.");
            }

            // Invariant 3: Primary index must be valid
            if (_context.Block.PrimaryIndex >= _context.Validators.Length)
            {
                throw new InvalidOperationException($"Invariant violated: Primary index {_context.Block.PrimaryIndex} is out of range.");
            }

            // Invariant 4: Preparation consistency
            var primaryPrep = _context.PreparationPayloads[_context.Block.PrimaryIndex];
            if (primaryPrep != null)
            {
                // If we have a primary preparation, all other preparations must match its hash
                for (int i = 0; i < _context.PreparationPayloads.Length; i++)
                {
                    if (i != _context.Block.PrimaryIndex && _context.PreparationPayloads[i] != null)
                    {
                        var response = _context.GetMessage<PrepareResponse>(_context.PreparationPayloads[i]);
                        if (response != null && !response.PreparationHash.Equals(primaryPrep.Hash))
                        {
                            throw new InvalidOperationException($"Invariant violated: Preparation hash mismatch for validator {i}.");
                        }
                    }
                }
            }

            // Invariant 5: Commit consistency - all commits must be for the same view
            var commitViews = _context.CommitPayloads
                .Where(p => p != null)
                .Select(p => _context.GetMessage<Commit>(p)?.ViewNumber ?? 0)
                .Distinct();

            if (commitViews.Count() > 1)
            {
                throw new InvalidOperationException("Invariant violated: Commits for different views detected.");
            }

            // Invariant 6: Transaction consistency
            if (_context.TransactionHashes != null)
            {
                // No duplicate transaction hashes
                if (_context.TransactionHashes.Length != _context.TransactionHashes.Distinct().Count())
                {
                    throw new InvalidOperationException("Invariant violated: Duplicate transaction hashes detected.");
                }

                // Transaction count within limits
                if (_context.TransactionHashes.Length > _system.Settings.MaxTransactionsPerBlock)
                {
                    throw new InvalidOperationException($"Invariant violated: Too many transactions: {_context.TransactionHashes.Length}.");
                }
            }

            // Invariant 7: ChangeView consistency
            foreach (var payload in _context.ChangeViewPayloads.Where(p => p != null))
            {
                var view = _context.GetMessage<ChangeView>(payload);
                if (view != null && view.NewViewNumber <= view.ViewNumber)
                {
                    throw new InvalidOperationException($"Invariant violated: ChangeView.NewViewNumber ({view.NewViewNumber}) <= ViewNumber ({view.ViewNumber}).");
                }
            }

            // Invariant 8: Validator indices must be valid
            foreach (var payload in _context.PreparationPayloads.Where(p => p != null))
            {
                var message = _context.GetMessage<ConsensusMessage>(payload);
                if (message != null && message.ValidatorIndex >= _context.Validators.Length)
                {
                    throw new InvalidOperationException($"Invariant violated: Invalid validator index in preparation: {message.ValidatorIndex}");
                }
            }

            foreach (var payload in _context.CommitPayloads.Where(p => p != null))
            {
                var message = _context.GetMessage<ConsensusMessage>(payload);
                if (message != null && message.ValidatorIndex >= _context.Validators.Length)
                {
                    throw new InvalidOperationException($"Invariant violated: Invalid validator index in commit: {message.ValidatorIndex}");
                }
            }

            foreach (var payload in _context.ChangeViewPayloads.Where(p => p != null))
            {
                var message = _context.GetMessage<ConsensusMessage>(payload);
                if (message != null && message.ValidatorIndex >= _context.Validators.Length)
                {
                    throw new InvalidOperationException($"Invariant violated: Invalid validator index in change view: {message.ValidatorIndex}");
                }
            }

            // Invariant 9: Block index consistency
            foreach (var payload in _context.PreparationPayloads.Where(p => p != null))
            {
                var message = _context.GetMessage<ConsensusMessage>(payload);
                if (message != null && message.BlockIndex != _context.Block.Index)
                {
                    throw new InvalidOperationException($"Invariant violated: Block index mismatch in preparation: {message.BlockIndex} != {_context.Block.Index}");
                }
            }

            foreach (var payload in _context.CommitPayloads.Where(p => p != null))
            {
                var message = _context.GetMessage<ConsensusMessage>(payload);
                if (message != null && message.BlockIndex != _context.Block.Index)
                {
                    throw new InvalidOperationException($"Invariant violated: Block index mismatch in commit: {message.BlockIndex} != {_context.Block.Index}");
                }
            }

            LogVerbose("All invariants verified successfully");
        }

        // Verify message fields are within valid ranges
        private static bool VerifyMessageBoundaries(ConsensusMessage message)
        {
            // Basic boundary checks that apply to all message types
            if (message.ValidatorIndex >= _context.Validators.Length)
            {
                LogVerbose($"Invalid ValidatorIndex: {message.ValidatorIndex}, max allowed: {_context.Validators.Length - 1}");
                return false;
            }

            if (message.ViewNumber < 0)
            {
                LogVerbose($"Invalid ViewNumber: {message.ViewNumber}");
                return false;
            }

            // Message-specific boundary checks
            switch (message)
            {
                case Commit commit:
                    if (commit.Signature.IsEmpty || commit.Signature.Length != 64)
                    {
                        LogVerbose($"Invalid Commit signature length: {commit.Signature.Length}");
                        return false;
                    }
                    break;

                case PrepareRequest request:
                    if (request.TransactionHashes == null)
                    {
                        LogVerbose("PrepareRequest has null TransactionHashes");
                        return false;
                    }
                    if (request.TransactionHashes.Length > _system.Settings.MaxTransactionsPerBlock)
                    {
                        LogVerbose($"PrepareRequest has too many transactions: {request.TransactionHashes.Length}");
                        return false;
                    }
                    break;

                case ChangeView view:
                    if (view.NewViewNumber <= 0)
                    {
                        LogVerbose($"ChangeView has invalid NewViewNumber: {view.NewViewNumber}");
                        return false;
                    }
                    break;

                case RecoveryMessage recovery:
                    // Check that dictionaries are not null
                    if (recovery.ChangeViewMessages == null || recovery.PreparationMessages == null || recovery.CommitMessages == null)
                    {
                        LogVerbose("RecoveryMessage has null message dictionaries");
                        return false;
                    }
                    break;
            }

            return true;
        }

        /// <summary>
        /// Track liveness properties across multiple fuzzing runs
        /// </summary>
        private static void TrackLiveness()
        {
            uint blockIndex = _context.Block.Index;

            // Get or create a liveness tracker for this block height
            if (!_livenessTrackers.TryGetValue(blockIndex, out var tracker))
            {
                tracker = new LivenessTracker(blockIndex);
                _livenessTrackers[blockIndex] = tracker;
            }

            // Update the tracker with the current state
            tracker.Update(_context);

            // Check for liveness issues
            if (tracker.ViewChanges >= MaxViewChangesBeforeLivenessIssue)
            {
                throw new InvalidOperationException($"Liveness violation: Block {blockIndex} has experienced {tracker.ViewChanges} view changes without consensus.");
            }

            // If we've reached consensus, log it
            if (tracker.HasReachedConsensus && !tracker.PreviouslyReachedConsensus)
            {
                LogInfo($"Block {blockIndex} reached consensus after {tracker.ViewChanges} view changes.");
            }

            // Clean up old trackers to prevent memory leaks
            // Only keep trackers for the current and previous block
            if (_livenessTrackers.Count > 2)
            {
                var oldKeys = _livenessTrackers.Keys.Where(k => k < blockIndex - 1).ToList();
                foreach (var key in oldKeys)
                {
                    _livenessTrackers.Remove(key);
                }
            }
        }

        /// <summary>
        /// Class to track liveness properties for a specific block height
        /// </summary>
        private class LivenessTracker
        {
            public uint BlockIndex { get; }
            public int ViewChanges { get; private set; }
            public bool HasReachedConsensus { get; private set; }
            public bool PreviouslyReachedConsensus { get; private set; }
            public byte HighestViewNumber { get; private set; }
            public DateTime FirstSeen { get; }
            public DateTime LastUpdated { get; private set; }

            public LivenessTracker(uint blockIndex)
            {
                BlockIndex = blockIndex;
                ViewChanges = 0;
                HasReachedConsensus = false;
                PreviouslyReachedConsensus = false;
                HighestViewNumber = 0;
                FirstSeen = DateTime.UtcNow;
                LastUpdated = FirstSeen;
            }

            public void Update(ConsensusContext context)
            {
                // Track if we previously reached consensus
                PreviouslyReachedConsensus = HasReachedConsensus;

                // Check if we've reached consensus
                int commitCount = context.CommitPayloads.Count(p => p != null && context.GetMessage<Commit>(p)?.ViewNumber == context.ViewNumber);
                bool hasAllTransactions = context.TransactionHashes.All(h => context.Transactions.ContainsKey(h));
                HasReachedConsensus = commitCount >= context.M && hasAllTransactions;

                // Track view changes
                if (context.ViewNumber > HighestViewNumber)
                {
                    ViewChanges += (context.ViewNumber - HighestViewNumber);
                    HighestViewNumber = context.ViewNumber;
                }

                LastUpdated = DateTime.UtcNow;
            }
        }
    }
}
