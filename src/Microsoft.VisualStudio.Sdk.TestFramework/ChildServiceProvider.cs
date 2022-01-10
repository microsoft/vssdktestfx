// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Sdk.TestFramework;

using System;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Shell;

/// <summary>
/// An <see cref="IServiceProvider"/> implementation that inherits services
/// from another.
/// </summary>
public class ChildServiceProvider : IServiceProvider
{
    /// <summary>
    /// The parent service provider to call when we're requested
    /// for services we don't have locally.
    /// </summary>
    private readonly IServiceProvider parent;

    /// <summary>
    /// The local services that have been added to this instance.
    /// </summary>
    private ImmutableDictionary<Type, object> services = ImmutableDictionary<Type, object>.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChildServiceProvider"/> class.
    /// </summary>
    public ChildServiceProvider()
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
        : this(ServiceProvider.GlobalProvider)
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChildServiceProvider"/> class.
    /// </summary>
    /// <param name="globalServiceProvider">The global service provider.</param>
    public ChildServiceProvider(IServiceProvider globalServiceProvider)
    {
        Requires.NotNull(globalServiceProvider, nameof(globalServiceProvider));

        this.parent = globalServiceProvider;
    }

    /// <summary>
    /// Adds a service to the service provider.
    /// </summary>
    /// <param name="serviceType">The service type identity.</param>
    /// <param name="service">The service instance.</param>
    public void AddService(Type serviceType, object service)
    {
        if (!ImmutableInterlocked.TryAdd(ref this.services, serviceType, service))
        {
            throw new InvalidOperationException(Strings.ServiceAlreadyAdded);
        }
    }

    /// <inheritdoc />
    public object GetService(Type serviceType)
    {
        Requires.NotNull(serviceType, nameof(serviceType));

        if (!this.services.TryGetValue(serviceType, out object? service))
        {
            service = this.parent.GetService(serviceType);
        }

        return service;
    }
}
