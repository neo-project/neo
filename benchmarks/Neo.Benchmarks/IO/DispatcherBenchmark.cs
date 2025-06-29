// Copyright (C) 2015-2025 The Neo Project.
//
// DispatcherBenchmark.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Akka.Configuration;
using BenchmarkDotNet.Attributes;
using System.Collections.Concurrent;

namespace Neo.IO
{
    //public class DebugConfig : ManualConfig
    //{
    //    public DebugConfig()
    //    {
    //        AddJob(Job.InProcess);
    //        WithOptions(ConfigOptions.DisableOptimizationsValidator);
    //    }
    //}

    //[Config(typeof(DebugConfig))]
    public class DispatcherBenchmark
    {
        public class Message(int value)
        {
            public int Value { get; } = value;
        }

        public class Counter(int count) : IDisposable
        {
            private readonly ConcurrentDictionary<int, bool> _set = [];

            public CountdownEvent CountDown { get; } = new CountdownEvent(count);

            public void Signal(Message msg)
            {
                if (_set.TryAdd(msg.Value, true))
                {
                    CountDown.Signal();
                }
                else
                {
                    // Note: AKKa send duplicate messages
                    // throw new InvalidOperationException($"Duplicate message value: {msg.Value}");
                }
            }

            public void Reset(int messageCount)
            {
                CountDown.Reset(messageCount);
                _set.Clear();
            }

            public void Dispose()
            {
                CountDown.Dispose();
                GC.SuppressFinalize(this);
            }
        }

        public class AkkaMessageActor(Counter countdown) : UntypedActor
        {
            public Counter Counter { get; } = countdown;

            protected override void OnReceive(object message)
            {
                Counter.Signal((Message)message);
            }
        }

        public class NeoMessageHandler(Counter countdown, int workerCount)
            : MessageReceiver<Message>(workerCount)
        {
            public Counter Counter { get; } = countdown;

            public override void OnReceive(Message message)
            {
                Counter.Signal(message);
            }
        }

        [Params(1, 1_000, 10_000)]
        public int MessageCount { get; set; }

        [Params(true, false)]
        public bool MultiThread { get; set; }

        private IActorRef _akkaActor;
        private ActorSystem _akkaSystem;
        private NeoMessageHandler _neoDispatcher;
        private Message[] _messages;
        private Counter _akkaCountdown;
        private Counter _neoCountdown;

        [GlobalSetup]
        public void Setup()
        {
            // Configs

            _messages = new Message[MessageCount];
            for (var i = 0; i < MessageCount; i++)
                _messages[i] = new Message(i);

            var threads = MultiThread ? Environment.ProcessorCount * 2 : 1;

            // Akka setup

            {
                var config = ConfigurationFactory.ParseString($@"
                    akka.actor.default-dispatcher {{
                        type = Dispatcher
                        executor = thread-pool-executor
                        thread-pool-executor {{
                            fixed-pool-size = {threads}
                        }}
                        throughput = 1
                    }}");
                _akkaSystem = ActorSystem.Create("AkkaMessages", config);
                _akkaCountdown = new Counter(MessageCount);
                _akkaActor = _akkaSystem.ActorOf(Props.Create(() => new AkkaMessageActor(_akkaCountdown)));
            }

            // Neo dispatcher setup

            {
                _neoCountdown = new Counter(MessageCount);
                _neoDispatcher = new NeoMessageHandler(_neoCountdown, MultiThread ? threads : 1);
            }
        }

        [GlobalCleanup]
        public async Task Cleanup()
        {
            _neoDispatcher.Dispose();
            await _akkaSystem.Terminate();
        }

        [Benchmark]
        public void Akka()
        {
            _akkaCountdown.Reset(MessageCount);
            foreach (var msg in _messages) _akkaActor.Tell(msg);
            _akkaCountdown.CountDown.Wait(TimeSpan.FromSeconds(500));
        }

        [Benchmark]
        public void MessageReceiver()
        {
            _neoCountdown.Reset(MessageCount);
            _neoDispatcher.Tell(_messages);
            _neoCountdown.CountDown.Wait(TimeSpan.FromMilliseconds(500));
        }
    }
}
