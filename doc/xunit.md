# Xunit instructions

These instructions apply when consuming this test framework from an Xunit project.
This is just a subset of the full instructions outlined in [our README](../README.md).

Add this class to your project (if a MockedVS.cs file was not added to your project automatically):

```csharp
using Xunit;
using Microsoft.VisualStudio.Sdk.TestFramework;

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
```

Then for *each* of your test classes, apply the `Collection` attribute and
add a parameter and statement to your constructor:

```csharp
using Microsoft.VisualStudio.Sdk.TestFramework;

[Collection(MockedVS.Collection)]
public class YourTestClass
{
    public TestFrameworkTests(GlobalServiceProvider sp)
    {
        sp.Reset();
    }
}
```
