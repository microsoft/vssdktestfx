// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETFRAMEWORK || WINDOWS

namespace Microsoft.VisualStudio.Sdk.TestFramework.Mocks;

#pragma warning disable VSTHRD002 // Our mock implementation is pretty cheap

/// <summary>
/// A mock implementation of <see cref="IVsTask"/>.
/// </summary>
internal sealed class MockVSTask : IVsTask, IVsTaskJoinableTask, IVsTaskEvents, IDisposable
{
    private readonly IVsTaskSchedulerService2 vsTaskSchedulerService2;
    private readonly CancellationTokenSource cancellationTokenSource;
    private readonly uint context;
    private readonly Task<object> task;
    private readonly object? asyncState;

    /// <summary>
    /// Initializes a new instance of the <see cref="MockVSTask"/> class.
    /// </summary>
    /// <param name="vsTaskSchedulerService2">The <see cref="SVsTaskSchedulerService"/>.</param>
    /// <param name="context">The scheduling option for this task.</param>
    /// <param name="task">The task to execute.</param>
    /// <param name="asyncState">The async state object to store.</param>
    /// <param name="cts">The cancellation token source to use. This is useful when the caller needs to have already observed the token for the construction of <paramref name="task"/>.</param>
    internal MockVSTask(IVsTaskSchedulerService2 vsTaskSchedulerService2, uint context, Task<object> task, object? asyncState = null, CancellationTokenSource? cts = null)
    {
        this.vsTaskSchedulerService2 = vsTaskSchedulerService2 ?? throw new ArgumentNullException(nameof(vsTaskSchedulerService2));
        this.context = context;
        this.task = task ?? throw new ArgumentNullException(nameof(task));
        this.asyncState = asyncState;
        this.cancellationTokenSource = cts ?? new CancellationTokenSource();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MockVSTask"/> class.
    /// </summary>
    /// <param name="vsTaskSchedulerService2">The <see cref="SVsTaskSchedulerService"/>.</param>
    /// <param name="context">The scheduling option for this task.</param>
    /// <param name="taskBody">The body to execute.</param>
    /// <param name="asyncState">The async state object to store.</param>
    internal MockVSTask(IVsTaskSchedulerService2 vsTaskSchedulerService2, uint context, IVsTaskBody taskBody, object? asyncState = null)
    {
        this.vsTaskSchedulerService2 = vsTaskSchedulerService2 ?? throw new ArgumentNullException(nameof(vsTaskSchedulerService2));
        this.context = context;
        this.cancellationTokenSource = new CancellationTokenSource();
        this.task = new Task<object>(
            () =>
            {
                taskBody.DoWork(this, 0, Array.Empty<IVsTask>(), out object result);
                return result;
            },
            this.cancellationTokenSource.Token);

        this.asyncState = asyncState;
    }

    /// <inheritdoc />
    public event EventHandler? OnBlockingWaitBegin;

    /// <inheritdoc />
    public event EventHandler? OnBlockingWaitEnd;

    /// <inheritdoc />
    public event EventHandler<BlockingTaskEventArgs>? OnMarkedAsBlocking;

    /// <inheritdoc />
    public object? AsyncState => this.asyncState;

    /// <inheritdoc />
    public string? Description { get; set; }

    /// <inheritdoc />
    public bool IsCanceled => this.task.IsCanceled;

    /// <inheritdoc />
    public bool IsCompleted => this.task.IsCompleted;

    /// <inheritdoc />
    public bool IsFaulted => this.task.IsFaulted;

    /// <inheritdoc />
    public CancellationToken CancellationToken => this.cancellationTokenSource.Token;

    /// <inheritdoc/>
    public void Dispose()
    {
        this.cancellationTokenSource.Dispose();
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
    public void Cancel() => this.cancellationTokenSource.Cancel();

    /// <inheritdoc />
    public IVsTask ContinueWith(uint context, IVsTaskBody pTaskBody)
    {
        return this.ContinueWithEx(context, 0, pTaskBody, pAsyncState: null);
    }

    /// <inheritdoc />
    public IVsTask ContinueWithEx(uint context, uint options, IVsTaskBody pTaskBody, object? pAsyncState)
    {
        // NOTE: We ignore context, if any tests are testing code that relies on either this
        // would need to be modified to properly support them.
        VsTaskContinuationOptions internalOptions = (VsTaskContinuationOptions)options;
        CancellationTokenSource cts = new();
        return new MockVSTask(
            this.vsTaskSchedulerService2,
            context,
            this.task.ContinueWith(
                t =>
                {
                    pTaskBody.DoWork(this, 0, Array.Empty<IVsTask>(), out object result);
                    return result;
                },
                cts.Token,
                GetTPLOptions(internalOptions),
                (TaskScheduler)this.vsTaskSchedulerService2.GetTaskScheduler(context)),
            pAsyncState,
            cts);
    }

    /// <inheritdoc />
    public object GetResult() => this.task.GetAwaiter().GetResult();

    /// <inheritdoc />
    public void Start()
    {
        this.task.Start((TaskScheduler)this.vsTaskSchedulerService2.GetTaskScheduler(this.context));
    }

    /// <inheritdoc />
    public void Wait() => this.WaitEx(-1, (uint)__VSTASKWAITOPTIONS.VSTWO_None);

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

    private static TaskContinuationOptions GetTPLOptions(VsTaskContinuationOptions options)
    {
        TaskContinuationOptions tplOptions = (TaskContinuationOptions)(options & ~(VsTaskContinuationOptions.NotCancelable | VsTaskContinuationOptions.IndependentlyCanceled | VsTaskContinuationOptions.CancelWithParent));

        // Do not execute continuations synchronously as VS Task Library doesn't support it.
        //
        // Reason is that we have to store the TPL task that Task.ContinueWith method returns in InternalTask property to do certain
        // checks (like deadlock mitigation) but if task starts executing inside ContinueWith function (i.e. inlined) before it returns
        // we never get to set InternalTask property. As VS task library tries to access InternalTask for checks, we redundantly spin
        // wait for it to be set for 10 seconds and then return null shortcutting the checks. Since we shipped this option with
        // Shell.11.0 it is better if we don't throw exception but silently ignore it.
        tplOptions &= ~TaskContinuationOptions.ExecuteSynchronously;

        return tplOptions;
    }

    private void FakeMethodToAvoidStupidWarningAsError()
    {
        this.OnBlockingWaitBegin?.Invoke(this, EventArgs.Empty);
        this.OnBlockingWaitEnd?.Invoke(this, EventArgs.Empty);
        this.OnMarkedAsBlocking?.Invoke(this, new BlockingTaskEventArgs(this, this));
    }
}

#endif
