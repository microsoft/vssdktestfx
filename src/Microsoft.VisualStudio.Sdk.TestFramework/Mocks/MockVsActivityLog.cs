// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Sdk.TestFramework.Mocks;

// This interface is actually free-threaded.
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread

/// <summary>
/// An implementation of <see cref="IVsActivityLog"/> for testing.
/// </summary>
public class MockVsActivityLog : IVsActivityLog
{
    /// <summary>
    /// Gets or sets an implementation to forward all calls to.
    /// </summary>
    /// <remarks>
    /// When this is <see langword="null" />, all methods on this object will simply return <see cref="VSConstants.S_OK"/>.
    /// </remarks>
    public IVsActivityLog? ForwardTo { get; set; }

    /// <inheritdoc/>
    public int LogEntry(uint actType, string pszSource, string pszDescription)
    {
        return this.ForwardTo is IVsActivityLog forwarder ? forwarder.LogEntry(actType, pszSource, pszDescription) : VSConstants.S_OK;
    }

    /// <inheritdoc/>
    public int LogEntryGuid(uint actType, string pszSource, string pszDescription, Guid guid)
    {
        return this.ForwardTo is IVsActivityLog forwarder
            ? forwarder.LogEntryGuid(actType, pszSource, pszDescription, guid)
            : VSConstants.S_OK;
    }

    /// <inheritdoc/>
    public int LogEntryHr(uint actType, string pszSource, string pszDescription, int hr)
    {
        return this.ForwardTo is IVsActivityLog forwarder ? forwarder.LogEntryHr(actType, pszSource, pszDescription, hr) : VSConstants.S_OK;
    }

    /// <inheritdoc/>
    public int LogEntryGuidHr(uint actType, string pszSource, string pszDescription, Guid guid, int hr)
    {
        return this.ForwardTo is IVsActivityLog forwarder
            ? forwarder.LogEntryGuidHr(actType, pszSource, pszDescription, guid, hr)
            : VSConstants.S_OK;
    }

    /// <inheritdoc/>
    public int LogEntryPath(uint actType, string pszSource, string pszDescription, string pszPath)
    {
        return this.ForwardTo is IVsActivityLog forwarder
            ? forwarder.LogEntryPath(actType, pszSource, pszDescription, pszPath)
            : VSConstants.S_OK;
    }

    /// <inheritdoc/>
    public int LogEntryGuidPath(uint actType, string pszSource, string pszDescription, Guid guid, string pszPath)
    {
        return this.ForwardTo is IVsActivityLog forwarder
            ? forwarder.LogEntryGuidPath(actType, pszSource, pszDescription, guid, pszPath)
            : VSConstants.S_OK;
    }

    /// <inheritdoc/>
    public int LogEntryHrPath(uint actType, string pszSource, string pszDescription, int hr, string pszPath)
    {
        return this.ForwardTo is IVsActivityLog forwarder
            ? forwarder.LogEntryHrPath(actType, pszSource, pszDescription, hr, pszPath)
            : VSConstants.S_OK;
    }

    /// <inheritdoc/>
    public int LogEntryGuidHrPath(uint actType, string pszSource, string pszDescription, Guid guid, int hr, string pszPath)
    {
        return this.ForwardTo is IVsActivityLog forwarder
            ? forwarder.LogEntryGuidHrPath(actType, pszSource, pszDescription, guid, hr, pszPath)
            : VSConstants.S_OK;
    }
}
