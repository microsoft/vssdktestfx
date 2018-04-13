namespace Microsoft.VisualStudio.Sdk.TestFramework
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.Threading;

    /// <summary>
    /// A mock implementation of <see cref="SVsTaskSchedulerService"/>.
    /// </summary>
    internal sealed class MockTaskSchedulerService : IVsTaskSchedulerService, IVsTaskSchedulerService2
    {
        private readonly JoinableTaskContext joinableTaskContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockTaskSchedulerService"/> class.
        /// </summary>
        /// <param name="context">The <see cref="JoinableTaskContext"/> to use.</param>
        internal MockTaskSchedulerService(JoinableTaskContext context)
        {
            this.joinableTaskContext = context;
        }

        /// <inheritdoc />
        public IVsTask ContinueWhenAllCompleted(uint context, uint dwTasks, IVsTask[] pDependentTasks, IVsTaskBody pTaskBody)
        {
            return this.ContinueWhenAllCompletedEx(context, dwTasks, pDependentTasks, options: 0, pTaskBody: pTaskBody, pAsyncState: null);
        }

        /// <inheritdoc />
        public IVsTask ContinueWhenAllCompletedEx(uint context, uint dwTasks, IVsTask[] pDependentTasks, uint options, IVsTaskBody pTaskBody, object pAsyncState)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public IVsTask CreateTask(uint context, IVsTaskBody pTaskBody)
        {
            return this.CreateTaskEx(context, options: 0, pTaskBody: pTaskBody, pAsyncState: null);
        }

        /// <inheritdoc />
        public IVsTaskCompletionSource CreateTaskCompletionSource() => this.CreateTaskCompletionSourceEx(0, null);

        /// <inheritdoc />
        public IVsTaskCompletionSource CreateTaskCompletionSourceEx(uint options, object asyncState)
        {
            return new MockVSTaskCompletionSource(this, asyncState);
        }

        /// <inheritdoc />
        public IVsTask CreateTaskEx(uint context, uint options, IVsTaskBody pTaskBody, object pAsyncState)
        {
            return new MockVSTask(this, context, pTaskBody, pAsyncState);
        }

        /// <inheritdoc />
        public object GetAsyncTaskContext() => this.joinableTaskContext;

        /// <inheritdoc />
        public object GetTaskScheduler([ComAliasName("VsShell.VSTASKRUNCONTEXT")]uint context)
        {
            var runContext = (VsTaskRunContext)context;
            if (runContext == VsTaskRunContext.CurrentContext)
            {
                runContext = ThreadHelper.CheckAccess() ? VsTaskRunContext.UIThreadNormalPriority : VsTaskRunContext.BackgroundThread;
            }

            switch (runContext)
            {
                case VsTaskRunContext.UIThreadBackgroundPriority:
                case VsTaskRunContext.UIThreadIdlePriority:
                case VsTaskRunContext.UIThreadNormalPriority:
                case VsTaskRunContext.UIThreadSend:
                    return new VsUITaskScheduler(this.joinableTaskContext.Factory);
                case VsTaskRunContext.BackgroundThread:
                case VsTaskRunContext.BackgroundThreadLowIOPriority:
                default:
                    return TaskScheduler.Default;
            }
        }
    }
}
