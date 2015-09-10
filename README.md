# Visual Studio SDK Test Framework

## Xunit instructions

Add this class to your project (if a MockedVS.cs file was not added to your project automatically):

    using Xunit;

    /// <summary>
    /// Defines the "MockedVS" xunit test collection.
    /// </summary>
    [CollectionDefinition(Collection)]
    public class MockedVS : ICollectionFixture<GlobalServiceProvider>, ICollectionFixture<MefHosting>
    {
        /// <summary>
        /// The name of the xunit test collection.
        /// </summary>
        public const string Collection = "MockedVS";
    }

Then for *each of your test classes, apply the `Collection` attribute and
add a parameter and statement to your constructor:

    [Collection(MockedVS.Collection)]
    public class YourTestClass
    {
        public TestFrameworkTests(GlobalServiceProvider sp)
        {
            sp.Reset();
        }
    }

## MSTest instructions

Add these members to *one* of your test classes:

    internal static GlobalServiceProvider mockServiceProvider;

    [AssemblyInitialize]
    public static void AssemblyInit(TestContext context)
    {
        mockServiceProvider = new GlobalServiceProvider();
    }

Then in *each* of your test classes, Reset() the static service provider created earlier:

    [TestInitialize]
    public void TestInit()
    {
        SharedClass.mockServiceProvider.Reset();
    }
