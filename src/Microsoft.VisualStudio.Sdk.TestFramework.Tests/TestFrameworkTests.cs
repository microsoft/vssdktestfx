/********************************************************
*                                                        *
*   © Copyright (C) Microsoft. All rights reserved.      *
*                                                        *
*********************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Sdk.TestFramework;
using Microsoft.VisualStudio.Sdk.TestFramework.Tests;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Xunit;
using Task = System.Threading.Tasks.Task;

[Collection(MockedVS.Collection)]
public class TestFrameworkTests
{
    private readonly MefHosting mef;

    public TestFrameworkTests(GlobalServiceProvider sp, MefHosting mef)
    {
        this.mef = mef;
        sp.Reset();
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

        ThreadHelper.ThrowIfNotOnUIThread();
        Assert.Throws<COMException>(() => ThreadHelper.ThrowIfOnUIThread());
    }

    [Fact]
    public async Task TestAssemblyIsInMefCatalog()
    {
        var ep = await this.mef.CreateExportProviderAsync();
        Assert.NotNull(ep.GetExportedValue<SomeMefExport>());
    }
}
