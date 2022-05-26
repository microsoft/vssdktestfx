// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETFRAMEWORK || WINDOWS

using Moq;

public class ChildServiceProviderTests
{
    private readonly IServiceProvider parentServiceProvider;

    public ChildServiceProviderTests()
    {
        var parentSP = new Mock<IServiceProvider>();
        parentSP.Setup(sp => sp.GetService(typeof(SVsSolution)))
            .Returns(new Mock<IVsSolution>().Object);
        this.parentServiceProvider = parentSP.Object;
    }

    [Fact]
    public void OffersOwnServices()
    {
        var childServiceProvider = new ChildServiceProvider(this.parentServiceProvider);
        childServiceProvider.AddService(typeof(IVsProject), new Mock<IVsProject>().Object);
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
