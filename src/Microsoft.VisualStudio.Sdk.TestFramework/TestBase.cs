// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Sdk.TestFramework;

using System.Diagnostics;

/// <summary>
/// A base class that offers some commonly useful tools for testing.
/// </summary>
public abstract class TestBase : IDisposable
{
    private readonly CancellationTokenSource timeoutTokenSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestBase"/> class.
    /// </summary>
    public TestBase()
    {
        this.timeoutTokenSource = new CancellationTokenSource(this.UnexpectedTimeout);
    }

    /// <summary>
    /// Gets a newly initialized <see cref="CancellationToken"/> that is canceled after <see cref="ExpectedTimeout"/> expires
    /// from the time this property is retrieved.
    /// </summary>
    protected CancellationToken ExpectedTimeoutToken => new CancellationTokenSource(this.ExpectedTimeout).Token;

    /// <summary>
    /// Gets a newly initialized <see cref="CancellationToken"/> that is canceled after <see cref="UnexpectedTimeout"/> expires
    /// from the time this property is retrieved.
    /// </summary>
    protected CancellationToken UnexpectedTimeoutToken => new CancellationTokenSource(this.UnexpectedTimeout).Token;

    /// <summary>
    /// Gets a reasonably short time period to wait where a timeout <em>is</em> expected.
    /// </summary>
    protected virtual TimeSpan ExpectedTimeout => TimeSpan.FromMilliseconds(200);

    /// <summary>
    /// Gets a reasonably long time period to wait where we expect some other event to occur rather than normal test execution waiting this whole time period.
    /// </summary>
    protected virtual TimeSpan UnexpectedTimeout => Debugger.IsAttached ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets a token that gets canceled <see cref="UnexpectedTimeout"/> after the test class is instantiated.
    /// </summary>
    protected CancellationToken TimeoutToken => Debugger.IsAttached ? CancellationToken.None : this.timeoutTokenSource.Token;

    /// <summary>
    /// Gets a stopwatch that is started when the test class is instantiated.
    /// </summary>
    protected Stopwatch TestStopwatch { get; } = Stopwatch.StartNew();

    /// <inheritdoc/>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes of managed and unmanaged resources owned by this object.
    /// </summary>
    /// <param name="disposing"><see langword="true"/> if this object is being disposed of; <see langword="false"/> if it is being finalized.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.timeoutTokenSource.Dispose();
        }
    }
}
