// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETFRAMEWORK || WINDOWS

using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.Sdk.TestFramework.Mocks;

/// <summary>
/// An adapter that can log to Xunit's <see cref="ITestOutputHelper"/>
/// when an instance of this class is set to the <see cref="MockVsActivityLog.ForwardTo"/> property.
/// </summary>
public class MockVsActivityLogXunitAdapter : IVsActivityLog
{
    private const string Prefix = "VsActivityLog";

    /// <summary>
    /// The xunit logger.
    /// </summary>
    private readonly ITestOutputHelper logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MockVsActivityLogXunitAdapter"/> class.
    /// </summary>
    /// <param name="logger">The Xunit logger to write events to.</param>
    public MockVsActivityLogXunitAdapter(ITestOutputHelper logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc/>
    public int LogEntry(uint actType, string pszSource, string pszDescription)
    {
        var type = (__ACTIVITYLOG_ENTRYTYPE)actType;
        this.logger.WriteLine($"{Prefix} {type} {pszSource}: {pszDescription}");
        return VSConstants.S_OK;
    }

    /// <inheritdoc/>
    public int LogEntryGuid(uint actType, string pszSource, string pszDescription, Guid guid)
    {
        var type = (__ACTIVITYLOG_ENTRYTYPE)actType;
        this.logger.WriteLine($"{Prefix} {type} {pszSource} ({guid}): {pszDescription}");
        return VSConstants.S_OK;
    }

    /// <inheritdoc/>
    public int LogEntryHr(uint actType, string pszSource, string pszDescription, int hr)
    {
        var type = (__ACTIVITYLOG_ENTRYTYPE)actType;
        this.logger.WriteLine($"{Prefix} {type} {pszSource} (HRESULT {hr}): {pszDescription}");
        return VSConstants.S_OK;
    }

    /// <inheritdoc/>
    public int LogEntryGuidHr(uint actType, string pszSource, string pszDescription, Guid guid, int hr)
    {
        var type = (__ACTIVITYLOG_ENTRYTYPE)actType;
        this.logger.WriteLine($"{Prefix} {type} {pszSource} (HRESULT {hr}, {guid}): {pszDescription}");
        return VSConstants.S_OK;
    }

    /// <inheritdoc/>
    public int LogEntryPath(uint actType, string pszSource, string pszDescription, string pszPath)
    {
        var type = (__ACTIVITYLOG_ENTRYTYPE)actType;
        this.logger.WriteLine($"{Prefix} {type} {pszSource} ({pszPath}): {pszDescription}");
        return VSConstants.S_OK;
    }

    /// <inheritdoc/>
    public int LogEntryGuidPath(uint actType, string pszSource, string pszDescription, Guid guid, string pszPath)
    {
        var type = (__ACTIVITYLOG_ENTRYTYPE)actType;
        this.logger.WriteLine($"{Prefix} {type} {pszSource} ({guid}, {pszPath}): {pszDescription}");
        return VSConstants.S_OK;
    }

    /// <inheritdoc/>
    public int LogEntryHrPath(uint actType, string pszSource, string pszDescription, int hr, string pszPath)
    {
        var type = (__ACTIVITYLOG_ENTRYTYPE)actType;
        this.logger.WriteLine($"{Prefix} {type} {pszSource} (HRESULT: {hr}, {pszPath}): {pszDescription}");
        return VSConstants.S_OK;
    }

    /// <inheritdoc/>
    public int LogEntryGuidHrPath(uint actType, string pszSource, string pszDescription, Guid guid, int hr, string pszPath)
    {
        var type = (__ACTIVITYLOG_ENTRYTYPE)actType;
        this.logger.WriteLine($"{Prefix} {type} {pszSource} ({guid}, HRESULT: {hr}, {pszPath}): {pszDescription}");
        return VSConstants.S_OK;
    }
}

#endif
