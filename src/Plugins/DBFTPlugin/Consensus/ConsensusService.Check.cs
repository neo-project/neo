// Copyright (C) 2015-2024 The Neo Project.
//
// ConsensusService.Check.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Neo.IO;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using System;
using System.Linq;

namespace Neo.Consensus
{
    partial class ConsensusService
    {
        private bool CheckPrepareResponse(uint pId)
        {
            if (context.TransactionHashes[pId].Length == context.Transactions[pId].Count)
            {
                // if we are the primary for this view, but acting as a backup because we recovered our own
                // previously sent prepare request, then we don't want to send a prepare response.
                if ((pId == 0 && context.IsPriorityPrimary) || (pId == 1 && context.IsFallbackPrimary) || context.WatchOnly) return true;

                // Check maximum block size via Native Contract policy
                if (context.GetExpectedBlockSize(pId) > dbftSettings.MaxBlockSize)
                {
                    Log($"Rejected block: {context.Block[pId].Index} The size exceed the policy", LogLevel.Warning);
                    RequestChangeView(ChangeViewReason.BlockRejectedByPolicy);
                    return false;
                }
                // Check maximum block system fee via Native Contract policy
                if (context.GetExpectedBlockSystemFee(pId) > dbftSettings.MaxBlockSystemFee)
                {
                    Log($"Rejected block: {context.Block[pId].Index} The system fee exceed the policy", LogLevel.Warning);
                    RequestChangeView(ChangeViewReason.BlockRejectedByPolicy);
                    return false;
                }

                // Timeout extension due to prepare response sent
                // around 2*15/M=30.0/5 ~ 40% block time (for M=5)
                ExtendTimerByFactor(2);

                Log($"Sending {nameof(PrepareResponse)}");
                localNode.Tell(new LocalNode.SendDirectly { Inventory = context.MakePrepareResponse(pId) });
                CheckPreparations(pId);
            }
            return true;
        }

        private void CheckPreCommits(uint pId, bool forced = false)
        {
            if (forced || context.PreCommitPayloads[pId].Count(p => p != null) >= context.M && context.TransactionHashes[pId].All(p => context.Transactions[pId].ContainsKey(p)))
            {
                ExtensiblePayload payload = context.MakeCommit(pId);
                Log($"Sending {nameof(Commit)} to pId={pId}");
                context.Save();
                localNode.Tell(new LocalNode.SendDirectly { Inventory = payload });
                // Set timer, so we will resend the commit in case of a networking issue
                ChangeTimer(TimeSpan.FromMilliseconds(neoSystem.Settings.MillisecondsPerBlock));
                CheckCommits(pId);
            }
        }

        private void CheckCommits(uint pId)
        {
            if (context.CommitPayloads[pId].Count(p => context.GetMessage(p)?.ViewNumber == context.ViewNumber) >= context.M && context.TransactionHashes[pId].All(p => context.Transactions[pId].ContainsKey(p)))
            {
                block_received_index = context.Block[pId].Index;
                block_received_time = TimeProvider.Current.UtcNow;
                Block block = context.CreateBlock(pId);
                Log($"Sending {nameof(Block)}: height={block.Index} hash={block.Hash} tx={block.Transactions.Length} Id={pId}");
                blockchain.Tell(block);
                return;
            }
        }

        private void CheckExpectedView(byte viewNumber)
        {
            if (context.ViewNumber >= viewNumber) return;
            var messages = context.ChangeViewPayloads.Select(p => context.GetMessage<ChangeView>(p)).ToArray();
            // if there are `M` change view payloads with NewViewNumber greater than viewNumber, then, it is safe to move
            if (messages.Count(p => p != null && p.NewViewNumber >= viewNumber) >= context.M)
            {
                if (!context.WatchOnly)
                {
                    ChangeView message = messages[context.MyIndex];
                    // Communicate the network about my agreement to move to `viewNumber`
                    // if my last change view payload, `message`, has NewViewNumber lower than current view to change
                    if (message is null || message.NewViewNumber < viewNumber)
                        localNode.Tell(new LocalNode.SendDirectly { Inventory = context.MakeChangeView(ChangeViewReason.ChangeAgreement) });
                }
                InitializeConsensus(viewNumber);
            }
        }

        private void CheckPreparations(uint pId)
        {
            if (context.TransactionHashes[pId].All(p => context.Transactions[pId].ContainsKey(p)))
            {
                var preparationsCount = context.PreparationPayloads[pId].Count(p => p != null);
                if (context.ViewNumber > 0)
                {
                    if (preparationsCount >= context.M)
                        CheckPreCommits(0, true);
                    return;
                }
                if (!context.PreCommitSent
                    && ((pId == 0 && preparationsCount >= context.F + 1)
                        || (pId == 1 && preparationsCount >= context.M)))
                {
                    ExtensiblePayload payload = context.MakePreCommit(pId);
                    Log($"Sending {nameof(PreCommit)} to pId={pId}");
                    context.Save();
                    localNode.Tell(new LocalNode.SendDirectly { Inventory = payload });
                    // Set timer, so we will resend the commit in case of a networking issue
                    ChangeTimer(TimeSpan.FromMilliseconds(neoSystem.Settings.MillisecondsPerBlock));
                    CheckPreCommits(pId);
                }
                if (context.ViewNumber == 0 && pId == 0 && preparationsCount >= context.M)
                {
                    CheckPreCommits(0, true);
                }
            }
        }
    }
}
