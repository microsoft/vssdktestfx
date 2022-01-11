// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Sdk.TestFramework
{
    using Xunit;

    /// <summary>
    /// Defines the "MockedVS" xunit test collection.
    /// </summary>
    [CollectionDefinition(Collection)]
    public class MockedVS : ICollectionFixture<GlobalServiceProvider>, ICollectionFixture<MefHostingFixture>
    {
        /// <summary>
        /// The name of the xunit test collection.
        /// </summary>
        public const string Collection = "MockedVS";
    }
}
