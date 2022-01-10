// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public abstract class LoggingTestBase : TestBase
{
    private CancellationTokenRegistration timeoutLoggerRegistration;

    public LoggingTestBase(ITestOutputHelper logger)
    {
        this.Logger = logger;
        this.timeoutLoggerRegistration = this.TimeoutToken.Register(() => this.Logger.WriteLine($"TEST TIMEOUT: {nameof(TestBase)}.{nameof(this.TimeoutToken)} has been canceled due to the test exceeding the {this.UnexpectedTimeout} time limit."));
    }

    public ITestOutputHelper Logger { get; }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.timeoutLoggerRegistration.Dispose();
            (this.Logger as IDisposable)?.Dispose();
        }

        base.Dispose(disposing);
    }
}
