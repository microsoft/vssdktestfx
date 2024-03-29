# Visual Studio SDK Test Framework

[![Build Status](https://dev.azure.com/azure-public/vside/_apis/build/status/vssdktestfx?branchName=main)](https://dev.azure.com/azure-public/vside/_build/latest?definitionId=45&branchName=main)
[![Join the chat at https://gitter.im/Microsoft/extendvs](https://badges.gitter.im/extendvs/Lobby.svg)](https://gitter.im/Microsoft/extendvs)

## Microsoft.VisualStudio.Sdk.TestFramework

[![NuGet package](https://img.shields.io/nuget/v/Microsoft.VisualStudio.Sdk.TestFramework.svg)](https://nuget.org/packages/Microsoft.VisualStudio.Sdk.TestFramework)

The VS SDK Test Framework is a library for your unit tests that exercise VS code to use
so that certain core VS functionality works outside the VS process so your unit tests can function.
For example, `ThreadHelper` and obtaining global services from the static `ServiceProvider`
tend to fail in unit tests without this library installed.

[Learn more about this package](src/Microsoft.VisualStudio.Sdk.TestFramework/README.md).

## Microsoft.VisualStudio.Sdk.TestFramework.Xunit

[![NuGet package](https://img.shields.io/nuget/v/Microsoft.VisualStudio.Sdk.TestFramework.Xunit.svg)](https://nuget.org/packages/Microsoft.VisualStudio.Sdk.TestFramework.Xunit)

This package contains functionality applicable when using Xunit as your test framework.

[Learn more about this package](src/Microsoft.VisualStudio.Sdk.TestFramework.Xunit/README.md).

## Contributing

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).

See our [contributing doc](CONTRIBUTING.md) for more info.
