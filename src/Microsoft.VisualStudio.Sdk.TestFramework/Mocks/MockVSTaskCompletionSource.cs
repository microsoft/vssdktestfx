namespace Microsoft.VisualStudio.Sdk.TestFramework
{
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Shell.Interop;

    /// <summary>
    /// A mock implementation of <see cref="IVsTaskCompletionSource"/>
    /// </summary>
    internal sealed class MockVSTaskCompletionSource : IVsTaskCompletionSource
    {
        private readonly TaskCompletionSource<object> taskCompletionSource = new TaskCompletionSource<object>();
        private readonly object asyncState;
        private MockVSTask underlyingTask;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockVSTaskCompletionSource"/> class.
        /// </summary>
        public MockVSTaskCompletionSource()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MockVSTaskCompletionSource"/> class.
        /// </summary>
        /// <param name="asyncState">The state object.</param>
        public MockVSTaskCompletionSource(object asyncState)
        {
            this.asyncState = asyncState;
        }

        /// <inheritdoc />
        public IVsTask Task
        {
            get
            {
                return this.UnderlyingTask;
            }
        }

        /// <summary>
        /// Gets the mocked VSTask.
        /// </summary>
        private MockVSTask UnderlyingTask
        {
            get
            {
                return this.underlyingTask ?? (this.underlyingTask = new MockVSTask(this.taskCompletionSource.Task));
            }
        }

        /// <inheritdoc />
        public void AddDependentTask(IVsTask pTask)
        {
            // We run everything on a threadpool so we ignore this
        }

        /// <inheritdoc />
        public void SetCanceled()
        {
            this.UnderlyingTask.Cancel();
        }

        /// <inheritdoc />
        public void SetFaulted(int hr)
        {
            this.taskCompletionSource.SetException(Marshal.GetExceptionForHR(hr));
        }

        /// <inheritdoc />
        public void SetResult(object result)
        {
            this.taskCompletionSource.SetResult(result);
        }
    }
}
