using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.Sdk.TestFramework
{
    internal sealed class MockTaskSchedulerService : IVsTaskSchedulerService, IVsTaskSchedulerService2
    {
        private readonly JoinableTaskContext joinableTaskContext;

        internal MockTaskSchedulerService(JoinableTaskContext context)
        {
            this.joinableTaskContext = context;
        }

        #region IVsTaskSchedulerService

        public IVsTask ContinueWhenAllCompleted(uint context, uint dwTasks, IVsTask[] pDependentTasks, IVsTaskBody pTaskBody)
        {
            return ContinueWhenAllCompletedEx(context, dwTasks, pDependentTasks, options: 0, pTaskBody: pTaskBody, pAsyncState: null);
        }

        public IVsTask ContinueWhenAllCompletedEx(uint context, uint dwTasks, IVsTask[] pDependentTasks, uint options, IVsTaskBody pTaskBody, object pAsyncState)
        {
            throw new NotImplementedException();
        }

        public IVsTask CreateTask(uint context, IVsTaskBody pTaskBody)
        {
            return CreateTaskEx(context, options: 0, pTaskBody: pTaskBody, pAsyncState: null);
        }

        public IVsTaskCompletionSource CreateTaskCompletionSource()
        {
            return CreateTaskCompletionSourceEx(0, null);
        }

        public IVsTaskCompletionSource CreateTaskCompletionSourceEx(uint options, object asyncState)
        {
            return new MockVSTaskCompletionSource(asyncState);
        }

        public IVsTask CreateTaskEx(uint context, uint options, IVsTaskBody pTaskBody, object pAsyncState)
        {
            return new MockVSTask(pTaskBody, pAsyncState);
        }

        #endregion

        #region IVsTaskSchedulerService2

        public object GetAsyncTaskContext()
        {
            return this.joinableTaskContext;
        }

        public object GetTaskScheduler([ComAliasName("VsShell.VSTASKRUNCONTEXT")]uint context)
        {
            var runContext = (VsTaskRunContext)context;
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

        #endregion
    }
}
