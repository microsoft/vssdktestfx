# Microsoft.VisualStudio.Sdk.TestFramework.Xunit

This package contains functionality applicable when using Xunit as your test framework.

For *each* of your test classes that rely on VS mocked services, apply the `Collection` attribute and add a parameter and statement to your constructor:

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
