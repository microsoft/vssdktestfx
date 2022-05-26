// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETFRAMEWORK || WINDOWS

namespace Microsoft.VisualStudio.Sdk.TestFramework.Mocks;

using System.Runtime.InteropServices.ComTypes;
using Microsoft.VisualStudio.OLE.Interop;

/// <summary>
/// A mock implementation of <see cref="ILocalRegistry"/>.
/// </summary>
internal class MockLocalRegistry : ILocalRegistry
{
    /// <summary>
    /// Gets or sets a factory for objects that may be created by this mock.
    /// </summary>
    internal Func<Guid, object>? Factory { get; set; }

    /// <inheritdoc/>
    public int CreateInstance(Guid clsid, object punkOuter, ref Guid riid, uint dwFlags, out IntPtr ppvObj)
    {
        if (punkOuter is object || (CLSCTX)dwFlags != CLSCTX.CLSCTX_INPROC_SERVER)
        {
            // These are arguments we do not support.
            ppvObj = IntPtr.Zero;
            return VSConstants.E_NOTIMPL;
        }

        if (this.Factory?.Invoke(clsid) is object service)
        {
            IntPtr punk = IntPtr.Zero;
            try
            {
                punk = Marshal.GetIUnknownForObject(service);
                return Marshal.QueryInterface(punk, ref riid, out ppvObj);
            }
            finally
            {
                if (punk != IntPtr.Zero)
                {
                    Marshal.Release(punk);
                }
            }
        }
        else
        {
            ppvObj = IntPtr.Zero;
            return VSConstants.E_INVALIDARG;
        }
    }

    /// <inheritdoc/>
    public int GetClassObjectOfClsid(ref Guid clsid, uint dwFlags, IntPtr lpReserved, ref Guid riid, out IntPtr ppvClassObject)
    {
        ppvClassObject = IntPtr.Zero;
        return VSConstants.E_NOTIMPL;
    }

    /// <inheritdoc/>
    public int GetTypeLibOfClsid(Guid clsid, out ITypeLib? pptLib)
    {
        pptLib = null;
        return VSConstants.E_NOTIMPL;
    }
}

#endif
