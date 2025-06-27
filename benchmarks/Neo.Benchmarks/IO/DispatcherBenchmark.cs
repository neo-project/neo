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

namespace Neo.IO
{
    public class DispatcherBenchmark
    {
        public class Message
        {
            public int Value { get; set; }
        }

        public class AkkaMessageActor : ReceiveActor
        {
            private readonly CountdownEvent _countdown;

            public AkkaMessageActor(CountdownEvent countdown)
            {
                _countdown = countdown;

                Receive<Message>(_ =>
                {
                    _countdown.Signal();
                });
            }
        }

        public class NeoMessageHandler(CountdownEvent countdown, int workerCount)
            : MessageReceiver<Message>(workerCount)
        {
            private readonly CountdownEvent _countdown = countdown;

            public override Task OnMessageAsync(Message message)
            {
                _countdown.Signal();
                return Task.CompletedTask;
            }
        }

        [Params(1, 1000, 10000)]
        public int MessageCount { get; set; }

        [Params(true, false)]
        public bool MultiThread { get; set; }

        private IActorRef _akkaActor;
        private ActorSystem _akkaSystem;
        private NeoMessageHandler _neoDispatcher;
        private Message[] _messages;

        private CountdownEvent _akkaCountdown;
        private CountdownEvent _neoCountdown;

        [GlobalSetup]
        public void Setup()
        {
            _messages = new Message[MessageCount];
            for (var i = 0; i < _messages.Length; i++)
                _messages[i] = new Message { Value = i };

            // Akka setup
            _akkaCountdown = new CountdownEvent(_messages.Length);

            var threads = MultiThread ? Environment.ProcessorCount * 2 : 1;

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
            _akkaActor = _akkaSystem.ActorOf(Props.Create(() => new AkkaMessageActor(_akkaCountdown)));

            // Neo dispatcher setup
            _neoCountdown = new CountdownEvent(_messages.Length);
            _neoDispatcher = new NeoMessageHandler(_neoCountdown, MultiThread ? threads : 1);
        }

        [GlobalCleanup]
        public async Task Cleanup()
        {
            _neoDispatcher.Dispose();
            await _akkaSystem.Terminate();
        }

        [Benchmark]
        public void Akka_Send()
        {
            _akkaCountdown.Reset();

            foreach (var msg in _messages)
                _akkaActor.Tell(msg);

            _akkaCountdown.Wait(TimeSpan.FromSeconds(1));
        }

        [Benchmark]
        public void Neo_Dispatch()
        {
            _neoCountdown.Reset();

            _neoDispatcher.TellAll(_messages);

            _neoCountdown.Wait(TimeSpan.FromSeconds(1));
        }
    }
}
