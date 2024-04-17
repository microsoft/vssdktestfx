// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if WINDOWS || NETFRAMEWORK

using Microsoft.VisualStudio.Sdk.TestFramework.Tests;

[Collection(MockedVS.Collection)]
public class SdkTestUtilitiesTests : LoggingTestBase
{
    public SdkTestUtilitiesTests(GlobalServiceProvider sp, ITestOutputHelper logger)
        : base(logger)
    {
        sp.Reset();
    }

    [Fact]
    public async Task LoadPackageAsync()
    {
        SomePackage package = await SdkTestUtilities.LoadPackageAsync<SomePackage>(this.Logger.WriteLine);
        Assert.NotNull(package);
    }

    [Fact]
    public async Task PromotedPackagesAvailable()
    {
        SomePackage package = await SdkTestUtilities.LoadPackageAsync<SomePackage>(this.Logger.WriteLine);

        Assert.NotNull(await package.GetServiceAsync<SomePackage.SVsPromotedService, object>());
        Assert.NotNull(await AsyncServiceProvider.GlobalProvider.GetServiceAsync<SomePackage.SVsPromotedService, object>());
    }

    [Fact]
    public async Task NonPromotedPackagesAvailableFromPackageOnly()
    {
        SomePackage package = await SdkTestUtilities.LoadPackageAsync<SomePackage>(this.Logger.WriteLine);

        Assert.NotNull(await package.GetServiceAsync(typeof(SomePackage.SVsNonPromotedService)));
        Assert.Null(await AsyncServiceProvider.GlobalProvider.GetServiceAsync(typeof(SomePackage.SVsNonPromotedService)));
    }

    [Fact]
    public async Task ServicesCached()
    {
        SomePackage package = await SdkTestUtilities.LoadPackageAsync<SomePackage>(this.Logger.WriteLine);

        object serviceOne = await package.GetServiceAsync<SomePackage.SVsNonPromotedService, object>();
        object serviceTwo = await package.GetServiceAsync<SomePackage.SVsNonPromotedService, object>();
        Assert.Same(serviceOne, serviceTwo);
    }

    [Fact]
    public async Task GetGlobalServicesViaPackageServiceProvider()
    {
        SomePackage package = await SdkTestUtilities.LoadPackageAsync<SomePackage>(this.Logger.WriteLine);

        Assert.NotNull(await package.GetServiceAsync(typeof(SVsActivityLog)));
    }
}

#endif
