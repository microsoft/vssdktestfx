---
name: testing
description: Provides guidance and instructions for writing, running and filtering tests.
user-invocable: false
---

The two test projects share their test sources but use different runners:

* `test\Microsoft.VisualStudio.Sdk.TestFramework.Xunit.Tests` uses xUnit v2 and VSTest.
* `test\Microsoft.VisualStudio.Sdk.TestFramework.Xunit.v3.Tests` uses xUnit v3 and Microsoft.Testing.Platform (MTP v2).

Both projects target `net472`, `net8.0`, and `net8.0-windows8.0`.

## Running Tests

Build first, then run [dotnet-test-cloud.ps1](../../../tools/dotnet-test-cloud.ps1) to exclude tests marked `FailsInCloudTest`:

```powershell
dotnet build -c Release
.\tools\dotnet-test-cloud.ps1 -Configuration Release
```

To run all tests, including any unstable tests:

```powershell
dotnet test --no-build -c Release
```

For a specific project or target framework, pass its project file and `--framework net8.0` to `dotnet test`.
The xUnit v2 project accepts VSTest filters, for example:

```powershell
dotnet test test\Microsoft.VisualStudio.Sdk.TestFramework.Xunit.Tests\Microsoft.VisualStudio.Sdk.TestFramework.Xunit.Tests.csproj --no-build -c Release --framework net8.0 --filter "FullyQualifiedName~ClassName.MethodName"
```

For xUnit v3, pass xUnit's MTP options to its executable rather than TUnit's `--treenode-filter`:

```powershell
dotnet run --project test\Microsoft.VisualStudio.Sdk.TestFramework.Xunit.v3.Tests\Microsoft.VisualStudio.Sdk.TestFramework.Xunit.v3.Tests.csproj --no-build -c Release --framework net8.0 -- --filter-method "*ClassName.MethodName"
```
