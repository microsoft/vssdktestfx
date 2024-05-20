// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETFRAMEWORK || WINDOWS

namespace Microsoft.VisualStudio.Sdk.TestFramework.Mocks;

/// <summary>
/// An implementation of <see cref="IAsyncServiceProvider2"/>
/// that simply returns services from <see cref="OLE.Interop.IServiceProvider"/>.
/// </summary>
internal class MockAsyncServiceProvider : IAsyncServiceProvider3, Shell.Interop.COMAsyncServiceProvider.IAsyncServiceProvider
{
    private readonly OLE.Interop.IServiceProvider serviceProvider;
    private readonly IVsTaskSchedulerService taskSchedulerService;

    /// <summary>
    /// Initializes a new instance of the <see cref="MockAsyncServiceProvider"/> class.
    /// </summary>
    /// <param name="serviceProvider">The root of all services.</param>
    internal MockAsyncServiceProvider(OLE.Interop.IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        this.taskSchedulerService = (IVsTaskSchedulerService)serviceProvider.QueryService(typeof(SVsTaskSchedulerService).GUID);
        Assumes.Present(this.taskSchedulerService);
    }

    /// <inheritdoc/>
    public Task<object?> GetServiceAsync(Type serviceType, bool swallowExceptions)
    {
        return this.GetServiceAsync(serviceType);
    }

    /// <inheritdoc/>
    public Task<object?> GetServiceAsync(Type serviceType)
    {
        Requires.NotNull(serviceType, nameof(serviceType));
        return Task.FromResult<object?>(this.serviceProvider.QueryService(serviceType.GUID));
    }

    /// <inheritdoc />
    public Task<TInterface?> GetServiceAsync<TService, TInterface>(bool throwOnFailure, CancellationToken cancellationToken)
        where TInterface : class
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<TInterface?>(cancellationToken);
        }

        var service = this.serviceProvider.QueryService(typeof(TService).GUID);

        if (service is not TInterface @interface)
        {
            if (throwOnFailure)
            {
                throw new ServiceUnavailableException(service is null ? typeof(TService) : typeof(TInterface));
            }

            return Task.FromResult<TInterface?>(null);
        }

        return Task.FromResult<TInterface?>(@interface);
    }

    /// <inheritdoc />
    public IVsTask QueryServiceAsync(ref Guid guidService)
    {
        IVsTaskCompletionSource completionSource = this.taskSchedulerService.CreateTaskCompletionSource();
        try
        {
            completionSource.SetResult(this.serviceProvider.QueryService(guidService));
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
        {
            completionSource.SetFaulted(Marshal.GetHRForException(ex));
        }

        return completionSource.Task;
    }
}

#endif
