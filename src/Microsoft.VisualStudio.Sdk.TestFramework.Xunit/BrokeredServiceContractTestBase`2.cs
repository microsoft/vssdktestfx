// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO.Pipelines;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Threading;
using Nerdbank.Streams;

namespace Microsoft.VisualStudio.Sdk.TestFramework;

/// <summary>
/// A base class for testing Visual Studio brokered service contracts.
/// </summary>
/// <typeparam name="TInterface">The service interface.</typeparam>
/// <typeparam name="TServiceMock">The class that mocks the service.</typeparam>
public abstract class BrokeredServiceContractTestBase<TInterface, TServiceMock> : LoggingTestBase, IAsyncLifetime
    where TInterface : class
    where TServiceMock : TInterface, new()
{
    /// <summary>
    /// Provides a unique number for each test that can be included in the log message.
    /// </summary>
    /// <remarks>
    /// Test logs can get misattributed to the wrong test, and prefixing each logged message with a number makes this detectable.
    /// See <see href="https://github.com/microsoft/vstest/issues/3047">this bug</see>.
    /// </remarks>
    private static int testCounter;

    private MultiplexingStream clientMxStream;
    private MultiplexingStream serviceMxStream;

#pragma warning disable CS8618 // We initialize non-nullable fields in InitializeAsync
    /// <summary>
    /// Initializes a new instance of the <see cref="BrokeredServiceContractTestBase{TInterface, TServiceMock}"/> class.
    /// </summary>
    /// <param name="logger"><inheritdoc cref="LoggingTestBase(ITestOutputHelper)" path="/param[@name='logger']"/></param>
    /// <param name="serviceRpcDescriptor">The descriptor that the product will use to request or proffer the brokered service.</param>
    public BrokeredServiceContractTestBase(ITestOutputHelper logger, ServiceRpcDescriptor serviceRpcDescriptor)
#pragma warning restore CS8618 // We initialize non-nullable fields in InitializeAsync
        : base(logger)
    {
        this.Descriptor = serviceRpcDescriptor ?? throw new ArgumentNullException(nameof(serviceRpcDescriptor));
        this.Service = new TServiceMock();
    }

    /// <summary>
    /// Gets the descriptor that the product will use to request or proffer the brokered service.
    /// </summary>
    public ServiceRpcDescriptor Descriptor { get; }

    /// <summary>
    /// Gets or sets the mock service implementation.
    /// </summary>
    public TServiceMock Service { get; protected set; }

    /// <summary>
    /// Gets or sets the client proxy the test will use to interact with the mock <see cref="Service"/>.
    /// </summary>
    public TInterface ClientProxy { get; protected set; }

    /// <summary>
    /// Gets or sets a value indicating whether convention tests defined on the <see cref="BrokeredServiceContractTestBase{TInterface, TServiceMock}"/> base class should run as part of the derived test class.
    /// </summary>
    /// <value>The default value is <see langword="true"/>.</value>
    /// <remarks>
    /// Derived test classes that want to disable default tests should set this property to <see langword="false" /> in their constructor.
    /// </remarks>
    protected bool DefaultTestsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the verbosity level to use for logging messages related to the <see cref="MultiplexingStream"/>.
    /// </summary>
    protected SourceLevels MultiplexingLoggingVerbosity { get; set; } = SourceLevels.Warning;

    /// <summary>
    /// Gets or sets the verbosity level to use for logging messages related to the RPC calls between client and service.
    /// </summary>
    protected SourceLevels DescriptorLoggingVerbosity { get; set; } = SourceLevels.Verbose;

    /// <inheritdoc/>
    public virtual async Task InitializeAsync()
    {
        int testId = Interlocked.Increment(ref testCounter);
        Func<string, SourceLevels, TraceSource> traceSourceFactory = (name, verbosity) =>
            new TraceSource(name)
            {
                Switch = { Level = verbosity },
                Listeners =
                {
                    new XunitTraceListener(this.Logger, testId, this.TestStopwatch),
                },
            };

        ServiceRpcDescriptor.RpcConnection clientConnection;
        ServiceRpcDescriptor.RpcConnection serverConnection;
        if (this.Descriptor is ServiceJsonRpcDescriptor { MultiplexingStreamOptions: object } descriptor)
        {
            // This is a V3 descriptor, which sets up its own MultiplexingStream.
            (IDuplexPipe, IDuplexPipe) underlyingPipes = FullDuplexStream.CreatePipePair();
            clientConnection = descriptor
                .WithMultiplexingStream(new MultiplexingStream.Options(descriptor.MultiplexingStreamOptions)
                {
                    TraceSource = traceSourceFactory("Client mxstream", this.MultiplexingLoggingVerbosity),
                    DefaultChannelTraceSourceFactoryWithQualifier = (id, name) => traceSourceFactory($"Client mxstream {id} (\"{name}\")", this.MultiplexingLoggingVerbosity),
                })
                .WithTraceSource(traceSourceFactory("Client RPC", this.DescriptorLoggingVerbosity))
                .ConstructRpcConnection(underlyingPipes.Item1);
            serverConnection = descriptor
                .WithMultiplexingStream(new MultiplexingStream.Options(descriptor.MultiplexingStreamOptions)
                {
                    TraceSource = traceSourceFactory("Server mxstream", this.MultiplexingLoggingVerbosity),
                    DefaultChannelTraceSourceFactoryWithQualifier = (id, name) => traceSourceFactory($"Server mxstream {id} (\"{name}\")", this.MultiplexingLoggingVerbosity),
                })
                .WithTraceSource(traceSourceFactory("Server RPC", this.DescriptorLoggingVerbosity))
                .ConstructRpcConnection(underlyingPipes.Item2);
        }
        else
        {
            // This is an older descriptor that we have to set up the multiplexing stream ourselves for.
            (Stream, Stream) underlyingStreams = FullDuplexStream.CreatePair();
            Task<MultiplexingStream> mxStreamTask1 = MultiplexingStream.CreateAsync(
                underlyingStreams.Item1,
                new MultiplexingStream.Options
                {
                    TraceSource = traceSourceFactory("Client mxstream", this.MultiplexingLoggingVerbosity),
                    DefaultChannelTraceSourceFactoryWithQualifier = (id, name) => traceSourceFactory($"Client mxstream {id} (\"{name}\")", this.MultiplexingLoggingVerbosity),
                },
                this.TimeoutToken);
            Task<MultiplexingStream> mxStreamTask2 = MultiplexingStream.CreateAsync(
                underlyingStreams.Item2,
                new MultiplexingStream.Options
                {
                    TraceSource = traceSourceFactory("Server mxstream", this.MultiplexingLoggingVerbosity),
                    DefaultChannelTraceSourceFactoryWithQualifier = (id, name) => traceSourceFactory($"Server mxstream {id} (\"{name}\")", this.MultiplexingLoggingVerbosity),
                },
                this.TimeoutToken);
            MultiplexingStream[] mxStreams = await Task.WhenAll(mxStreamTask1, mxStreamTask2);
            this.clientMxStream = mxStreams[0];
            this.serviceMxStream = mxStreams[1];

            Task<MultiplexingStream.Channel> offerTask = mxStreams[0].OfferChannelAsync(string.Empty, this.TimeoutToken);
            Task<MultiplexingStream.Channel> acceptTask = mxStreams[1].AcceptChannelAsync(string.Empty, this.TimeoutToken);
            MultiplexingStream.Channel[] channels = await Task.WhenAll(offerTask, acceptTask);

#pragma warning disable CS0618 // Type or member is obsolete
            clientConnection = this.Descriptor
                .WithTraceSource(traceSourceFactory("Client RPC", this.DescriptorLoggingVerbosity))
                .WithMultiplexingStream(mxStreams[0])
                .ConstructRpcConnection(channels[0]);
            serverConnection = this.Descriptor
                .WithTraceSource(traceSourceFactory("Server RPC", this.DescriptorLoggingVerbosity))
                .WithMultiplexingStream(mxStreams[1])
                .ConstructRpcConnection(channels[1]);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        this.ConfigureRpcConnections(clientConnection, serverConnection);

        clientConnection.StartListening();
        serverConnection.StartListening();
    }

    /// <inheritdoc/>
    public virtual async Task DisposeAsync()
    {
        (this.ClientProxy as IDisposable)?.Dispose();

        List<Task> tasks = new();
        if (this.clientMxStream is not null)
        {
            tasks.Add(this.clientMxStream.DisposeAsync().AsTask());
            tasks.Add(this.clientMxStream.Completion);
        }

        if (this.serviceMxStream is not null)
        {
            tasks.Add(this.serviceMxStream.DisposeAsync().AsTask());
            tasks.Add(this.serviceMxStream.Completion);
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Verifies that all methods on the service interface include a <see cref="CancellationToken"/> as the last parameter.
    /// </summary>
    [SkippableFact]
    public void AllMethodsIncludeCancellationToken()
    {
        Skip.IfNot(this.DefaultTestsEnabled, $"{nameof(this.DefaultTestsEnabled)} is set to false.");
        AssertAllMethodsIncludeCancellationToken<TInterface>();
    }

    /// <summary>
    /// Verifies that all methods on a given interface include a <see cref="CancellationToken"/> as the last parameter.
    /// </summary>
    /// <typeparam name="T">The interface on which to perform the test.</typeparam>
    protected static void AssertAllMethodsIncludeCancellationToken<T>()
    {
        Assert.All(typeof(T).GetMethods(), m =>
        {
            // SpecialName is true for property getters/setters and event adders/removers.
            if (!m.IsSpecialName)
            {
                Assert.Contains(m.GetParameters(), p => p.ParameterType == typeof(CancellationToken));
            }
        });
    }

    /// <summary>
    /// Asserts that an event is raised with expected data.
    /// </summary>
    /// <param name="addHandler">The delegate that can add the given handler to the event on the <see cref="ClientProxy"/>.</param>
    /// <param name="removeHandler">The delegate that can remove the given handler from the event on the <see cref="ClientProxy"/>.</param>
    /// <param name="triggerEvent">The delegate that calls directly into the <see cref="Service"/> to raise the event.</param>
    /// <returns>A <see cref="Task"/> that should be awaited by the test method.</returns>
    protected async Task AssertEventRaisedAsync(Action<TInterface, EventHandler> addHandler, Action<TInterface, EventHandler> removeHandler, Action<TServiceMock> triggerEvent)
    {
        Requires.NotNull(addHandler);
        Requires.NotNull(removeHandler);
        Requires.NotNull(triggerEvent);

        var eventRaised = new TaskCompletionSource<object?>();
        EventHandler handler = (s, e) =>
        {
            try
            {
                eventRaised.SetResult(null);
            }
            catch (Exception ex)
            {
                eventRaised.SetException(ex);
            }
        };
        addHandler(this.ClientProxy, handler);
        triggerEvent(this.Service);
        await eventRaised.Task.WithCancellation(this.TimeoutToken);
        removeHandler(this.ClientProxy, handler);
    }

    /// <summary>
    /// Asserts that an event is raised with expected data.
    /// </summary>
    /// <typeparam name="TEventArgs">The type argument for the <see cref="EventHandler{TEventArgs}"/> delegate.</typeparam>
    /// <param name="addHandler">The delegate that can add the given handler to the event on the <see cref="ClientProxy"/>.</param>
    /// <param name="removeHandler">The delegate that can remove the given handler from the event on the <see cref="ClientProxy"/>.</param>
    /// <param name="triggerEvent">The delegate that calls directly into the <see cref="Service"/> to raise the event.</param>
    /// <param name="argsAssertions">A delegate to execute that contains assertions on the data sent with the event.</param>
    /// <returns>A <see cref="Task"/> that should be awaited by the test method.</returns>
    protected async Task AssertEventRaisedAsync<TEventArgs>(Action<TInterface, EventHandler<TEventArgs>> addHandler, Action<TInterface, EventHandler<TEventArgs>> removeHandler, Action<TServiceMock> triggerEvent, Action<TEventArgs> argsAssertions)
    {
        Requires.NotNull(addHandler);
        Requires.NotNull(removeHandler);
        Requires.NotNull(triggerEvent);

        var eventRaised = new TaskCompletionSource<object?>();
        EventHandler<TEventArgs> handler = (s, e) =>
        {
            try
            {
                argsAssertions(e);
                eventRaised.SetResult(null);
            }
            catch (Exception ex)
            {
                eventRaised.SetException(ex);
            }
        };
        addHandler(this.ClientProxy, handler);
        triggerEvent(this.Service);
        await eventRaised.Task.WithCancellation(this.TimeoutToken);
        removeHandler(this.ClientProxy, handler);
    }

    /// <summary>
    /// Configures the targets and proxies for the RPC connections.
    /// </summary>
    /// <param name="clientConnection">RPC connection to the test class (client).</param>
    /// <param name="serverConnection">RPC connection to the mocked service (server).</param>
    protected virtual void ConfigureRpcConnections([ValidatedNotNull] ServiceRpcDescriptor.RpcConnection clientConnection, [ValidatedNotNull] ServiceRpcDescriptor.RpcConnection serverConnection)
    {
        Requires.NotNull(serverConnection, nameof(serverConnection));
        Requires.NotNull(clientConnection, nameof(clientConnection));

        this.ClientProxy = clientConnection.ConstructRpcClient<TInterface>();
        serverConnection.AddLocalRpcTarget(this.Service);
    }
}
