namespace Microsoft.VisualStudio.Sdk.TestFramework
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using Composition;

    /// <summary>
    /// Provides VS MEF hosting facilities for unit tests.
    /// </summary>
    public class MefHosting
    {
        /// <summary>
        /// The MEF discovery module to use (which finds both MEFv1 and MEFv2 parts).
        /// </summary>
        private readonly PartDiscovery discoverer = PartDiscovery.Combine(
            new AttributedPartDiscovery(Resolver.DefaultInstance, isNonPublicSupported: true),
            new AttributedPartDiscoveryV1(Resolver.DefaultInstance));

        /// <summary>
        /// Creates a factory for <see cref="ExportProvider"/> containers with a
        /// backing catalog that contains all the parts being tested.
        /// </summary>
        /// <returns>A task whose result is the <see cref="IExportProviderFactory"/>.</returns>
        public async Task<IExportProviderFactory> CreateExportProviderFactoryAsync(IEnumerable<string> assemblyNames)
        {
            ComposableCatalog catalog = await this.CreateProductCatalogAsync(assemblyNames);
            var configuration = CompositionConfiguration.Create(catalog);
            var exportProviderFactory = configuration.CreateExportProviderFactory();
            return exportProviderFactory;
        }

        /// <summary>
        /// Creates a catalog containing the MEF parts we want to test.
        /// </summary>
        /// <returns>A task whose result is the <see cref="ComposableCatalog"/>.</returns>
        private async Task<ComposableCatalog> CreateProductCatalogAsync(IEnumerable<string> assemblyNames)
        {
            var assemblies = assemblyNames.Select(Assembly.Load);
            var discoveredParts = await this.discoverer.CreatePartsAsync(assemblies);
            var catalog = ComposableCatalog.Create(Resolver.DefaultInstance)
                .AddParts(discoveredParts)
                .WithCompositionService()
                .WithDesktopSupport();
            return catalog;
        }
    }
}
