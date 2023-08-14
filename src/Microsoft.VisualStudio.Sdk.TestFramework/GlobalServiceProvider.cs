// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETFRAMEWORK || WINDOWS

using System.Windows;
using System.Windows.Threading;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.ServiceHub.Framework.Testing;
using Microsoft.VisualStudio.Sdk.TestFramework.Mocks;
using Microsoft.VisualStudio.Shell.ServiceBroker;

namespace Microsoft.VisualStudio.Sdk.TestFramework;

/// <summary>
/// Provides the "global service provider" for Visual Studio components.
/// </summary>
/// <remarks>
/// This type manages the adding of services to the global service provider.
/// The service provider itself is exposed to test and product code via the
/// <see cref="ServiceProvider.GlobalProvider" /> static property.
/// </remarks>
public class GlobalServiceProvider : IDisposable
{
    /// <summary>
    /// The global service provider in use by this fixture.
    /// </summary>
    private OleServiceProviderMock instance;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalServiceProvider"/> class.
    /// </summary>
    public GlobalServiceProvider()
    {
        this.instance = new OleServiceProviderMock();
    }

    /// <summary>
    /// Disposes of any dedicated resources for VS mocks.
    /// </summary>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc cref="OleServiceProviderMock.Reset()" />
    public void Reset() => this.instance.Reset();

    /// <inheritdoc cref="OleServiceProviderMock.AddService(Type, object)" />
    public void AddService(Type serviceType, object instance) => this.instance.AddService(serviceType ?? throw new ArgumentNullException(nameof(serviceType)), instance ?? throw new ArgumentNullException(nameof(instance)));

    /// <summary>
    /// Disposes of managed and native resources.
    /// </summary>
    /// <param name="disposing">A value indicating whether the object is being disposed.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.instance.Shutdown();
        }
    }

    /// <summary>
    /// The mock OLE service provider used by Visual Studio as the global service provider.
    /// </summary>
#pragma warning disable CA1001 // Types that own disposable fields should be disposable
    private class OleServiceProviderMock : OLE.Interop.IServiceProvider
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
    {
        private readonly Thread mainThread;

        private readonly TaskCompletionSource<object?> mainThreadInitialized = new TaskCompletionSource<object?>();

        private MockBrokeredServiceContainer mockBrokeredServiceContainer = new MockBrokeredServiceContainer();

        /// <summary>
        /// The initial set of minimal services.
        /// </summary>
        private ImmutableDictionary<Guid, object> baseServices;

        /// <summary>
        /// The current set of services.
        /// </summary>
        private ImmutableDictionary<Guid, object> services;

        /// <summary>
        /// The <see cref="JoinableTaskContext"/> that serves the mocked main thread.
        /// </summary>
        private JoinableTaskContext joinableTaskContext;

        /// <summary>
        /// The <see cref="SynchronizationContext"/> set on the mocked main thread.
        /// </summary>
        private DispatcherSynchronizationContext mainThreadSyncContext;

        /// <summary>
        /// The frame that keeps the main message pump running.
        /// </summary>
        private DispatcherFrame mainMessagePumpFrame;

        /// <summary>
        /// Initializes a new instance of the <see cref="OleServiceProviderMock"/> class.
        /// </summary>
        /// <remarks>
        /// This type must be a singleton because static members in VS components
        /// capture it once they've seen it once.
        /// </remarks>
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        internal OleServiceProviderMock()
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        {
            this.services = ImmutableDictionary.Create<Guid, object>();

            this.mainThread = new Thread(this.MainThread);
            this.mainThread.SetApartmentState(ApartmentState.STA);
            this.mainThread.Name = "VS Main Thread (mocked)";
            this.mainThread.Start();
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
            this.mainThreadInitialized.Task.GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits

            this.baseServices = this.services;
        }

        /// <inheritdoc />
        int OLE.Interop.IServiceProvider.QueryService(ref Guid guidService, ref Guid riid, out IntPtr ppvObject)
        {
            if (!this.services.TryGetValue(guidService, out object? service))
            {
                ppvObject = IntPtr.Zero;
                return VSConstants.E_INVALIDARG;
            }

            IntPtr pUnk = IntPtr.Zero;
            try
            {
                pUnk = Marshal.GetIUnknownForObject(service);
                return Marshal.QueryInterface(pUnk, ref riid, out ppvObject);
            }
            finally
            {
                Marshal.Release(pUnk);
            }
        }

        /// <summary>
        /// Removes any services added via <see cref="AddService"/>,
        /// leaving only those services whose marks are built into this library.
        /// </summary>
        internal void Reset()
        {
            this.mockBrokeredServiceContainer = new();
            this.services = this.baseServices.SetItem(typeof(SVsBrokeredServiceContainer).GUID, this.mockBrokeredServiceContainer);
        }

        /// <summary>
        /// Adds a global service, replacing a built-in mock if any.
        /// </summary>
        /// <param name="serviceType">The type identity of the service.</param>
        /// <param name="instance">The instance of the service.</param>
        internal void AddService(Type serviceType, object instance)
        {
            Verify.Operation(
                ImmutableInterlocked.TryAdd(ref this.services, serviceType.GUID, instance) ||
                (this.baseServices.TryGetValue(serviceType.GUID, out object? mock) && ImmutableInterlocked.TryUpdate(ref this.services, serviceType.GUID, instance, mock)),
                Strings.ServiceAlreadyAdded);
        }

        /// <summary>
        /// Terminates the main thread.
        /// </summary>
        internal void Shutdown()
        {
            // Terminate the main thread.
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
            this.mainThreadInitialized.Task.GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
            this.mainMessagePumpFrame.Continue = false;

            // The test runner has been known to at least once call us *on* the mocked main thread.
            // In such a case, joining oneself would deadlock the thread.
            // But if we're *on* the main thread, we don't need to block on its completion
            // as it is implicitly so until the stack unwinds.
            if (Thread.CurrentThread != this.mainThread)
            {
                this.mainThread.Join();
            }
        }

        private IVsTaskSchedulerService CreateVsTaskSchedulerServiceMock()
        {
            return new MockTaskSchedulerService(this.joinableTaskContext);
        }

        /// <summary>
        /// The entrypoint for the mocked main thread.
        /// </summary>
        private void MainThread()
        {
            try
            {
                this.mainThreadSyncContext = new DispatcherSynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(this.mainThreadSyncContext);
                _ = new Application(); // just creating this sets it to Application.Current, as required

#pragma warning disable VSSDK005 // Avoid instantiating JoinableTaskContext
                this.joinableTaskContext = new JoinableTaskContext();
#pragma warning restore VSSDK005 // Avoid instantiating JoinableTaskContext
                this.mainMessagePumpFrame = new DispatcherFrame();

                this.AddService(typeof(OLE.Interop.IServiceProvider), this);
                this.AddService(typeof(SVsActivityLog), new MockVsActivityLog());
                this.AddService(typeof(SVsTaskSchedulerService), this.CreateVsTaskSchedulerServiceMock());
                this.AddService(typeof(SVsUIThreadInvokerPrivate), new VsUIThreadInvoker(this.joinableTaskContext));
                this.AddService(typeof(SVsBrokeredServiceContainer), this.mockBrokeredServiceContainer);

                Shell.Interop.COMAsyncServiceProvider.IAsyncServiceProvider asyncServiceProvider = new MockAsyncServiceProvider(this);
                this.AddService(typeof(SAsyncServiceProvider), asyncServiceProvider);

                // We can only call this once for the AppDomain.
#pragma warning disable CA2000 // Dispose objects before losing scope
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
                ServiceProvider.CreateFromSetSite(this);
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
                AsyncServiceProvider.CreateFromSetSite(asyncServiceProvider);
#pragma warning restore CA2000 // Dispose objects before losing scope

                // Arrange to signal that we're done initialization the main thread
                // once the message pump starts.
                this.mainThreadSyncContext.Post(s => this.mainThreadInitialized.TrySetResult(null), null);

                Dispatcher.PushFrame(this.mainMessagePumpFrame);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                this.mainThreadInitialized.TrySetException(ex);
            }
        }

        private class VsUIThreadInvoker : IVsInvokerPrivate
        {
            private readonly JoinableTaskContext joinableTaskContext;

            /// <summary>
            /// Initializes a new instance of the <see cref="VsUIThreadInvoker"/> class.
            /// </summary>
            /// <param name="joinableTaskContext">The joinable task context to use to get to the UI thread.</param>
            internal VsUIThreadInvoker(JoinableTaskContext joinableTaskContext)
            {
                Requires.NotNull(joinableTaskContext, nameof(joinableTaskContext));

                this.joinableTaskContext = joinableTaskContext;
            }

            /// <summary>
            /// Executes the provided delegate on the UI thread, while blocking the calling thread.
            /// </summary>
            /// <param name="pInvokable">The interface with the delegate to invoke.</param>
            /// <returns>The HRESULT of the delegate or the attempt to execute it.</returns>
            public int Invoke(IVsInvokablePrivate pInvokable)
            {
                try
                {
                    return this.joinableTaskContext.Factory.Run(async delegate
                    {
                        await this.joinableTaskContext.Factory.SwitchToMainThreadAsync();
                        return pInvokable.Invoke();
                    });
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    return Marshal.GetHRForException(ex);
                }
            }
        }
    }
}

#endif
