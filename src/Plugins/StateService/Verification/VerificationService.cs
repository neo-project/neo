// Copyright (C) 2015-2025 The Neo Project.
//
// VerificationService.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Akka.Util.Internal;
using Neo.Extensions;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Plugins.StateService.Network;
using Neo.Wallets;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Neo.Plugins.StateService.Verification
{
    public class VerificationService : UntypedActor
    {
        public class ValidatedRootPersisted { public uint Index; }
        public class BlockPersisted { public uint Index; }
        public const int MaxCachedVerificationProcessCount = 10;
        private class Timer { public uint Index; }
        private static readonly uint TimeoutMilliseconds = StatePlugin._system.Settings.MillisecondsPerBlock;
        private static readonly uint DelayMilliseconds = 3000;
        private readonly Wallet wallet;
        private readonly ConcurrentDictionary<uint, VerificationContext> contexts = new ConcurrentDictionary<uint, VerificationContext>();

        public VerificationService(Wallet wallet)
        {
            this.wallet = wallet;
            StatePlugin._system.ActorSystem.EventStream.Subscribe(Self, typeof(Blockchain.RelayResult));
        }

        private void SendVote(VerificationContext context)
        {
            if (context.VoteMessage is null) return;
            Utility.Log(nameof(VerificationService), LogLevel.Info, $"relay vote, height={context.RootIndex}, retry={context.Retries}");
            StatePlugin._system.Blockchain.Tell(context.VoteMessage);
        }

        private void OnStateRootVote(Vote vote)
        {
            if (contexts.TryGetValue(vote.RootIndex, out VerificationContext context) && context.AddSignature(vote.ValidatorIndex, vote.Signature.ToArray()))
            {
                CheckVotes(context);
            }
        }

        private void CheckVotes(VerificationContext context)
        {
            if (context.IsSender && context.CheckSignatures())
            {
                if (context.StateRootMessage is null) return;
                Utility.Log(nameof(VerificationService), LogLevel.Info, $"relay state root, height={context.StateRoot.Index}, root={context.StateRoot.RootHash}");
                StatePlugin._system.Blockchain.Tell(context.StateRootMessage);
            }
        }

        private void OnBlockPersisted(uint index)
        {
            if (MaxCachedVerificationProcessCount <= contexts.Count)
            {
                contexts.Keys.OrderBy(p => p).Take(contexts.Count - MaxCachedVerificationProcessCount + 1).ForEach(p =>
                {
                    if (contexts.TryRemove(p, out var value))
                    {
                        value.Timer.CancelIfNotNull();
                    }
                });
            }
            var p = new VerificationContext(wallet, index);
            if (p.IsValidator && contexts.TryAdd(index, p))
            {
                p.Timer = Context.System.Scheduler.ScheduleTellOnceCancelable(TimeSpan.FromMilliseconds(DelayMilliseconds), Self, new Timer
                {
                    Index = index,
                }, ActorRefs.NoSender);
                Utility.Log(nameof(VerificationContext), LogLevel.Info, $"new validate process, height={index}, index={p.MyIndex}, ongoing={contexts.Count}");
            }
        }

        private void OnValidatedRootPersisted(uint index)
        {
            Utility.Log(nameof(VerificationService), LogLevel.Info, $"persisted state root, height={index}");
            foreach (var i in contexts.Where(i => i.Key <= index))
            {
                if (contexts.TryRemove(i.Key, out var value))
                {
                    value.Timer.CancelIfNotNull();
                }
            }
        }

        private void OnTimer(uint index)
        {
            if (contexts.TryGetValue(index, out VerificationContext context))
            {
                SendVote(context);
                CheckVotes(context);
                context.Timer.CancelIfNotNull();
                context.Timer = Context.System.Scheduler.ScheduleTellOnceCancelable(TimeSpan.FromMilliseconds(TimeoutMilliseconds << context.Retries), Self, new Timer
                {
                    Index = index,
                }, ActorRefs.NoSender);
                context.Retries++;
            }
        }

        private void OnVoteMessage(ExtensiblePayload payload)
        {
            if (payload.Data.Length == 0) return;
            if ((MessageType)payload.Data.Span[0] != MessageType.Vote) return;
            Vote message;
            try
            {
                message = payload.Data[1..].AsSerializable<Vote>();
            }
            catch (FormatException)
            {
                return;
            }
            OnStateRootVote(message);
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case Vote v:
                    OnStateRootVote(v);
                    break;
                case BlockPersisted bp:
                    OnBlockPersisted(bp.Index);
                    break;
                case ValidatedRootPersisted root:
                    OnValidatedRootPersisted(root.Index);
                    break;
                case Timer timer:
                    OnTimer(timer.Index);
                    break;
                case Blockchain.RelayResult rr:
                    if (rr.Result == VerifyResult.Succeed && rr.Inventory is ExtensiblePayload payload && payload.Category == StatePlugin.StatePayloadCategory)
                    {
                        OnVoteMessage(payload);
                    }
                    break;
                default:
                    break;
            }
        }

        protected override void PostStop()
        {
            base.PostStop();
        }

        public static Props Props(Wallet wallet)
        {
            return Akka.Actor.Props.Create(() => new VerificationService(wallet));
        }
    }
}
