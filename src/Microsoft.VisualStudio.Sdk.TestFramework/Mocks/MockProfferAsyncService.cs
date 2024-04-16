// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETFRAMEWORK || WINDOWS

using System.IO;
using Microsoft;
using Microsoft.VisualStudio.Shell.Interop;
using ComAsyncServiceProvider = Microsoft.VisualStudio.Shell.Interop.COMAsyncServiceProvider.IAsyncServiceProvider;

namespace Microsoft.VisualStudio.Sdk.TestFramework.Mocks;

/// <summary>
/// A working implementation of <see cref="IProfferAsyncService"/>.
/// </summary>
/// <param name="globalServiceProvider">The global service provider to which proffered services will be added.</param>
/// <param name="logger">A means to log progress as it is reported.</param>
public class MockProfferAsyncService(GlobalServiceProvider? globalServiceProvider, Action<string>? logger) : IProfferAsyncService
{
    private readonly List<Guid?> profferedCookies = new();

    /// <summary>
    /// Gets a dictionary of proffered services.
    /// </summary>
    public Dictionary<Guid, ComAsyncServiceProvider> ProfferedServices { get; } = new();

    /// <summary>
    /// Checks whether a service has been proffered.
    /// </summary>
    /// <param name="serviceType">The <see cref="Type"/> used for the service IID.</param>
    /// <returns><see langword="true"/> if the service is proffered; otherwise <see langword="false" />.</returns>
    public bool IsServiceProffered(Type serviceType)
    {
        Requires.NotNull(serviceType);
        return this.ProfferedServices.ContainsKey(serviceType.GUID);
    }

    /// <inheritdoc/>
    public IAsyncProgressCallback GetServiceProgressCallback()
    {
        return new MockAsyncProgressCallback(logger);
    }

    /// <inheritdoc/>
    uint IProfferAsyncService.ProfferAsyncService(ref Guid rguidService, ComAsyncServiceProvider psp)
    {
        Guid serviceGuid = rguidService;
        globalServiceProvider?.AddService(rguidService, async () => await psp.QueryServiceAsync(ref serviceGuid));
        this.ProfferedServices.Add(rguidService, psp);
        this.profferedCookies.Add(rguidService);

        // The cookie must never be 0 for a properly proffered service,
        // since that equals VSCOOKIE_NIL.
        // So we'll have our cookie be a 1-based index into our array.
        uint cookie = (uint)this.profferedCookies.Count;
        return cookie;
    }

    /// <inheritdoc/>
    void IProfferAsyncService.RevokeAsyncService(uint dwCookie)
    {
        if (dwCookie == VSConstants.VSCOOKIE_NIL)
        {
            return;
        }

        int index = (int)dwCookie - 1;
        Guid? guid = this.profferedCookies[index];
        Verify.Operation(guid.HasValue, "Invalid cookie.");
        this.ProfferedServices.Remove(guid.Value);
        this.profferedCookies[index] = null;
    }

    private class MockAsyncProgressCallback(Action<string>? logger) : IAsyncProgressCallback
    {
        public void ReportProgress(ref Guid guidService, string szWaitMessage, string szProgressText, int iCurrentStep, int iTotalSteps)
        {
            logger?.Invoke($"Progress: {szWaitMessage} {szProgressText} {iCurrentStep}/{iTotalSteps}");
        }
    }
}

#endif
