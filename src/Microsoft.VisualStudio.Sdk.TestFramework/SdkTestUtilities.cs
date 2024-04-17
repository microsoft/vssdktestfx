// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETFRAMEWORK || WINDOWS

using System.IO;
using ComAsyncServiceProvider = Microsoft.VisualStudio.Shell.Interop.COMAsyncServiceProvider.IAsyncServiceProvider;
using OleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace Microsoft.VisualStudio.Sdk.TestFramework;

/// <summary>
/// Test utilities for VS SDK tests.
/// </summary>
public static class SdkTestUtilities
{
    /// <summary>
    /// Initializes an instance of an <see cref="AsyncPackage"/>.
    /// </summary>
    /// <typeparam name="T">The package to initialize.</typeparam>
    /// <param name="logger">An optional logger that will receive progress updates.</param>
    /// <returns>The loaded package.</returns>
    public static async Task<T> LoadPackageAsync<T>(Action<string>? logger = null)
        where T : AsyncPackage, new()
    {
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread -- All the APIs here are free-threaded.
        ServiceProvider globalProvider = ServiceProvider.GlobalProvider;

        T package = new();
        IVsPackage pkg = package;
        IAsyncLoadablePackageInitialize asyncPackage = package;

        try
        {
            OleServiceProvider oleServiceProvider = globalProvider.GetService<OleServiceProvider, OleServiceProvider>();
            pkg.SetSite(oleServiceProvider);

            ComAsyncServiceProvider comAsyncServiceProvider = globalProvider.GetService<SAsyncServiceProvider, ComAsyncServiceProvider>();
            Mocks.MockProfferAsyncService promotedServices = new(GlobalServiceProvider.ThisInstance, logger);
            await asyncPackage.Initialize(comAsyncServiceProvider, promotedServices, promotedServices.GetServiceProgressCallback());

            return package;
        }
        catch
        {
            pkg.Close();
            throw;
        }
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
    }
}

#endif
