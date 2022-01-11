// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Sdk.TestFramework;

using System;
using System.Buffers;
using System.Diagnostics;
using System.Text;

/// <summary>
/// A <see cref="TraceListener"/> adapter for <see cref="ITestOutputHelper"/>.
/// </summary>
internal class XunitTraceListener : TraceListener
{
    private readonly ITestOutputHelper logger;
    private readonly int testId;
    private readonly Stopwatch testTime;
    private readonly StringBuilder lineInProgress = new StringBuilder();
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="XunitTraceListener"/> class.
    /// </summary>
    /// <param name="logger">The Xunit logger.</param>
    /// <param name="testId">A unique ID assigned to the test that began running when this instance was created.</param>
    /// <param name="testTime">The stopwatch that tracks this test's running time.</param>
    internal XunitTraceListener(ITestOutputHelper logger, int testId, Stopwatch testTime)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.testId = testId;
        this.testTime = testTime;
    }

    /// <summary>
    /// Gets or sets the <see cref="Encoding"/> to use to decode the data for readable trace messages
    /// if the data is encoded text.
    /// </summary>
    public Encoding? DataEncoding { get; set; }

    /// <inheritdoc/>
    public override bool IsThreadSafe => false;

    /// <inheritdoc/>
    public override unsafe void TraceData(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, object? data)
    {
        if (data is ReadOnlySequence<byte> sequence)
        {
            // Trim the traced output in case it's ridiculously huge.
            const int maxLength = 100;
            bool truncated = false;
            if (sequence.Length > maxLength)
            {
                sequence = sequence.Slice(0, maxLength);
                truncated = true;
            }

            var sb = new StringBuilder(2 + ((int)sequence.Length * 2));
            Decoder? decoder = this.DataEncoding?.GetDecoder();
            sb.Append(decoder is not null ? "\"" : "0x");
            foreach (ReadOnlyMemory<byte> segment in sequence)
            {
                if (decoder is not null)
                {
                    // Write out decoded characters.
                    using (MemoryHandle segmentPointer = segment.Pin())
                    {
                        int charCount = decoder.GetCharCount((byte*)segmentPointer.Pointer, segment.Length, false);
                        char[] chars = ArrayPool<char>.Shared.Rent(charCount);
                        try
                        {
                            fixed (char* pChars = &chars[0])
                            {
                                int actualCharCount = decoder.GetChars((byte*)segmentPointer.Pointer, segment.Length, pChars, charCount, flush: false);
                                sb.Append(pChars, actualCharCount);
                            }
                        }
                        finally
                        {
                            ArrayPool<char>.Shared.Return(chars);
                        }
                    }
                }
                else
                {
                    // Write out data blob as hex
                    for (int i = 0; i < segment.Length; i++)
                    {
                        sb.AppendFormat("{0:X2}", segment.Span[i]);
                    }
                }
            }

            if (decoder is not null)
            {
                int charCount = decoder.GetCharCount(Array.Empty<byte>(), 0, 0, flush: true);
                if (charCount > 0)
                {
                    char[] chars = ArrayPool<char>.Shared.Rent(charCount);
                    try
                    {
                        int actualCharCount = decoder.GetChars(Array.Empty<byte>(), 0, 0, chars, 0, flush: true);
                        sb.Append(chars, 0, actualCharCount);
                    }
                    finally
                    {
                        ArrayPool<char>.Shared.Return(chars);
                    }
                }

                sb.Append('"');
            }

            if (truncated)
            {
                sb.Append("...");
            }

            this.logger.WriteLine(sb.ToString());
        }
    }

    /// <inheritdoc/>
    public override void Write(string? message) => this.lineInProgress.Append(message);

    /// <inheritdoc/>
    public override void WriteLine(string? message)
    {
        if (!this.disposed)
        {
            this.logger.WriteLine($"[{this.testId,4} {this.testTime.Elapsed.TotalSeconds:00.00}] {this.lineInProgress}{message}");
            this.lineInProgress.Clear();
        }
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        this.disposed = true;
        base.Dispose(disposing);
    }
}
