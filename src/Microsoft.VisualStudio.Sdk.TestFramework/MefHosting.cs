/********************************************************
*                                                        *
*   © Copyright (C) Microsoft. All rights reserved.      *
*                                                        *
*********************************************************/

namespace Microsoft.VisualStudio.Sdk.TestFramework
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Composition;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Threading;

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
        /// The names of the assemblies to include in the catalog.
        /// </summary>
        private readonly ImmutableArray<string> catalogAssemblyNames;

        /// <summary>
        /// The lazily-created export provider factory.
        /// </summary>
        private AsyncLazy<IExportProviderFactory> exportProviderFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="MefHosting"/> class
        /// where all assemblies in the test project's output directory are
        /// included in the MEF catalog.
        /// </summary>
        public MefHosting()
            : this(GetDefaultAssemblyNames())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MefHosting"/> class.
        /// </summary>
        /// <param name="assemblyNames">The names of the assemblies to include in the MEF catalog.</param>
        public MefHosting(IEnumerable<string> assemblyNames)
        {
            Requires.NotNull(assemblyNames, nameof(assemblyNames));

            this.catalogAssemblyNames = ImmutableArray.CreateRange(assemblyNames);
            this.exportProviderFactory = new AsyncLazy<IExportProviderFactory>(
                this.CreateExportProviderFactoryAsync,
                ThreadHelper.JoinableTaskFactory);
        }

        /// <summary>
        /// Creates a new MEF container, initialized with all the assemblies
        /// specified in the constructor.
        /// </summary>
        /// <returns>A task whose result is the <see cref="ExportProvider"/>.</returns>
        public async Task<ExportProvider> CreateExportProviderAsync()
        {
            IExportProviderFactory exportProviderFactory = await this.exportProviderFactory.GetValueAsync();
            return exportProviderFactory.CreateExportProvider();
        }

        /// <summary>
        /// Gets a reasonable guess at which assemblies to include in the MEF catalog.
        /// </summary>
        /// <returns>The list of assembly names.</returns>
        private static IEnumerable<string> GetDefaultAssemblyNames()
        {
            foreach (string file in Directory.EnumerateFiles(Environment.CurrentDirectory, "*.dll"))
            {
                if (file.EndsWith(".resources.dll", StringComparison.OrdinalIgnoreCase))
                {
                    // Skip satellite assemblies if we come across any.
                    continue;
                }

                string assemblyFullName = null;
                try
                {
                    var assemblyName = AssemblyName.GetAssemblyName(file);
                    if (assemblyName != null)
                    {
                        assemblyFullName = assemblyName.FullName;
                    }
                }
                catch (Exception)
                {
                }

                if (assemblyFullName != null)
                {
                    yield return assemblyFullName;
                }
            }
        }

        /// <summary>
        /// Creates a factory for <see cref="ExportProvider"/> containers with a
        /// backing catalog that contains all the parts being tested.
        /// </summary>
        /// <returns>A task whose result is the <see cref="IExportProviderFactory"/>.</returns>
        private async Task<IExportProviderFactory> CreateExportProviderFactoryAsync()
        {
            ComposableCatalog catalog = await this.CreateProductCatalogAsync();
            var configuration = CompositionConfiguration.Create(catalog);
            IExportProviderFactory exportProviderFactory = configuration.CreateExportProviderFactory();
            return exportProviderFactory;
        }

        /// <summary>
        /// Creates a catalog containing the MEF parts we want to test.
        /// </summary>
        /// <returns>A task whose result is the <see cref="ComposableCatalog"/>.</returns>
        private async Task<ComposableCatalog> CreateProductCatalogAsync()
        {
            IEnumerable<Assembly> assemblies = this.catalogAssemblyNames.Select(Assembly.Load);
            DiscoveredParts discoveredParts = await this.discoverer.CreatePartsAsync(assemblies);
            ComposableCatalog catalog = ComposableCatalog.Create(Resolver.DefaultInstance)
                .AddParts(discoveredParts)
                .WithCompositionService();
            return catalog;
        }
    }
}
