// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading;
using Xunit.Abstractions;

public abstract class TestBase : IDisposable
{
    protected static readonly TimeSpan ExpectedTimeout = TimeSpan.FromMilliseconds(200);

    protected static readonly TimeSpan UnexpectedTimeout = Debugger.IsAttached ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(5);

    private readonly CancellationTokenSource timeoutTokenSource;

    protected TestBase(ITestOutputHelper logger)
    {
        this.Logger = logger;
        this.timeoutTokenSource = new CancellationTokenSource(TestTimeout);
        this.timeoutTokenSource.Token.Register(() => this.Logger.WriteLine($"TEST TIMEOUT: {nameof(TestBase)}.{nameof(this.TimeoutToken)} has been canceled due to the test exceeding the {TestTimeout} time limit."));
    }

    protected static CancellationToken ExpectedTimeoutToken => new CancellationTokenSource(ExpectedTimeout).Token;

    protected static CancellationToken UnexpectedTimeoutToken => new CancellationTokenSource(UnexpectedTimeout).Token;

    protected static bool IsTestRunOnAzurePipelines => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SYSTEM_DEFINITIONID"));

    protected ITestOutputHelper Logger { get; }

    protected CancellationToken TimeoutToken => Debugger.IsAttached ? CancellationToken.None : this.timeoutTokenSource.Token;

    private static TimeSpan TestTimeout => UnexpectedTimeout;

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.timeoutTokenSource.Dispose();
        }
    }
}
