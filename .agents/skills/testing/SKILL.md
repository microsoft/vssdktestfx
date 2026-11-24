---
name: testing
description: Provides guidance and instructions for writing, running and filtering tests.
user-invocable: false
---

The two test projects share their test sources but use different runners:

* `test\Microsoft.VisualStudio.Sdk.TestFramework.Xunit.Tests` uses xUnit v2 and VSTest.
* `test\Microsoft.VisualStudio.Sdk.TestFramework.Xunit.v3.Tests` uses xUnit v3 and Microsoft.Testing.Platform (MTP v2).

Both projects target `net472`, `net10.0`, and `net10.0-windows8.0`.
Run each project separately when passing runner-specific options: VSTest filters do not filter the xUnit v3 MTP project.

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

### xUnit v2 (VSTest)

Run a test or exclude the unstable trait:

```powershell
dotnet test test\Microsoft.VisualStudio.Sdk.TestFramework.Xunit.Tests\Microsoft.VisualStudio.Sdk.TestFramework.Xunit.Tests.csproj --no-build -c Release --framework net10.0 --filter "FullyQualifiedName~ClassName.MethodName"
dotnet test test\Microsoft.VisualStudio.Sdk.TestFramework.Xunit.Tests\Microsoft.VisualStudio.Sdk.TestFramework.Xunit.Tests.csproj --no-build -c Release --framework net10.0 --filter "TestCategory!=FailsInCloudTest"
```

### xUnit v3 (MTP)

Pass xUnit's MTP options to its executable, after `--`:

```powershell
dotnet run --project test\Microsoft.VisualStudio.Sdk.TestFramework.Xunit.v3.Tests\Microsoft.VisualStudio.Sdk.TestFramework.Xunit.v3.Tests.csproj --no-build -c Release --framework net10.0 -- --filter-method "*ClassName.MethodName"
dotnet run --project test\Microsoft.VisualStudio.Sdk.TestFramework.Xunit.v3.Tests\Microsoft.VisualStudio.Sdk.TestFramework.Xunit.v3.Tests.csproj --no-build -c Release --framework net10.0 -- --filter-class "*ClassName"
dotnet run --project test\Microsoft.VisualStudio.Sdk.TestFramework.Xunit.v3.Tests\Microsoft.VisualStudio.Sdk.TestFramework.Xunit.v3.Tests.csproj --no-build -c Release --framework net10.0 -- --filter-not-trait "TestCategory=FailsInCloudTest"
```

Use `--framework` to select a target framework. Wildcards are supported at the beginning and/or end of xUnit v3 filter values. The commands above do not automatically exclude unstable tests.
