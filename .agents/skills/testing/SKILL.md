---
name: testing
description: Provides guidance and instructions for writing, running and filtering tests.
user-invocable: false
---

**IMPORTANT**: This repository has xUnit v2 tests using VSTest and xUnit v3 tests using Microsoft.Testing.Platform (MTP v2). Their filtering options are different.

* There should generally be one test project (under the `test` directory) per shipping project (under the `src` directory). Test projects are named after the project being tested with a `.Tests` suffix.
* Run each test project separately when passing runner-specific options. VSTest filters do not filter the xUnit v3 MTP project.

## Running Tests

There *may* be unstable tests that are attributed as such.
Running the [dotnet-test-cloud.ps1](../../../tools/dotnet-test-cloud.ps1) script will execute all previously built tests except the unstable ones.
The commands below do not automatically exclude unstable tests. Use the appropriate runner-specific exclusion shown below.

**Run all tests** (from the root of the repository):
```bash
dotnet test --no-build -c Release
```

To run a specific test project, change to that project's directory first.

**Run tests for a specific test project** (within that test project's directory):
```bash
dotnet test --no-build -c Release
```

### xUnit v2 (VSTest)

Run tests in a class or exclude the unstable trait:
```bash
dotnet test --no-build -c Release --filter "FullyQualifiedName~ClassName"
dotnet test --no-build -c Release --filter "TestCategory!=FailsInCloudTest"
```

### xUnit v3 (MTP)

Pass runner arguments after `--`:
```bash
dotnet test --no-build -c Release -- --filter-method "*ClassName.MethodName"
dotnet test --no-build -c Release -- --filter-class "*ClassName"
dotnet test --no-build -c Release -- --filter-not-trait "TestCategory=FailsInCloudTest"
```

Use `--framework` to select a target framework. Wildcards are supported at the beginning and/or end of xUnit v3 filter values.
