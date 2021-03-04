// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft;
using Microsoft.VisualStudio.Sdk.TestFramework;
using Microsoft.VisualStudio.Sdk.TestFramework.Tests;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using StreamJsonRpc;
using Xunit;
using Xunit.Abstractions;
using OleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using Task = System.Threading.Tasks.Task;

#pragma warning disable CS0618 // We intentionally test APIs that are obsolete
#pragma warning disable VSTHRD001 // We intentionally test APIs that are obsolete

[Collection(MockedVS.Collection)]
public class TestFrameworkTests : TestBase
{
    private readonly MefHosting mef;
    private readonly GlobalServiceProvider container;

    public TestFrameworkTests(GlobalServiceProvider sp, MefHostingFixture mef, ITestOutputHelper logger)
        : base(logger)
    {
        Requires.NotNull(sp, nameof(sp));
        this.mef = mef;
        sp.Reset();
        this.container = sp;
    }

    private interface ITestService
    {
        int Add(int a, int b);

        void Throw();
    }

    [Fact]
    public async Task OleServiceProviderIsService()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        object sp = ServiceProvider.GlobalProvider.GetService(typeof(OleServiceProvider));
        Assert.IsAssignableFrom<OleServiceProvider>(sp);
    }

    [Fact]
    public async Task ServicesObtainableViaAsyncServiceProvider()
    {
        object? activityLog = await AsyncServiceProvider.GlobalProvider.GetServiceAsync(typeof(SVsActivityLog));
        Assert.IsAssignableFrom<IVsActivityLog>(activityLog);
    }

    [Fact]
    public async Task MainThreadHandling()
    {
        if (ThreadHelper.JoinableTaskContext.MainThread == Thread.CurrentThread)
        {
            // Get off the "main thread" so we can switch back.
            await TaskScheduler.Default;
            Assert.NotSame(ThreadHelper.JoinableTaskContext.MainThread, Thread.CurrentThread);
        }

        ThreadHelper.ThrowIfOnUIThread();
        Assert.Throws<COMException>(() => ThreadHelper.ThrowIfNotOnUIThread());

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        Assert.Same(ThreadHelper.JoinableTaskContext.MainThread, Thread.CurrentThread);

#pragma warning disable VSTHRD109 // Switch instead of assert in async methods
        ThreadHelper.ThrowIfNotOnUIThread();
#pragma warning restore VSTHRD109 // Switch instead of assert in async methods
        Assert.Throws<COMException>(() => ThreadHelper.ThrowIfOnUIThread());
    }

    [Fact]
    public async Task TestAssemblyIsInMefCatalog()
    {
        Microsoft.VisualStudio.Composition.ExportProvider ep = await this.mef.CreateExportProviderAsync();
        Assert.NotNull(ep.GetExportedValue<SomeMefExport>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ThreadHelper_Invoke(bool fromMainThread)
    {
        if (ThreadHelper.CheckAccess() && !fromMainThread)
        {
            // Get off the "main thread" so we can switch back.
            await TaskScheduler.Default;
            Assert.False(ThreadHelper.CheckAccess());
        }
        else if (fromMainThread && !ThreadHelper.CheckAccess())
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            Assert.True(ThreadHelper.CheckAccess());
        }

        bool delegateExecuted = false;
        ThreadHelper.Generic.Invoke(delegate
        {
            Assert.True(ThreadHelper.CheckAccess());
            ThreadHelper.ThrowIfNotOnUIThread();
            delegateExecuted = true;
        });
        Assert.True(delegateExecuted);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ThreadHelper_Invoke_PropagatesExceptions(bool fromMainThread)
    {
        if (ThreadHelper.CheckAccess() && !fromMainThread)
        {
            // Get off the "main thread" so we can switch back.
            await TaskScheduler.Default;
            Assert.False(ThreadHelper.CheckAccess());
        }
        else if (fromMainThread && !ThreadHelper.CheckAccess())
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            Assert.True(ThreadHelper.CheckAccess());
        }

        // Verify that exceptions are propagated.
        Assert.Throws<ApplicationException>(() =>
        {
            ThreadHelper.Generic.Invoke(delegate
            {
                Assert.True(ThreadHelper.CheckAccess());
                ThreadHelper.ThrowIfNotOnUIThread();
                throw new ApplicationException();
            });
        });
    }

    [Fact]
    public async Task ThreadHelper_BeginInvoke()
    {
        var delegateExecuted = new AsyncManualResetEvent();
        ThreadHelper.Generic.BeginInvoke(delegate
        {
            delegateExecuted.Set();
        });
        await delegateExecuted.WaitAsync();
    }

    [Fact]
    public async Task ThreadHelper_InvokeAsync()
    {
        bool delegateExecuted = false;
        Task t = ThreadHelper.Generic.InvokeAsync(delegate
        {
            delegateExecuted = true;
        });
        await t;
        Assert.True(delegateExecuted);
    }

    [Fact]
    public async Task VsTaskScheduler_UISchedulersWork()
    {
        await ThreadHelper.JoinableTaskFactory.RunAsync(
            VsTaskRunContext.UIThreadIdlePriority,
            async delegate
            {
                await Task.Yield();
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            });
    }

    [Fact]
    public async Task RunAsyncAsVsTask()
    {
        bool result = (bool)await ThreadHelper.JoinableTaskFactory.RunAsyncAsVsTask(
            VsTaskRunContext.UIThreadNormalPriority,
            async ct =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                Assert.True(ThreadHelper.CheckAccess());
                return true;
            });
        Assert.True(result);
    }

    [Fact]
    public async Task YieldWorksOnUIThread()
    {
        var delegateInvoked = new AsyncManualResetEvent();
        async Task Helper()
        {
            await Task.Yield();
            delegateInvoked.Set();
        }

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        Helper().Forget();
        Assert.False(delegateInvoked.IsSet);
        await delegateInvoked.WaitAsync();
    }

    [Fact]
    public async Task StartOnIdleExecutesDelegateLater()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var delegateInvoked = new AsyncManualResetEvent();
        ThreadHelper.JoinableTaskFactory.StartOnIdle(delegate
        {
            delegateInvoked.Set();
        }).Task.Forget();
        Assert.False(delegateInvoked.IsSet);
        await delegateInvoked.WaitAsync();
    }

    [Fact]
    public async Task ExplicitVsTaskCreation()
    {
        IVsTask vsTask = VsTaskLibraryHelper.CreateAndStartTask(
            VsTaskLibraryHelper.ServiceInstance,
            VsTaskRunContext.UIThreadNormalPriority,
            VsTaskLibraryHelper.CreateTaskBody(() => Assert.True(ThreadHelper.CheckAccess())));
        await vsTask;
    }

    [Fact]
    public async Task AwaitingIVsTaskPreservesContext()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        await ThreadHelper.JoinableTaskFactory.RunAsyncAsVsTask(
            VsTaskRunContext.UIThreadBackgroundPriority,
            async ct =>
            {
                await Task.Delay(100);
                return 0;
            });
#pragma warning disable VSTHRD109 // Switch instead of assert in async methods
        ThreadHelper.ThrowIfNotOnUIThread();
#pragma warning restore VSTHRD109 // Switch instead of assert in async methods
    }

    [Fact]
    public async Task AddService()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        object expected = new object();
        this.container.AddService(typeof(SVsProjectMRU), expected);
        Assert.Same(expected, ServiceProvider.GlobalProvider.GetService(typeof(SVsProjectMRU)));
    }

    [Fact]
    public async Task AddService_TwiceThrows()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        object expected = new object();
        this.container.AddService(typeof(SVsProjectMRU), expected);
        Assert.Throws<InvalidOperationException>(() => this.container.AddService(typeof(SVsProjectMRU), new object()));
        Assert.Same(expected, ServiceProvider.GlobalProvider.GetService(typeof(SVsProjectMRU)));
    }

    [Fact]
    public async Task AddService_ExistingMock()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        object expected = new object();
        this.container.AddService(typeof(SVsActivityLog), expected);
        Assert.Same(expected, ServiceProvider.GlobalProvider.GetService(typeof(SVsActivityLog)));
    }

    private class TestService : ITestService
    {
        public int Add(int a, int b) => a + b;

        public void Throw() => throw new InvalidOperationException("Test");
    }
}
