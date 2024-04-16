// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if WINDOWS || NETFRAMEWORK

#pragma warning disable SA1302 // Interface names should begin with I

using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.Sdk.TestFramework.Tests;

public class SomePackage : AsyncPackage
{
    [Guid("7FEA8105-6990-4A36-A301-36EE8BA1D02F")]
    public interface SVsPromotedService
    {
    }

    [Guid("73C85F42-723D-44E4-95D8-F3599D57596F")]
    public interface SVsNonPromotedService
    {
    }

    protected override Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        this.AddService(typeof(SVsPromotedService), (sc, ct, serviceType) => Task.FromResult<object?>(new object()), true);
        this.AddService(typeof(SVsNonPromotedService), (sc, ct, serviceType) => Task.FromResult<object?>(new object()), false);

        return Task.CompletedTask;
    }
}

#endif
