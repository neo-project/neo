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

        public class NeoMessageHandler(CountdownEvent countdown) : IMessageHandler<Message>
        {
            private readonly CountdownEvent _countdown = countdown;

            public void OnMessage(Message message)
            {
                _countdown.Signal();
            }
        }

        private IActorRef _akkaActor;
        private ActorSystem _akkaSystem;
        private MessageDispatcher _neoDispatcher;
        private Message[] _messages;

        private CountdownEvent _akkaCountdown;
        private CountdownEvent _neoCountdown;

        [GlobalSetup]
        public void Setup()
        {
            _messages = new Message[1000];
            for (var i = 0; i < 1000; i++)
                _messages[i] = new Message { Value = i };

            // Akka setup
            _akkaSystem = ActorSystem.Create("benchmark");
            _akkaCountdown = new CountdownEvent(_messages.Length);
            _akkaActor = _akkaSystem.ActorOf(Props.Create(() => new AkkaMessageActor(_akkaCountdown)));

            // Neo dispatcher setup
            _neoDispatcher = new MessageDispatcher(workerCount: 2);
            _neoCountdown = new CountdownEvent(_messages.Length);
            _neoDispatcher.RegisterHandler<Message>(new NeoMessageHandler(_neoCountdown));
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

            _akkaCountdown.Wait();
        }

        [Benchmark]
        public void Neo_Dispatch()
        {
            _akkaCountdown.Reset();
            _neoDispatcher.DispatchAll(_messages);
            _neoCountdown.Wait();
        }
    }
}
