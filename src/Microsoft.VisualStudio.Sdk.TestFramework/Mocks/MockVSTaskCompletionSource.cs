namespace Microsoft.VisualStudio.Sdk.TestFramework
{
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Shell.Interop;

    /// <summary>
    /// A mock implementation of <see cref="IVsTaskCompletionSource"/>.
    /// </summary>
    internal sealed class MockVSTaskCompletionSource : IVsTaskCompletionSource
    {
        private readonly IVsTaskSchedulerService2 vsTaskSchedulerService2;
        private readonly TaskCompletionSource<object> taskCompletionSource = new TaskCompletionSource<object>();
        private readonly object asyncState;
        private MockVSTask underlyingTask;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockVSTaskCompletionSource"/> class.
        /// </summary>
        /// <param name="vsTaskSchedulerService2">The <see cref="SVsTaskSchedulerService"/>.</param>
        /// <param name="asyncState">The state object.</param>
        public MockVSTaskCompletionSource(IVsTaskSchedulerService2 vsTaskSchedulerService2, object asyncState = null)
        {
            this.vsTaskSchedulerService2 = vsTaskSchedulerService2 ?? throw new System.ArgumentNullException(nameof(vsTaskSchedulerService2));
            this.asyncState = asyncState;
        }

        /// <inheritdoc />
        public IVsTask Task => this.UnderlyingTask;

        /// <summary>
        /// Gets the mocked VSTask.
        /// </summary>
        private MockVSTask UnderlyingTask
        {
            get
            {
                if (this.underlyingTask == null)
                {
                    this.underlyingTask = new MockVSTask(
                        this.vsTaskSchedulerService2,
                        (uint)__VSTASKRUNCONTEXT.VSTC_CURRENTCONTEXT,
                        this.taskCompletionSource.Task);
                }

                return this.underlyingTask;
            }
        }

        /// <inheritdoc />
        public void AddDependentTask(IVsTask pTask)
        {
            // We run everything on a threadpool so we ignore this
        }

        /// <inheritdoc />
        public void SetCanceled() => this.UnderlyingTask.Cancel();

        /// <inheritdoc />
        public void SetFaulted(int hr) => this.taskCompletionSource.SetException(Marshal.GetExceptionForHR(hr));

        /// <inheritdoc />
        public void SetResult(object result) => this.taskCompletionSource.SetResult(result);
    }
}
