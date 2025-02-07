// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Composition;

[Collection(MockedVS.Collection)]
public class MefHostingTests
{
    private readonly MefHostingFixture mefHosting;

    public MefHostingTests(MefHostingFixture mefHosting)
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

    [Export]
    [PartNotDiscoverable]
    private class SomePart
    {
    }
}
