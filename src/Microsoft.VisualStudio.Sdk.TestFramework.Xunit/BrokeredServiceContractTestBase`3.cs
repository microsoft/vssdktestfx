// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO.Pipelines;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Threading;
using Nerdbank.Streams;
using Xunit;

namespace Microsoft.VisualStudio.Sdk.TestFramework
{
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
        private MultiplexingStream? clientCallbackClientMxStream;
        private MultiplexingStream? clientCallbackServiceMxStream;

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
        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            int testId = this.TestCounter;
            Func<string, SourceLevels, TraceSource> traceSourceFactory = (name, verbosity) =>
                new TraceSource(name)
                {
                    Switch = { Level = verbosity },
                    Listeners =
                    {
                    new XunitTraceListener(this.Logger, testId, this.TestStopwatch),
                    },
                };

            if (this.Descriptor is ServiceJsonRpcDescriptor { MultiplexingStreamOptions: object } descriptor)
            {
                // This is a V3 descriptor, which sets up its own MultiplexingStream.
                (IDuplexPipe, IDuplexPipe) underlyingPipes = FullDuplexStream.CreatePipePair();
                (this.Service as IMockServiceWithClientCallback).ClientCallback = descriptor
                    .WithMultiplexingStream(new MultiplexingStream.Options(descriptor.MultiplexingStreamOptions)
                    {
                        TraceSource = traceSourceFactory("Client mxstream", this.MultiplexingLoggingVerbosity),
                        DefaultChannelTraceSourceFactoryWithQualifier = (id, name) => traceSourceFactory($"Client mxstream {id} (\"{name}\")", this.MultiplexingLoggingVerbosity),
                    })
                    .WithTraceSource(traceSourceFactory("Client RPC", this.DescriptorLoggingVerbosity))
                    .ConstructRpc<TClientInterfaceMock>(underlyingPipes.Item1);
                descriptor
                    .WithMultiplexingStream(new MultiplexingStream.Options(descriptor.MultiplexingStreamOptions)
                    {
                        TraceSource = traceSourceFactory("Server mxstream", this.MultiplexingLoggingVerbosity),
                        DefaultChannelTraceSourceFactoryWithQualifier = (id, name) => traceSourceFactory($"Server mxstream {id} (\"{name}\")", this.MultiplexingLoggingVerbosity),
                    })
                    .WithTraceSource(traceSourceFactory("Server RPC", this.DescriptorLoggingVerbosity))
                    .ConstructRpc(this.ClientInterface, underlyingPipes.Item2);
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
                this.clientCallbackClientMxStream = mxStreams[0];
                this.clientCallbackServiceMxStream = mxStreams[1];

                Task<MultiplexingStream.Channel> offerTask = mxStreams[0].OfferChannelAsync(string.Empty, this.TimeoutToken);
                Task<MultiplexingStream.Channel> acceptTask = mxStreams[1].AcceptChannelAsync(string.Empty, this.TimeoutToken);
                MultiplexingStream.Channel[] channels = await Task.WhenAll(offerTask, acceptTask);

#pragma warning disable CS0618 // Type or member is obsolete
                (this.Service as IMockServiceWithClientCallback).ClientCallback = this.Descriptor
                    .WithTraceSource(traceSourceFactory("Client RPC", this.DescriptorLoggingVerbosity))
                    .WithMultiplexingStream(mxStreams[0])
                    .ConstructRpc<TClientInterfaceMock>(channels[0]);
                this.Descriptor
                    .WithTraceSource(traceSourceFactory("Server RPC", this.DescriptorLoggingVerbosity))
                    .WithMultiplexingStream(mxStreams[1])
                    .ConstructRpc(this.ClientInterface, channels[1]);
#pragma warning restore CS0618 // Type or member is obsolete
            }
        }

        /// <inheritdoc/>
        public override async Task DisposeAsync()
        {
            await base.DisposeAsync();

            (this.ClientInterface as IDisposable)?.Dispose();

            List<Task> tasks = new();
            if (this.clientCallbackClientMxStream is not null)
            {
                tasks.Add(this.clientCallbackClientMxStream.DisposeAsync().AsTask());
                tasks.Add(this.clientCallbackClientMxStream.Completion);
            }

            if (this.clientCallbackServiceMxStream is not null)
            {
                tasks.Add(this.clientCallbackServiceMxStream.DisposeAsync().AsTask());
                tasks.Add(this.clientCallbackServiceMxStream.Completion);
            }

            await Task.WhenAll(tasks);
        }
    }
}
