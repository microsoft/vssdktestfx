// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETFRAMEWORK || WINDOWS

[Collection(MockedVS.Collection)]
public class VsTaskTests
{
    public VsTaskTests(GlobalServiceProvider sp)
    {
        sp.Reset();
    }

    [Fact]
    public async Task CanAwaitCancelledTask()
    {
        var cancelledToken = new CancellationToken(true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () =>
            {
                await ThreadHelper.JoinableTaskFactory.RunAsyncAsVsTask(
                    VsTaskRunContext.UIThreadNormalPriority,
                    (ct) => Task.FromCanceled<bool>(cancelledToken));
            });
    }

    [Fact]
    public async Task CanAwaitFaultedTask()
    {
        await Assert.ThrowsAsync<Exception>(
            async () =>
            {
                await ThreadHelper.JoinableTaskFactory.RunAsyncAsVsTask(
                    VsTaskRunContext.UIThreadNormalPriority,
                    (ct) => Task.FromException<bool>(new Exception()));
            });
    }

    [Fact]
    public async Task CanAwaitCompletedTask()
    {
        var result = (bool)await ThreadHelper.JoinableTaskFactory.RunAsyncAsVsTask(
            VsTaskRunContext.UIThreadNormalPriority,
            (ct) => Task.FromResult(true));

        Assert.True(result);
    }
}

#endif
