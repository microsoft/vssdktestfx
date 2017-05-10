/********************************************************
*                                                        *
*   © Copyright (C) Microsoft. All rights reserved.      *
*                                                        *
*********************************************************/

namespace Microsoft.VisualStudio.Sdk.TestFramework
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Threading;
    using Internal.VisualStudio.Shell.Interop;
    using Moq;
    using Shell;
    using Shell.Interop;
    using Threading;

    /// <summary>
    /// Provides the "global service provider" for Visual Studio components.
    /// </summary>
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
            this.instance.Shutdown();
        }

        /// <summary>
        /// Removes any services added via <see cref="AddService"/>.
        /// </summary>
        public void Reset()
        {
            this.instance.Reset();
        }

        /// <summary>
        /// Adds a global service.
        /// </summary>
        /// <param name="serviceType">The type identity of the service.</param>
        /// <param name="instance">The instance of the service.</param>
        public void AddService(Type serviceType, object instance)
        {
            this.instance.AddService(serviceType, instance);
        }

        /// <summary>
        /// The mock OLE service provider used by Visual Studio as the global service provider.
        /// </summary>
        private class OleServiceProviderMock : OLE.Interop.IServiceProvider
        {
            private readonly Thread mainThread;

            private readonly AsyncManualResetEvent mainThreadInitialized;

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
            internal OleServiceProviderMock()
            {
                this.mainThreadInitialized = new AsyncManualResetEvent();
                this.mainThread = new Thread(this.MainThread);
                this.mainThread.SetApartmentState(ApartmentState.STA);
                this.mainThread.Name = "VS Main Thread (mocked)";
                this.mainThread.Start();
                this.mainThreadInitialized.WaitAsync().Wait();
            }

            /// <inheritdoc />
            int OLE.Interop.IServiceProvider.QueryService(ref Guid guidService, ref Guid riid, out IntPtr ppvObject)
            {
                object service;
                if (!this.services.TryGetValue(guidService, out service))
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
            /// Removes any services added via <see cref="AddService"/>.
            /// </summary>
            internal void Reset()
            {
                this.services = this.baseServices;
            }

            /// <summary>
            /// Adds a global service.
            /// </summary>
            /// <param name="serviceType">The type identity of the service.</param>
            /// <param name="instance">The instance of the service.</param>
            internal void AddService(Type serviceType, object instance)
            {
                Requires.NotNull(serviceType, nameof(serviceType));
                Requires.NotNull(instance, nameof(instance));

                Verify.Operation(
                    ImmutableInterlocked.TryAdd(ref this.services, serviceType.GUID, instance),
                    "Service already added.");
            }

            /// <summary>
            /// Terminates the main thread.
            /// </summary>
            internal void Shutdown()
            {
                // Terminate the main thread.
                this.mainThreadInitialized?.WaitAsync().Wait();
                this.mainMessagePumpFrame.Continue = false;
                this.mainThread.Join();
            }

            /// <summary>
            /// The entrypoint for the mocked main thread.
            /// </summary>
            private void MainThread()
            {
                this.mainThreadSyncContext = new DispatcherSynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(this.mainThreadSyncContext);
                new Application(); // just creating this sets it to Application.Current, as required

                this.joinableTaskContext = new JoinableTaskContext();
                this.mainMessagePumpFrame = new DispatcherFrame();

                this.services = ImmutableDictionary.Create<Guid, object>();
                this.AddService(typeof(SVsActivityLog), this.CreateVsActivityLogMock().Object);
                this.AddService(typeof(SVsTaskSchedulerService), this.CreateVsTaskSchedulerServiceMock().Object);
                this.AddService(typeof(SVsUIThreadInvokerPrivate), new VsUIThreadInvoker(this.joinableTaskContext));
                this.baseServices = this.services;

                // We can only call this once for the AppDomain.
                ServiceProvider.CreateFromSetSite(this);

                // Arrange to signal that we're done initialization the main thread
                // once the message pump starts.
                this.mainThreadSyncContext.Post(s => this.mainThreadInitialized.Set(), null);

                Dispatcher.PushFrame(this.mainMessagePumpFrame);
            }

            private Mock<IVsActivityLog> CreateVsActivityLogMock()
            {
                var mock = new Mock<IVsActivityLog>();
                return mock;
            }

            private Mock<IVsTaskSchedulerService> CreateVsTaskSchedulerServiceMock()
            {
                var taskSchedulerService = new Mock<IVsTaskSchedulerService>();
                var taskSchedulerService2 = taskSchedulerService.As<IVsTaskSchedulerService2>();
                taskSchedulerService2.Setup(ts => ts.GetTaskScheduler(It.IsIn<uint>((uint)VsTaskRunContext.BackgroundThread, (uint)VsTaskRunContext.BackgroundThreadLowIOPriority)))
                    .Returns(TaskScheduler.Default);
                taskSchedulerService2.Setup(ts => ts.GetTaskScheduler(It.IsIn<uint>((uint)VsTaskRunContext.UIThreadBackgroundPriority, (uint)VsTaskRunContext.UIThreadIdlePriority, (uint)VsTaskRunContext.UIThreadNormalPriority, (uint)VsTaskRunContext.UIThreadSend)))
                    .Returns(new VsUITaskScheduler(this.joinableTaskContext.Factory));
                taskSchedulerService2.Setup(ts => ts.GetAsyncTaskContext())
                    .Returns(this.joinableTaskContext);
                return taskSchedulerService;
            }

            private class VsUITaskScheduler : TaskScheduler
            {
                private readonly JoinableTaskFactory jtf;

                internal VsUITaskScheduler(JoinableTaskFactory jtf)
                {
                    Requires.NotNull(jtf, nameof(jtf));
                    this.jtf = jtf;
                }

                protected override IEnumerable<System.Threading.Tasks.Task> GetScheduledTasks()
                {
                    throw new NotImplementedException();
                }

                protected override void QueueTask(System.Threading.Tasks.Task task)
                {
                    using (this.jtf.Context.SuppressRelevance())
                    {
                        this.jtf.RunAsync(async delegate
                        {
                            await this.jtf.SwitchToMainThreadAsync();
                            this.TryExecuteTask(task);
                        });
                    }
                }

                protected override bool TryExecuteTaskInline(System.Threading.Tasks.Task task, bool taskWasPreviouslyQueued)
                {
                    throw new NotImplementedException();
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
                    catch (Exception ex)
                    {
                        return Marshal.GetHRForException(ex);
                    }
                }
            }
        }
    }
}
