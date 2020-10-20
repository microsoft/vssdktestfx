# MSTest instructions

These instructions apply when consuming this test framework from an MSTest project.
This is just a subset of the full instructions outlined in [our README](../README.md).

Add these members to *one* of your test classes:

```csharp
using Microsoft.VisualStudio.Sdk.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class AssemblyFixture
{
    internal static GlobalServiceProvider MockServiceProvider { get; private set; }

    [AssemblyInitialize]
    public static void AssemblyInit(TestContext context)
    {
        MockServiceProvider = new GlobalServiceProvider();
    }

    [AssemblyCleanup]
    public static void AssemblyCleanup()
    {
        MockServiceProvider.Dispose();
    }
}
```

Then in *each* of your test classes, Reset() the static service provider created earlier:

```csharp
using Microsoft.VisualStudio.Sdk.TestFramework;

[TestInitialize]
public void TestInit()
{
    AssemblyFixture.MockServiceProvider.Reset();
}
```

For a sample of applying this pattern to an MSTest project within the VS repo, check out [this pull request](https://dev.azure.com/devdiv/DevDiv/_git/VS/pullrequest/57056?_a=files&path=%2Fsrc%2Fenv%2Fshell%2FConnected%2Ftests).
