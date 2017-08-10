using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.Sdk.TestFramework
{
    internal sealed class MockVSTask : IVsTask, IVsTaskJoinableTask, IVsTaskEvents
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private Task<object> task;
        private readonly object asyncState;
        private string description;

        public MockVSTask(Task<object> task) : this(task, null)
        {
        }

        public MockVSTask(Task<object> task, object asyncState)
        {
            if (task == null)
            {
                throw new ArgumentNullException("task");
            }

            this.task = task;
            this.asyncState = asyncState;
        }

        public MockVSTask(IVsTaskBody taskBody) : this(taskBody, null)
        {
        }

        public MockVSTask(IVsTaskBody taskBody, object asyncState)
        {
            this.task = new Task<object>(() =>
            {
                object result;
                taskBody.DoWork(this, 0, new IVsTask[0], out result);
                return result;
            }, this.cancellationTokenSource.Token);

            this.asyncState = asyncState;
        }

        public object AsyncState
        {
            get
            {
                return this.asyncState;
            }
        }

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

        public bool IsCanceled
        {
            get
            {
                return this.task.IsCanceled;
            }
        }

        public bool IsCompleted
        {
            get
            {
                return this.task.IsCompleted;
            }
        }

        public bool IsFaulted
        {
            get
            {
                return this.task.IsFaulted;
            }
        }

        public CancellationToken CancellationToken
        {
            get
            {
                return this.cancellationTokenSource.Token;
            }
        }

        public void AbortIfCanceled()
        {
            if (this.task.IsCanceled)
            {
                throw new TaskCanceledException();
            }
        }

        public void Cancel()
        {
            this.cancellationTokenSource.Cancel();
        }

        public IVsTask ContinueWith(uint context, IVsTaskBody pTaskBody)
        {
            return ContinueWithEx(0, 0, pTaskBody, pAsyncState: null);
        }

        public IVsTask ContinueWithEx(uint context, uint options, IVsTaskBody pTaskBody, object pAsyncState)
        {
            // NOTE: We ignore options (and context), if any tests are testing code that relies on either this
            // would need to be modified to properly support them.

            return new MockVSTask(this.task.ContinueWith((t) =>
            {
                object result;
                pTaskBody.DoWork(this, 0, new IVsTask[0], out result);
                return result;
            }, this.cancellationTokenSource.Token), pAsyncState);


        }

        public object GetResult()
        {
            return this.task.Result;
        }

        public void Start()
        {
            this.task.Start();
        }

        public void Wait()
        {
            WaitEx(-1, (uint)__VSTASKWAITOPTIONS.VSTWO_None);
        }

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

        #region IVsJoinableTask

        public void AssociateJoinableTask(object joinableTask)
        {
        }

        #endregion

        #region IVsTaskEvents

        public event EventHandler OnBlockingWaitBegin;
        public event EventHandler OnBlockingWaitEnd;
        public event EventHandler<BlockingTaskEventArgs> OnMarkedAsBlocking;

        #endregion

        private void FakeMethodToAvoidStupidWarningAsError()
        {
            OnBlockingWaitBegin(this, EventArgs.Empty); ;
            OnBlockingWaitEnd(this, EventArgs.Empty);
            OnMarkedAsBlocking(this, new BlockingTaskEventArgs(this, this));
        }
    }
}
