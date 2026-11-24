// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETFRAMEWORK

using System.Windows.Threading;

namespace Microsoft.VisualStudio.Sdk.TestFramework.Xunit.Tests.Shared;

/// <summary>
/// Tests the lifetime of the mocked VS main thread in isolated application domains.
/// </summary>
[Collection(MockedVS.Collection)]
public class GlobalServiceProviderTests
{
    /// <summary>
    /// Verifies that disposing the service provider releases dispatcher-owned resources before its thread exits.
    /// </summary>
    /// <param name="disposeFromMainThread">Whether to dispose on the mocked main thread.</param>
    /// <param name="shutdownFirst">Whether the dispatcher has already been asked to shut down.</param>
    /// <returns>A task representing the isolated lifetime check.</returns>
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Dispose_ShutsDownDispatcherOnOwningThread(bool disposeFromMainThread, bool shutdownFirst)
    {
        // Dispose an isolated fixture twice on the selected thread and verify that WPF cleanup runs exactly once
        // on its owning STA, including when dispatcher shutdown was already requested.
        var setup = new AppDomainSetup
        {
            ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
            ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile,
        };
        AppDomain domain = AppDomain.CreateDomain(nameof(this.Dispose_ShutsDownDispatcherOnOwningThread), null, setup);
        try
        {
            var probe = (DispatcherLifetimeProbe)domain.CreateInstanceAndUnwrap(
                typeof(DispatcherLifetimeProbe).Assembly.FullName,
                typeof(DispatcherLifetimeProbe).FullName);
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(1));
            (bool threadExited, bool shutdownFinished, int startedCount, int finishedCount, int mainThreadId, int startedThreadId, int finishedThreadId) =
                await Task.Run(() => probe.Shutdown(disposeFromMainThread, shutdownFirst), timeout.Token).WithCancellation(timeout.Token);
            Assert.True(threadExited, "The mocked main thread did not exit.");
            Assert.True(shutdownFinished);
            Assert.Equal(1, startedCount);
            Assert.Equal(1, finishedCount);
            Assert.Equal(mainThreadId, startedThreadId);
            Assert.Equal(mainThreadId, finishedThreadId);
        }
        finally
        {
            AppDomain.Unload(domain);
        }
    }

    /// <summary>
    /// Exercises a complete fixture lifetime without changing the test runner's global VS services.
    /// </summary>
    public class DispatcherLifetimeProbe : MarshalByRefObject
    {
        /// <summary>
        /// Disposes the fixture and records dispatcher shutdown.
        /// </summary>
        /// <param name="disposeFromMainThread">Whether to dispose on the mocked main thread.</param>
        /// <param name="shutdownFirst">Whether to request dispatcher shutdown before disposal.</param>
        /// <returns>The thread and dispatcher state after disposal.</returns>
        public (bool ThreadExited, bool ShutdownFinished, int StartedCount, int FinishedCount, int MainThreadId, int StartedThreadId, int FinishedThreadId) Shutdown(bool disposeFromMainThread, bool shutdownFirst)
        {
            var provider = new GlobalServiceProvider();
            Thread mainThread = ThreadHelper.JoinableTaskContext.MainThread;
            int shutdownStartedCount = 0;
            int shutdownFinishedCount = 0;
            int shutdownStartedThreadId = 0;
            int shutdownFinishedThreadId = 0;
            Dispatcher dispatcher = ThreadHelper.JoinableTaskFactory.Run(async delegate
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(CancellationToken.None);
                Dispatcher current = Dispatcher.CurrentDispatcher;
                current.ShutdownStarted += (sender, args) =>
                {
                    shutdownStartedCount++;
                    shutdownStartedThreadId = Thread.CurrentThread.ManagedThreadId;
                };
                current.ShutdownFinished += (sender, args) =>
                {
                    shutdownFinishedCount++;
                    shutdownFinishedThreadId = Thread.CurrentThread.ManagedThreadId;
                };

                if (shutdownFirst)
                {
                    current.InvokeShutdown();
                }

                if (disposeFromMainThread)
                {
                    provider.Dispose();
                    provider.Dispose();
                }

                return current;
            });

            if (!disposeFromMainThread)
            {
                provider.Dispose();
                provider.Dispose();
            }

            bool threadExited = mainThread.Join(TimeSpan.FromSeconds(10));
            return (threadExited, dispatcher.HasShutdownFinished, shutdownStartedCount, shutdownFinishedCount, mainThread.ManagedThreadId, shutdownStartedThreadId, shutdownFinishedThreadId);
        }
    }
}

#endif
