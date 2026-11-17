// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Composition;

[Collection(MockedVS.Collection)]
public class MefHostingTests : LoggingTestBase
{
    private readonly MefHostingFixture mefHosting;

    public MefHostingTests(MefHostingFixture mefHosting, ITestOutputHelper logger)
        : base(logger)
    {
        this.mefHosting = mefHosting;
    }

    [Fact]
    public async Task CreateExportProviderAsync_TypeArray()
    {
        ExportProvider exportProvider = await MefHosting.CreateExportProviderAsync(typeof(SomePart));
        SomePart part = exportProvider.GetExportedValue<SomePart>();
        Assert.NotNull(part);
    }

    [Fact]
    public async Task GetConfiguration()
    {
        Assert.NotNull(await this.mefHosting.GetConfigurationAsync());
    }

    [Fact]
    public async Task GetCatalog()
    {
        Assert.NotNull(await this.mefHosting.GetCatalogAsync());
    }

#if NETFRAMEWORK || WINDOWS
    [Fact]
    public async Task SynchronousDisposalJoinsAsyncParts()
    {
        ExportProvider provider = await this.mefHosting.CreateExportProviderAsync();
        AsyncDisposablePart part = provider.GetExportedValue<AsyncDisposablePart>();
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(CancellationToken);
        provider.Dispose();
        Assert.True(part.Disposed);
    }

    [Export]
    private class AsyncDisposablePart : System.IAsyncDisposable
    {
        public bool Disposed { get; private set; }

        public async ValueTask DisposeAsync()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await TaskScheduler.Default;
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(timeout.Token);
            this.Disposed = true;
        }
    }
#endif

    [Export]
    [PartNotDiscoverable]
    private class SomePart
    {
    }
}
