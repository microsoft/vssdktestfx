# Visual Studio SDK Test Framework

[![NuGet package](https://img.shields.io/nuget/v/Microsoft.VisualStudio.Sdk.TestFramework.svg)](https://nuget.org/packages/Microsoft.VisualStudio.Sdk.TestFramework)
[![Build Status](https://dev.azure.com/azure-public/vside/_apis/build/status/vssdktestfx?branchName=main)](https://dev.azure.com/azure-public/vside/_build/latest?definitionId=45&branchName=main)
[![Join the chat at https://gitter.im/Microsoft/extendvs](https://badges.gitter.im/extendvs/Lobby.svg)](https://gitter.im/Microsoft/extendvs)

The VS SDK Test Framework is a library for your unit tests that exercise VS code to use
so that certain core VS functionality works outside the VS process so your unit tests can function.
For example, `ThreadHelper` and obtaining global services from the static `ServiceProvider`
tend to fail in unit tests without this library installed.

## Consuming this test framework

**Microsoft Internal users**: See [specific guidance if consuming within the `VS` repo](doc/vs_repo.md).

1. Install the NuGet package [Microsoft.VisualStudio.Sdk.TestFramework](https://nuget.org/packages/Microsoft.VisualStudio.Sdk.TestFramework),
   or for Xunit test projects, install the more specific [Microsoft.VisualStudio.Sdk.TestFramework.Xunit](https://nuget.org/packages/Microsoft.VisualStudio.Sdk.TestFramework.Xunit) package

1. Make sure your unit test project generates the required binding redirects by adding these two properties to your project file:

    ```xml
    <AutoGenerateBindingRedirects>true</AutoGenerateBindingRedirects>
    <GenerateBindingRedirectsOutputType>true</GenerateBindingRedirectsOutputType>
    ```

1. Apply some changes to your test project source as appropriate given the test framework you're already using:

    * [Xunit](doc/xunit.md)
    * [MSTest](doc/mstest.md)

### Main Thread considerations

This library will create a mocked up UI thread, such that `ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync()`
can switch to it. Your unit tests do *not* start on this mocked up UI thread. If your product code contains checks
that it is invoked on the UI thread (e.g. `ThreadHelper.ThrowIfNotOnUIThread()`) your test method should look like this:

```cs
[TestMethod] // or [Fact]
public async Task VerifyWeDoSomethingGood()
{
    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
    MyVSPackage.DoSomethingAwesome();
}
```

### Built in service mocks

There are a collection of "base services" that the VSSDKTestFx comes with mocks for.
Calling `GlobalServiceProvider.AddService` for any of these will result in an `InvalidOperationException` being thrown claiming
that the service is already added.

These services include:

* `SVsActivityLog`
* `OLE.Interop.IServiceProvider`
* `SVsTaskSchedulerService`
* `SVsUIThreadInvokerPrivate`

More may be added and can be found in [source code](https://github.com/microsoft/vssdktestfx/blob/main/src/Microsoft.VisualStudio.Sdk.TestFramework/GlobalServiceProvider.cs#L217-L224).

## Contributing

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).

See our [contributing doc](CONTRIBUTING.md) for more info.
