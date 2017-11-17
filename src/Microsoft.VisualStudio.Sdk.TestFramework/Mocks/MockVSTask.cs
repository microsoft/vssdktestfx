namespace Microsoft.VisualStudio.Sdk.TestFramework
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    /// <summary>
    /// A mock implementation of <see cref="IVsTask"/>.
    /// </summary>
    internal sealed class MockVSTask : IVsTask, IVsTaskJoinableTask, IVsTaskEvents
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly Task<object> task;
        private readonly object asyncState;
        private string description;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockVSTask"/> class.
        /// </summary>
        /// <param name="task">The task to wrap.</param>
        public MockVSTask(Task<object> task)
            : this(task, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MockVSTask"/> class.
        /// </summary>
        /// <param name="task">The task to execute.</param>
        /// <param name="asyncState">The async state object to store.</param>
        public MockVSTask(Task<object> task, object asyncState)
        {
            this.task = task ?? throw new ArgumentNullException(nameof(task));
            this.asyncState = asyncState;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MockVSTask"/> class.
        /// </summary>
        /// <param name="taskBody">The body to execute.</param>
        public MockVSTask(IVsTaskBody taskBody)
            : this(taskBody, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MockVSTask"/> class.
        /// </summary>
        /// <param name="taskBody">The body to execute.</param>
        /// <param name="asyncState">The async state object to store.</param>
        public MockVSTask(IVsTaskBody taskBody, object asyncState)
        {
            this.task = new Task<object>(
                () =>
                {
                    taskBody.DoWork(this, 0, new IVsTask[0], out object result);
                    return result;
                },
                this.cancellationTokenSource.Token);

            this.asyncState = asyncState;
        }

        /// <inheritdoc />
        public event EventHandler OnBlockingWaitBegin;

        /// <inheritdoc />
        public event EventHandler OnBlockingWaitEnd;

        /// <inheritdoc />
        public event EventHandler<BlockingTaskEventArgs> OnMarkedAsBlocking;

        /// <inheritdoc />
        public object AsyncState => this.asyncState;

        /// <inheritdoc />
        public string Description
        {
            get
            {
                return this.description;
            }

            set
            {
                this.description = value;
            }
        }

        /// <inheritdoc />
        public bool IsCanceled => this.task.IsCanceled;

        /// <inheritdoc />
        public bool IsCompleted
        {
            get
            {
                return this.task.IsCompleted;
            }
        }

        /// <inheritdoc />
        public bool IsFaulted
        {
            get
            {
                return this.task.IsFaulted;
            }
        }

        /// <inheritdoc />
        public CancellationToken CancellationToken
        {
            get
            {
                return this.cancellationTokenSource.Token;
            }
        }

        /// <inheritdoc />
        public void AbortIfCanceled()
        {
            if (this.task.IsCanceled)
            {
                throw new TaskCanceledException();
            }
        }

        /// <inheritdoc />
        public void Cancel()
        {
            this.cancellationTokenSource.Cancel();
        }

        /// <inheritdoc />
        public IVsTask ContinueWith(uint context, IVsTaskBody pTaskBody)
        {
            return this.ContinueWithEx(0, 0, pTaskBody, pAsyncState: null);
        }

        /// <inheritdoc />
        public IVsTask ContinueWithEx(uint context, uint options, IVsTaskBody pTaskBody, object pAsyncState)
        {
            // NOTE: We ignore options (and context), if any tests are testing code that relies on either this
            // would need to be modified to properly support them.
            return new MockVSTask(
                this.task.ContinueWith(
                    t =>
                    {
                        pTaskBody.DoWork(this, 0, new IVsTask[0], out object result);
                        return result;
                    },
                    this.cancellationTokenSource.Token),
                pAsyncState);
        }

        /// <inheritdoc />
        public object GetResult()
        {
            return this.task.GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        public void Start()
        {
            this.task.Start();
        }

        /// <inheritdoc />
        public void Wait()
        {
            this.WaitEx(-1, (uint)__VSTASKWAITOPTIONS.VSTWO_None);
        }

        /// <inheritdoc />
        public bool WaitEx(int millisecondsTimeout, uint options)
        {
            __VSTASKWAITOPTIONS typedOptions = (__VSTASKWAITOPTIONS)options;

            if ((typedOptions & __VSTASKWAITOPTIONS.VSTWO_AbortOnTaskCancellation) == __VSTASKWAITOPTIONS.VSTWO_AbortOnTaskCancellation)
            {
                return this.task.Wait(millisecondsTimeout, this.cancellationTokenSource.Token);
            }
            else
            {
                return this.task.Wait(millisecondsTimeout);
            }
        }

        /// <inheritdoc />
        public void AssociateJoinableTask(object joinableTask)
        {
        }

        private void FakeMethodToAvoidStupidWarningAsError()
        {
            this.OnBlockingWaitBegin(this, EventArgs.Empty);
            this.OnBlockingWaitEnd(this, EventArgs.Empty);
            this.OnMarkedAsBlocking(this, new BlockingTaskEventArgs(this, this));
        }
    }
}
