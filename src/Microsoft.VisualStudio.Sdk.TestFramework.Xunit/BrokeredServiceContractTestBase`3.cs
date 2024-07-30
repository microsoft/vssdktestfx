// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO.Pipelines;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Threading;
using Nerdbank.Streams;
using Xunit;

namespace Microsoft.VisualStudio.Sdk.TestFramework;

/// <summary>
/// A base class for testing Visual Studio brokered service contracts with client callbacks.
/// </summary>
/// <typeparam name="TInterface">The service interface.</typeparam>
/// <typeparam name="TServiceMock">The class that mocks the service.</typeparam>
/// <typeparam name="TClientInterfaceMock">The class that mockes the service's client callback.</typeparam>
public abstract class BrokeredServiceContractTestBase<TInterface, TServiceMock, TClientInterfaceMock> : BrokeredServiceContractTestBase<TInterface, TServiceMock>
    where TInterface : class
    where TServiceMock : TInterface, IMockServiceWithClientCallback, new()
    where TClientInterfaceMock : class, new()
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BrokeredServiceContractTestBase{TInterface, TServiceMock, TClientInterfaceMock}"/> class.
    /// </summary>
    /// <param name="logger"><inheritdoc cref="LoggingTestBase(ITestOutputHelper)" path="/param[@name='logger']"/></param>
    /// <param name="serviceRpcDescriptor">The descriptor that the product will use to request or proffer the brokered service.</param>
    public BrokeredServiceContractTestBase(ITestOutputHelper logger, ServiceRpcDescriptor serviceRpcDescriptor)
        : base(logger, serviceRpcDescriptor)
    {
        Assert.NotNull(serviceRpcDescriptor.ClientInterface);
        Assert.Equal(typeof(TClientInterfaceMock), serviceRpcDescriptor.ClientInterface.GetType());

        this.ClientInterface = new TClientInterfaceMock();
    }

    /// <summary>
    /// Gets or sets the mock client callback instance.
    /// </summary>
    public TClientInterfaceMock ClientInterface { get; protected set; }

    /// <inheritdoc/>
    protected override void ConfigureClientCallbackProxy(ServiceRpcDescriptor.RpcConnection clientConnection, ServiceRpcDescriptor.RpcConnection serverConnection)
    {
        Requires.NotNull(serverConnection, nameof(serverConnection));
        Requires.NotNull(clientConnection, nameof(clientConnection));

        base.ConfigureClientCallbackProxy(clientConnection, serverConnection);

        (this.Service as IMockServiceWithClientCallback).ClientCallback = serverConnection.ConstructRpcClient<TClientInterfaceMock>();
        clientConnection.AddLocalRpcTarget(this.ClientInterface);
    }
}
