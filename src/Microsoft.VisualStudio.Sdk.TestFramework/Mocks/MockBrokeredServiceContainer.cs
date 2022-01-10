// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Sdk.TestFramework;

using System.IO.Pipelines;
using Microsoft.ServiceHub.Framework;
using Microsoft.ServiceHub.Framework.Services;
using Microsoft.VisualStudio.Shell.ServiceBroker;

/// <summary>
/// A mock for the <see cref="SVsBrokeredServiceContainer"/> service.
/// </summary>
internal class MockBrokeredServiceContainer : IBrokeredServiceContainer
{
    private readonly Dictionary<ServiceMoniker, object> profferedServices = new Dictionary<ServiceMoniker, object>();

    /// <inheritdoc/>
    public IDisposable Proffer(ServiceRpcDescriptor serviceDescriptor, BrokeredServiceFactory factory)
    {
        this.profferedServices.Add(serviceDescriptor.Moniker, factory);
        return new DisposableAction(this, serviceDescriptor.Moniker);
    }

    /// <inheritdoc/>
    public IDisposable Proffer(ServiceRpcDescriptor serviceDescriptor, AuthorizingBrokeredServiceFactory factory)
    {
        this.profferedServices.Add(serviceDescriptor.Moniker, factory);
        return new DisposableAction(this, serviceDescriptor.Moniker);
    }

    /// <inheritdoc/>
    public IServiceBroker GetFullAccessServiceBroker() => new MockServiceBroker(this);

    /// <summary>
    /// Clears all proffered services.
    /// </summary>
    internal void Reset()
    {
        this.profferedServices.Clear();
    }

    private class DisposableAction : IDisposable
    {
        private readonly MockBrokeredServiceContainer container;
        private readonly ServiceMoniker moniker;
        private bool disposed;

        internal DisposableAction(MockBrokeredServiceContainer container, ServiceMoniker moniker)
        {
            this.container = container;
            this.moniker = moniker;
        }

        public void Dispose()
        {
            if (!this.disposed)
            {
                this.container.profferedServices.Remove(this.moniker);
                this.disposed = true;
            }
        }
    }

    private class MockServiceBroker : IServiceBroker
    {
        private readonly MockBrokeredServiceContainer container;

        internal MockServiceBroker(MockBrokeredServiceContainer container)
        {
            this.container = container;
        }

        public event EventHandler<BrokeredServicesChangedEventArgs>? AvailabilityChanged;

        public ValueTask<IDuplexPipe?> GetPipeAsync(ServiceMoniker serviceMoniker, ServiceActivationOptions options = default, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async ValueTask<T?> GetProxyAsync<T>(ServiceRpcDescriptor serviceDescriptor, ServiceActivationOptions options = default, CancellationToken cancellationToken = default)
            where T : class
        {
            if (this.container.profferedServices.TryGetValue(serviceDescriptor.Moniker, out object? factory))
            {
                if (factory is BrokeredServiceFactory fac1)
                {
                    T? service = (T?)await fac1(serviceDescriptor.Moniker, options, this, cancellationToken).ConfigureAwait(false);
                    if (service is object)
                    {
                        return serviceDescriptor.ConstructLocalProxy<T>(service);
                    }
                }
                else if (factory is AuthorizingBrokeredServiceFactory fac2)
                {
#pragma warning disable CA2000 // Dispose objects before losing scope
                    T? service = (T?)await fac2(serviceDescriptor.Moniker, options, this, new AuthorizationServiceClient(new MockAuthorizationService()), cancellationToken).ConfigureAwait(false);
#pragma warning restore CA2000 // Dispose objects before losing scope
                    if (service is object)
                    {
                        return serviceDescriptor.ConstructLocalProxy<T>(service);
                    }
                }
                else
                {
                    throw Assumes.NotReachable();
                }
            }

            return default;
        }

        protected virtual void OnAvailabilityChanged(BrokeredServicesChangedEventArgs args) => this.AvailabilityChanged?.Invoke(this, args);
    }

    private class MockAuthorizationService : IAuthorizationService
    {
        public event EventHandler? CredentialsChanged;

        public event EventHandler? AuthorizationChanged;

        public ValueTask<bool> CheckAuthorizationAsync(ProtectedOperation operation, CancellationToken cancellationToken = default) => new ValueTask<bool>(true);

        public ValueTask<IReadOnlyDictionary<string, string>> GetCredentialsAsync(CancellationToken cancellationToken = default) => new ValueTask<IReadOnlyDictionary<string, string>>(ImmutableDictionary<string, string>.Empty);

        protected virtual void OnCredentialsChanged() => this.CredentialsChanged?.Invoke(this, EventArgs.Empty);

        protected virtual void OnAuthorizationChanged() => this.AuthorizationChanged?.Invoke(this, EventArgs.Empty);
    }
}
