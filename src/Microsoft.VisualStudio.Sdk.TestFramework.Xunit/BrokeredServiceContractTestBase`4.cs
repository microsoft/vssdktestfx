// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.ServiceHub.Framework;

namespace Microsoft.VisualStudio.Sdk.TestFramework;

/// <summary>
/// A base class for testing Visual Studio brokered service contracts with client callbacks.
/// </summary>
/// <typeparam name="TInterface">The service interface.</typeparam>
/// <typeparam name="TServiceMock">The class that mocks the service.</typeparam>
/// <typeparam name="TClientInterface">The interface of the client callback.</typeparam>
/// <typeparam name="TClientMock">The class that mocks the service's client callback.</typeparam>
public abstract class BrokeredServiceContractTestBase<TInterface, TServiceMock, TClientInterface, TClientMock> : BrokeredServiceContractTestBase<TInterface, TServiceMock>
    where TInterface : class
    where TServiceMock : TInterface, IMockServiceWithClientCallback, new()
    where TClientInterface : class
    where TClientMock : TClientInterface, new()
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BrokeredServiceContractTestBase{TInterface, TServiceMock, TClientInterfaceMock}"/> class.
    /// </summary>
    /// <param name="logger"><inheritdoc cref="LoggingTestBase(ITestOutputHelper)" path="/param[@name='logger']"/></param>
    /// <param name="serviceRpcDescriptor">The descriptor that the product will use to request or proffer the brokered service.</param>
    public BrokeredServiceContractTestBase(ITestOutputHelper logger, ServiceRpcDescriptor serviceRpcDescriptor)
        : base(logger, serviceRpcDescriptor)
    {
        Requires.Argument(serviceRpcDescriptor.ClientInterface?.IsAssignableFrom(typeof(TClientMock)) is true, nameof(serviceRpcDescriptor), $"The type identified by the {nameof(ServiceRpcDescriptor.ClientInterface)} property must be assignable from the {nameof(TClientMock)} generic type argument.");

        this.ClientCallback = new TClientMock();
    }

    /// <summary>
    /// Gets or sets the mock client callback instance.
    /// </summary>
    public TClientMock ClientCallback { get; protected set; }

    /// <inheritdoc/>
    protected override void ConfigureRpcConnections(ServiceRpcDescriptor.RpcConnection clientConnection, ServiceRpcDescriptor.RpcConnection serverConnection)
    {
        base.ConfigureRpcConnections(clientConnection, serverConnection);

        this.Service.ClientCallback = serverConnection.ConstructRpcClient<TClientInterface>();
        clientConnection.AddLocalRpcTarget(this.ClientCallback);
    }
}
