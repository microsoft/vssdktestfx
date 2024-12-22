# Xunit instructions

These instructions apply when consuming this test framework from an Xunit project.
This is an addendum to general instructions outlined in [our README](../README.md).

1. Install the NuGet package.
    * When using xUnit v2, install `Microsoft.VisualStudio.Sdk.TestFramework.Xunit` [![NuGet package](https://img.shields.io/nuget/v/Microsoft.VisualStudio.Sdk.TestFramework.Xunit.svg)](https://nuget.org/packages/Microsoft.VisualStudio.Sdk.TestFramework.Xunit)
    * When using xUnit v3, install `Microsoft.VisualStudio.Sdk.TestFramework.Xunit.V3` [![NuGet package](https://img.shields.io/nuget/v/Microsoft.VisualStudio.Sdk.TestFramework.Xunit.V3.svg)](https://nuget.org/packages/Microsoft.VisualStudio.Sdk.TestFramework.Xunit.V3)
1. For *each* of your test classes that rely on VS mocked services, apply the `Collection` attribute and add a parameter and statement to your constructor:

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
