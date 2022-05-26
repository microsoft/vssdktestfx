// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Sdk.TestFramework;

using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.Composition;

/// <summary>
/// Provides VS MEF hosting facilities for unit tests.
/// </summary>
public class MefHosting
{
    /// <summary>
    /// The MEF discovery module to use (which finds both MEFv1 and MEFv2 parts).
    /// </summary>
    public static readonly PartDiscovery PartDiscoverer = PartDiscovery.Combine(
        new AttributedPartDiscovery(Resolver.DefaultInstance, isNonPublicSupported: true),
        new AttributedPartDiscoveryV1(Resolver.DefaultInstance));

    /// <summary>
    /// The names of the assemblies to include in the catalog.
    /// </summary>
    private readonly ImmutableArray<string> catalogAssemblyNames;

    /// <summary>
    /// The lazily-created catalog.
    /// </summary>
    private AsyncLazy<ComposableCatalog> catalog;

    /// <summary>
    /// The lazily-created configuration.
    /// </summary>
    private AsyncLazy<CompositionConfiguration> configuration;

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
        this.catalog = new AsyncLazy<ComposableCatalog>(this.CreateProductCatalogAsync, ThreadHelper.JoinableTaskFactory);
        this.configuration = new AsyncLazy<CompositionConfiguration>(this.CreateConfigurationAsync, ThreadHelper.JoinableTaskFactory);
        this.exportProviderFactory = new AsyncLazy<IExportProviderFactory>(
            this.CreateExportProviderFactoryAsync,
            ThreadHelper.JoinableTaskFactory);
    }

    /// <summary>
    /// Creates a one-off <see cref="ExportProvider"/> based on an explicit list of MEF parts.
    /// </summary>
    /// <param name="partTypes">The types that define the MEF parts to include in the backing catalog.</param>
    /// <returns>An <see cref="ExportProvider"/>.</returns>
    public static async Task<ExportProvider> CreateExportProviderAsync(params Type[] partTypes)
    {
        DiscoveredParts parts = await PartDiscoverer.CreatePartsAsync(partTypes);
        ComposableCatalog catalog = ComposableCatalog.Create(Resolver.DefaultInstance).AddParts(parts);
        CompositionConfiguration configuration = CompositionConfiguration.Create(catalog);
        IExportProviderFactory exportProviderFactory = configuration.CreateExportProviderFactory();
        return exportProviderFactory.CreateExportProvider();
    }

    /// <summary>
    /// Gets the <see cref="ComposableCatalog"/> that backs this MEF service.
    /// </summary>
    /// <returns>The <see cref="ComposableCatalog"/>.</returns>
    public Task<ComposableCatalog> GetCatalogAsync() => this.catalog.GetValueAsync();

    /// <summary>
    /// Gets the <see cref="CompositionConfiguration"/> that backs this MEF service.
    /// </summary>
    /// <returns>The <see cref="CompositionConfiguration"/>.</returns>
    public Task<CompositionConfiguration> GetConfigurationAsync() => this.configuration.GetValueAsync();

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

            string? assemblyFullName = null;
            try
            {
                var assemblyName = AssemblyName.GetAssemblyName(file);
                if (assemblyName != null)
                {
                    assemblyFullName = assemblyName.FullName;
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception)
#pragma warning restore CA1031 // Do not catch general exception types
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
        CompositionConfiguration configuration = await this.configuration.GetValueAsync();
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
        DiscoveredParts discoveredParts = await PartDiscoverer.CreatePartsAsync(assemblies);
        ComposableCatalog catalog = ComposableCatalog.Create(Resolver.DefaultInstance)
            .AddParts(discoveredParts)
            .WithCompositionService();
        return catalog;
    }

    private async Task<CompositionConfiguration> CreateConfigurationAsync()
    {
        ComposableCatalog catalog = await this.catalog.GetValueAsync();
        return CompositionConfiguration.Create(catalog);
    }
}
