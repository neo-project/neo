// Copyright (C) 2015-2024 The Neo Project.
//
// NamedPipeServicePlugin.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Threading;
using System.Threading.Tasks;
using static System.IO.Path;

namespace Neo.Plugins
{
    public sealed class NamedPipeServicePlugin : Plugin
    {
        private readonly SemaphoreSlim _bindSemaphore = new(1);
        private readonly CancellationTokenSource _stopTokenSource = new();
        private readonly TaskCompletionSource _stoppedCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private NeoSystem? _system;
        private NamedPipeServerListener? _listener;
        private NamedPipeServiceSettings _settings = NamedPipeServiceSettings.Default;

        private bool _hasStarted;
        private int _stopping;

        #region Overrides

        public override string ConfigFile => Combine(RootPath, "NamedPipeService.json");

        public override string Name => "NamedPipeService";

        public override string Description => "Allows communication with the node over NamedPipes";

        protected override UnhandledExceptionPolicy ExceptionPolicy { get; init; } = UnhandledExceptionPolicy.Ignore;


        #endregion

        public override void Dispose()
        {
            StopAsync(new CancellationToken(true)).GetAwaiter().GetResult();
        }

        protected override void Configure()
        {
            _settings = NamedPipeServiceSettings.Load(GetConfiguration());
        }

        protected override void OnSystemLoaded(NeoSystem system)
        {
            if (_hasStarted)
                throw new InvalidOperationException($"{nameof(NamedPipeServicePlugin)} has already been started.");

            _hasStarted = true;
            _system ??= system;
            _listener ??= new(_settings.PipeName, _settings.TransportOptions);

            _listener.Start();

            _ = ProcessClientsAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (Interlocked.Exchange(ref _stopping, 1) == 1)
            {
                await _stoppedCompletionSource.Task.ConfigureAwait(false);
                return;
            }

            _stopTokenSource.Cancel();

#pragma warning disable CA2016 // Don't use cancellationToken when acquiring the semaphore. Dispose calls this with a pre-canceled token.
            await _bindSemaphore.WaitAsync().ConfigureAwait(false);
#pragma warning restore CA2016

            try
            {
                await _listener!.UnbindAsync(new CancellationToken(true)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _stoppedCompletionSource.TrySetException(ex);
                throw;
            }
            finally
            {
                _stopTokenSource.Dispose();
                _bindSemaphore.Release();
            }

            _stoppedCompletionSource.TrySetResult();
        }

        private async Task ProcessClientsAsync()
        {
            var stoppingToken = _stopTokenSource.Token;
            await _bindSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                if (_stopping == 1)
                    throw new InvalidOperationException($"{nameof(NamedPipeServicePlugin)} has already been stopped.");

                if (_system is null || _listener is null)
                    throw new InvalidOperationException($"{nameof(NamedPipeServicePlugin)} has not been started.");

                while (true)
                {
                    var connection = await _listener.AcceptAsync(stoppingToken).ConfigureAwait(false);

                    if (stoppingToken.IsCancellationRequested)
                        break;

                    if (connection is null)
                        continue;

                    var threadPoolItem = new NamedPipeServerConnectionThread(_system, connection);
                    ThreadPool.UnsafeQueueUserWorkItem(threadPoolItem, preferLocal: false);
                }
            }
            finally
            {

                _bindSemaphore.Release();
            }
        }
    }
}
