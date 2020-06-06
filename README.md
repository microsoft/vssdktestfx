# Visual Studio SDK Test Framework

The VS SDK Test Framework is a library for your unit tests that exercise VS code to use
so that certain core VS functionality works outside the VS process so your unit tests can function.
For example, `ThreadHelper` and obtaining global services from the static `ServiceProvider`
tend to fail in unit tests without this library installed.

## Referencing

### For tests that build outside the VS repo:

To reference this test framework outside the VS repo,
you will need these feeds as package sources in your [nuget.config file](https://docs.microsoft.com/en-us/nuget/schema/nuget-config-file#packagesources):

    https://api.nuget.org/v3/index.json (this is the default feed)
    https://pkgs.dev.azure.com/devdiv/_packaging/vs-impl/nuget/v3/index.json"

Then install its NuGet package:

    Install-Package Microsoft.VisualStudio.Sdk.TestFramework -Pre

Make sure your unit test project generates the required binding redirects by adding these two properties to your project file:

```xml
<AutoGenerateBindingRedirects>true</AutoGenerateBindingRedirects>
<GenerateBindingRedirectsOutputType>true</GenerateBindingRedirectsOutputType>
```

### For tests that build within the VS repo:

Add this import to your project:

```xml
<Import Project="$(SrcRoot)\Tests\vssdktestfx.targets" />
```

Remove any references to an App.config or App.config.tt file from your test project
since this is now generated during the build automatically.

## Unit test source code changes

### Xunit instructions

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

### MSTest instructions

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

For a sample of applying this pattern to an MSTest project within the VS repo, check out [this pull request](https://devdiv.visualstudio.com/DevDiv/default/_git/VS/pullrequest/57056?_a=files&path=%2Fsrc%2Fenv%2Fshell%2FConnected%2Ftests).

## Main Thread considerations

This library will create a mocked up UI thread, such that `ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync()`
can switch to it. Your unit tests do *not* start on this mocked up UI thread. If your product code contains checks
that it is invoked on the UI thread (e.g. `ThreadHelper.ThrowIfNotOnUIThread()`) your test method should look like this:

```csharp
[TestMethod] // or [Fact]
public async Task VerifyWeDoSomethingGood()
{
    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
    MyVSPackage.DoSomethingAwesome();
}
```

## Built in service mocks

There are a collection of "base services" that the VSSDKTestFx comes with mocks for.
Calling `GlobalServiceProvider.AddService` for any of these will result in an `InvalidOperationException` being thrown claiming
that the service is already added.

These services include:

* `SVsActivityLog`
* `OLE.Interop.IServiceProvider`
* `SVsTaskSchedulerService`
* `SVsUIThreadInvokerPrivate`

More may be added and can be found in [source code](https://devdiv.visualstudio.com/DefaultCollection/DevDiv/default/_git/VSSDKTestFx?_a=contents&path=%2Fsrc%2FMicrosoft.VisualStudio.Sdk.TestFramework%2FGlobalServiceProvider.cs&version=GBmaster&line=194&lineStyle=plain&lineEnd=198&lineStartColumn=1&lineEndColumn=1).

## Contributing

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).

See our [contributing doc](CONTRIBUTING.md) for more info.
