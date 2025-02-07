// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETFRAMEWORK || WINDOWS

#pragma warning disable VSSDK006 // Check services exist

using NSubstitute;

public class ChildServiceProviderTests
{
    private readonly IServiceProvider parentServiceProvider;

    public ChildServiceProviderTests()
    {
        this.parentServiceProvider = Substitute.For<IServiceProvider>();
        _ = this.parentServiceProvider.GetService(typeof(SVsSolution)).Returns(Substitute.For<IVsSolution>());
    }

    [Fact]
    public void OffersOwnServices()
    {
        var childServiceProvider = new ChildServiceProvider(this.parentServiceProvider);
        childServiceProvider.AddService(typeof(IVsProject), Substitute.For<IVsProject>());
        Assert.NotNull(childServiceProvider.GetService(typeof(IVsProject)));
    }

    [Fact]
    public void OffersParentServices()
    {
        var childServiceProvider = new ChildServiceProvider(this.parentServiceProvider);
        Assert.IsAssignableFrom<IVsSolution>(childServiceProvider.GetService(typeof(SVsSolution)));
    }
}

#endif
