// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Sdk.TestFramework;

/// <summary>
/// Interface for service mocks that need to interact with a client callback proxy.
/// </summary>
public interface IMockServiceWithClientCallback
{
    /// <summary>
    /// Gets or sets the proxy to a client callback object.
    /// </summary>
    public object ClientCallback { get; set; }
}
