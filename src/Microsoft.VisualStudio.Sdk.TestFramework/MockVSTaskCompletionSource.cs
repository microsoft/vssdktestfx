using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.Sdk.TestFramework
{
    internal sealed class MockVSTaskCompletionSource : IVsTaskCompletionSource
    {
        private TaskCompletionSource<object> taskCompletionSource = new TaskCompletionSource<object>();
        private readonly object asyncState;
        private MockVSTask underlyingTask;

        public MockVSTaskCompletionSource() : this(null)
        { }

        public MockVSTaskCompletionSource(object asyncState)
        {
            this.asyncState = asyncState;
        }

        public IVsTask Task
        {
            get
            {
                return this.UnderlyingTask;
            }
        }

        public void AddDependentTask(IVsTask pTask)
        {
            // We run everything on a threadpool so we ignore this
        }

        public void SetCanceled()
        {
            this.UnderlyingTask.Cancel();
        }

        public void SetFaulted(int hr)
        {
            this.taskCompletionSource.SetException(Marshal.GetExceptionForHR(hr));
        }

        public void SetResult(object result)
        {
            this.taskCompletionSource.SetResult(result);
        }

        private MockVSTask UnderlyingTask
        {
            get
            {
                return (this.underlyingTask ?? (this.underlyingTask = new MockVSTask(this.taskCompletionSource.Task)));
            }
        }
    }
}
