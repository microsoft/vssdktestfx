// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Sdk.TestFramework;

/// <summary>
/// Provides VS MEF hosting facilities for unit tests as an xunit fixture.
/// </summary>
/// <remarks>
/// This class is equivalent to <see cref="MefHosting" /> except that it only offers one public constructor,
/// as is required of an xunit fixture.
/// </remarks>
public class MefHostingFixture : MefHosting
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MefHostingFixture"/> class
    /// where all assemblies in the test project's output directory are
    /// included in the MEF catalog.
    /// </summary>
    public MefHostingFixture()
    {
    }
}
