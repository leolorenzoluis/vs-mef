﻿namespace Microsoft.VisualStudio.Composition.Tests
{
    using System;
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Composition.AppDomainTests;
    using Microsoft.VisualStudio.Composition.AppDomainTests2;
    using Composition.Reflection;
    using Xunit;

    [Trait("Efficiency", "LazyLoad")]
    public abstract class AssembliesLazyLoadedTests : IDisposable
    {
        private ICompositionCacheManager cacheManager;

        private TempFileCollection tfc;

        protected AssembliesLazyLoadedTests(ICompositionCacheManager cacheManager)
        {
            Requires.NotNull(cacheManager, nameof(cacheManager));

            this.cacheManager = cacheManager;
            this.tfc = new TempFileCollection();
        }

        public void Dispose()
        {
            this.tfc.Delete();
        }

        /// <summary>
        /// Verifies that the assemblies that MEF parts belong to are only loaded when their parts are actually instantiated.
        /// </summary>
        [Fact]
        public async Task ComposableAssembliesLazyLoadedWhenQueried()
        {
            var catalog = TestUtilities.EmptyCatalog.AddParts(
                await TestUtilities.V2Discovery.CreatePartsAsync(typeof(ExternalExport), typeof(YetAnotherExport)));
            var catalogCache = await this.SaveCatalogAsync(catalog);
            var configuration = CompositionConfiguration.Create(catalog);
            var compositionCache = await this.SaveConfigurationAsync(configuration);

            // Use a sub-appdomain so we can monitor which assemblies get loaded by our composition engine.
            var appDomain = AppDomain.CreateDomain("Composition Test sub-domain", null, AppDomain.CurrentDomain.SetupInformation);
            try
            {
                var driver = (AppDomainTestDriver)appDomain.CreateInstanceAndUnwrap(typeof(AppDomainTestDriver).Assembly.FullName, typeof(AppDomainTestDriver).FullName);
                driver.Initialize(this.cacheManager.GetType(), compositionCache, catalogCache);
                driver.TestExternalExport(typeof(ExternalExport).Assembly.Location);
                driver.TestYetAnotherExport(typeof(YetAnotherExport).Assembly.Location);
            }
            finally
            {
                AppDomain.Unload(appDomain);
            }
        }

        [Fact(Skip = "We have to resolve the TypeRefs to traverse the hierarchy of types currently")]
        public async Task CatalogGetInputAssembliesDoesNotLoadLazyExports()
        {
            var catalog = TestUtilities.EmptyCatalog.AddParts(
                await TestUtilities.V2Discovery.CreatePartsAsync(typeof(ExternalExportWithExternalMetadataType), typeof(ExternalExportWithExternalMetadataTypeArray), typeof(ExternalExportWithExternalMetadataEnum32)));
            var catalogCache = await this.SaveCatalogAsync(catalog);
            var configuration = CompositionConfiguration.Create(catalog);
            var compositionCache = await this.SaveConfigurationAsync(configuration);

            var appDomain = AppDomain.CreateDomain("Composition Test sub-domain", null, AppDomain.CurrentDomain.SetupInformation);
            try
            {
                var driver = (AppDomainTestDriver)appDomain.CreateInstanceAndUnwrap(typeof(AppDomainTestDriver).Assembly.FullName, typeof(AppDomainTestDriver).FullName);
                driver.Initialize(this.cacheManager.GetType(), compositionCache, catalogCache);

                // GetInputAssemblies should not load the YetAnotherExport assembly or the CustomEnum assembly (both in AppDomainTests2)
                driver.TestGetInputAssembliesDoesNotLoadLazyExport(typeof(YetAnotherExport).Assembly.Location);
            }
            finally
            {
                AppDomain.Unload(appDomain);
            }
        }

        /// <summary>
        /// Verifies that the assemblies that MEF parts belong to are only loaded when their parts are actually instantiated.
        /// </summary>
        [Fact]
        public async Task ComposableAssembliesLazyLoadedByLazyImport()
        {
            var catalog = TestUtilities.EmptyCatalog.AddParts(
                await TestUtilities.V2Discovery.CreatePartsAsync(typeof(ExternalExportWithLazy), typeof(YetAnotherExport)));
            var catalogCache = await this.SaveCatalogAsync(catalog);
            var configuration = CompositionConfiguration.Create(catalog);
            var compositionCache = await this.SaveConfigurationAsync(configuration);

            // Use a sub-appdomain so we can monitor which assemblies get loaded by our composition engine.
            var appDomain = AppDomain.CreateDomain("Composition Test sub-domain", null, AppDomain.CurrentDomain.SetupInformation);
            try
            {
                var driver = (AppDomainTestDriver)appDomain.CreateInstanceAndUnwrap(typeof(AppDomainTestDriver).Assembly.FullName, typeof(AppDomainTestDriver).FullName);
                driver.Initialize(this.cacheManager.GetType(), compositionCache, catalogCache);
                driver.TestExternalExportWithLazy(typeof(YetAnotherExport).Assembly.Location);
            }
            finally
            {
                AppDomain.Unload(appDomain);
            }
        }

        /// <summary>
        /// Verifies that the assemblies that MEF parts belong to are only loaded when
        /// their metadata is actually retrieved.
        /// </summary>
        [Fact]
        public async Task ComposableAssembliesLazyLoadedByLazyMetadataDictionary()
        {
            var catalog = TestUtilities.EmptyCatalog.AddParts(
                await TestUtilities.V2Discovery.CreatePartsAsync(typeof(PartThatLazyImportsExportWithTypeMetadataViaDictionary), typeof(AnExportWithMetadataTypeValue)));
            var catalogCache = await this.SaveCatalogAsync(catalog);
            var configuration = CompositionConfiguration.Create(catalog);
            var compositionCache = await this.SaveConfigurationAsync(configuration);

            // Use a sub-appdomain so we can monitor which assemblies get loaded by our composition engine.
            var appDomain = AppDomain.CreateDomain("Composition Test sub-domain", null, AppDomain.CurrentDomain.SetupInformation);
            try
            {
                var driver = (AppDomainTestDriver)appDomain.CreateInstanceAndUnwrap(typeof(AppDomainTestDriver).Assembly.FullName, typeof(AppDomainTestDriver).FullName);
                driver.Initialize(this.cacheManager.GetType(), compositionCache, catalogCache);
                driver.TestPartThatImportsExportWithTypeMetadataViaDictionary(typeof(YetAnotherExport).Assembly.Location);
            }
            finally
            {
                AppDomain.Unload(appDomain);
            }
        }

        /// <summary>
        /// Verifies that the assemblies that MEF parts belong to are only loaded when
        /// their metadata is actually retrieved.
        /// </summary>
        [Fact]
        public async Task ComposableAssembliesLazyLoadedByLazyTMetadata()
        {
            var catalog = TestUtilities.EmptyCatalog.AddParts(
                await TestUtilities.V2Discovery.CreatePartsAsync(typeof(PartThatLazyImportsExportWithTypeMetadataViaTMetadata), typeof(AnExportWithMetadataTypeValue)))
                .WithDesktopSupport();
            var catalogCache = await this.SaveCatalogAsync(catalog);
            var configuration = CompositionConfiguration.Create(catalog);
            var compositionCache = await this.SaveConfigurationAsync(configuration);

            // Use a sub-appdomain so we can monitor which assemblies get loaded by our composition engine.
            var appDomain = AppDomain.CreateDomain("Composition Test sub-domain", null, AppDomain.CurrentDomain.SetupInformation);
            try
            {
                var driver = (AppDomainTestDriver)appDomain.CreateInstanceAndUnwrap(typeof(AppDomainTestDriver).Assembly.FullName, typeof(AppDomainTestDriver).FullName);
                driver.Initialize(this.cacheManager.GetType(), compositionCache, catalogCache);
                driver.TestPartThatImportsExportWithTypeMetadataViaTMetadata(typeof(YetAnotherExport).Assembly.Location);
            }
            finally
            {
                AppDomain.Unload(appDomain);
            }
        }

        /// <summary>
        /// Verifies that lazy assembly load isn't defeated when that assembly
        /// defines a type used as a generic type argument elsewhere.
        /// </summary>
        [Fact]
        public async Task ComposableAssembliesLazyLoadedWithGenericTypeArg()
        {
            var catalog = TestUtilities.EmptyCatalog.AddParts(
                await TestUtilities.V2Discovery.CreatePartsAsync(typeof(PartImportingOpenGenericExport), typeof(OpenGenericExport<>)))
                .WithDesktopSupport();
            var catalogCache = await this.SaveCatalogAsync(catalog);
            var configuration = CompositionConfiguration.Create(catalog);
            var compositionCache = await this.SaveConfigurationAsync(configuration);

            // Use a sub-appdomain so we can monitor which assemblies get loaded by our composition engine.
            var appDomain = AppDomain.CreateDomain("Composition Test sub-domain", null, AppDomain.CurrentDomain.SetupInformation);
            try
            {
                var driver = (AppDomainTestDriver)appDomain.CreateInstanceAndUnwrap(typeof(AppDomainTestDriver).Assembly.FullName, typeof(AppDomainTestDriver).FullName);
                driver.Initialize(this.cacheManager.GetType(), compositionCache, catalogCache);
                driver.TestPartThatImportsExportWithGenericTypeArg(typeof(SomeOtherType).Assembly.Location);
            }
            finally
            {
                AppDomain.Unload(appDomain);
            }
        }

        /// <summary>
        /// Verifies that the assemblies that MEF parts belong to are only loaded when the custom metadata types they define
        /// are actually required by some import.
        /// </summary>
        [Fact]
        public async Task ComposableAssembliesLazyLoadedWhenCustomMetadataIsRequired()
        {
            var catalog = TestUtilities.EmptyCatalog.AddParts(
                await TestUtilities.V2Discovery.CreatePartsAsync(typeof(ExportWithCustomMetadata), typeof(PartThatLazyImportsExportWithMetadataOfCustomType)));
            var catalogCache = await this.SaveCatalogAsync(catalog);
            var configuration = CompositionConfiguration.Create(catalog);
            var compositionCache = await this.SaveConfigurationAsync(configuration);

            // Use a sub-appdomain so we can monitor which assemblies get loaded by our composition engine.
            var appDomain = AppDomain.CreateDomain("Composition Test sub-domain", null, AppDomain.CurrentDomain.SetupInformation);
            try
            {
                var driver = (AppDomainTestDriver)appDomain.CreateInstanceAndUnwrap(typeof(AppDomainTestDriver).Assembly.FullName, typeof(AppDomainTestDriver).FullName);
                driver.Initialize(this.cacheManager.GetType(), compositionCache, catalogCache);
                driver.TestPartThatLazyImportsExportWithMetadataOfCustomType(typeof(CustomEnum).Assembly.Location, this is AssembliesLazyLoadedDataFileCacheTests);
            }
            finally
            {
                AppDomain.Unload(appDomain);
            }
        }

        private async Task<Stream> SaveConfigurationAsync(CompositionConfiguration configuration)
        {
            Requires.NotNull(configuration, nameof(configuration));

            var ms = new MemoryStream();
            await this.cacheManager.SaveAsync(configuration, ms);
            ms.Position = 0;
            return ms;
        }

        private async Task<Stream> SaveCatalogAsync(ComposableCatalog catalog)
        {
            Requires.NotNull(catalog, nameof(catalog));

            var ms = new MemoryStream();
            await new CachedCatalog().SaveAsync(catalog, ms);
            ms.Position = 0;
            return ms;
        }

        private class AppDomainTestDriver : MarshalByRefObject
        {
            private ExportProvider container;
            private ComposableCatalog catalog;

            internal void Initialize(Type cacheManagerType, Stream cachedComposition, Stream cachedCatalog)
            {
                Requires.NotNull(cacheManagerType, nameof(cacheManagerType));
                Requires.NotNull(cachedComposition, nameof(cachedComposition));
                Requires.NotNull(cachedCatalog, nameof(cachedCatalog));

                // Copy the streams to ones inside our app domain.
                Stream cachedCompositionLocal = CopyStream(cachedComposition);
                Stream cachedCatalogLocal = CopyStream(cachedCatalog);

                // Deserialize the catalog to verify that it doesn't load any assemblies.
                var catalogManager = new CachedCatalog();
                this.catalog = catalogManager.LoadAsync(cachedCatalogLocal, TestUtilities.Resolver).Result;

                // Deserialize the composition to prepare for the rest of the test.
                var cacheManager = (ICompositionCacheManager)Activator.CreateInstance(cacheManagerType);
                var containerFactory = cacheManager.LoadExportProviderFactoryAsync(cachedCompositionLocal, TestUtilities.Resolver).GetAwaiter().GetResult();
                this.container = containerFactory.CreateExportProvider();
            }

            internal void TestGetInputAssembliesDoesNotLoadLazyExport(string lazyLoadedAssemblyPath)
            {
                Assert.False(GetLoadedAssemblies().Any(a => a.Location.Equals(lazyLoadedAssemblyPath, StringComparison.OrdinalIgnoreCase)));
                this.catalog.GetInputAssemblies();
                Assert.False(GetLoadedAssemblies().Any(a => a.Location.Equals(lazyLoadedAssemblyPath, StringComparison.OrdinalIgnoreCase)));
            }

            internal void TestExternalExport(string lazyLoadedAssemblyPath)
            {
                Assert.False(GetLoadedAssemblies().Any(a => a.Location.Equals(lazyLoadedAssemblyPath, StringComparison.OrdinalIgnoreCase)));
                this.CauseLazyLoad1(this.container);
                Assert.True(GetLoadedAssemblies().Any(a => a.Location.Equals(lazyLoadedAssemblyPath, StringComparison.OrdinalIgnoreCase)));
            }

            internal void TestYetAnotherExport(string lazyLoadedAssemblyPath)
            {
                Assert.False(GetLoadedAssemblies().Any(a => a.Location.Equals(lazyLoadedAssemblyPath, StringComparison.OrdinalIgnoreCase)));
                this.CauseLazyLoad2(this.container);
                Assert.True(GetLoadedAssemblies().Any(a => a.Location.Equals(lazyLoadedAssemblyPath, StringComparison.OrdinalIgnoreCase)));
            }

            internal void TestExternalExportWithLazy(string lazyLoadedAssemblyPath)
            {
                Assert.False(GetLoadedAssemblies().Any(a => a.Location.Equals(lazyLoadedAssemblyPath, StringComparison.OrdinalIgnoreCase)));
                var exportWithLazy = this.container.GetExportedValue<ExternalExportWithLazy>();
                Assert.False(GetLoadedAssemblies().Any(a => a.Location.Equals(lazyLoadedAssemblyPath, StringComparison.OrdinalIgnoreCase)));
                Assert.NotNull(exportWithLazy.YetAnotherExport.Value);
                Assert.True(GetLoadedAssemblies().Any(a => a.Location.Equals(lazyLoadedAssemblyPath, StringComparison.OrdinalIgnoreCase)));
            }

            internal void TestPartThatImportsExportWithTypeMetadataViaDictionary(string lazyLoadedAssemblyPath)
            {
                Assert.False(GetLoadedAssemblies().Any(a => a.Location.Equals(lazyLoadedAssemblyPath, StringComparison.OrdinalIgnoreCase)));
                var exportWithLazy = this.container.GetExportedValue<PartThatLazyImportsExportWithTypeMetadataViaDictionary>();
                Assert.False(GetLoadedAssemblies().Any(a => a.Location.Equals(lazyLoadedAssemblyPath, StringComparison.OrdinalIgnoreCase)));
                Assert.NotNull(exportWithLazy.ImportWithDictionary.Metadata.ContainsKey("foo"));
                Assert.False(GetLoadedAssemblies().Any(a => a.Location.Equals(lazyLoadedAssemblyPath, StringComparison.OrdinalIgnoreCase)));
                Type type = (Type)exportWithLazy.ImportWithDictionary.Metadata["SomeType"];
                Type[] types = (Type[])exportWithLazy.ImportWithDictionary.Metadata["SomeTypes"];
                Assert.Equal("YetAnotherExport", type.Name);
                types.Single(t => t.Name == "String");
                types.Single(t => t.Name == "YetAnotherExport");
                Assert.True(GetLoadedAssemblies().Any(a => a.Location.Equals(lazyLoadedAssemblyPath, StringComparison.OrdinalIgnoreCase)));
            }

            internal void TestPartThatImportsExportWithTypeMetadataViaTMetadata(string lazyLoadedAssemblyPath)
            {
                Assert.False(GetLoadedAssemblies().Any(a => a.Location.Equals(lazyLoadedAssemblyPath, StringComparison.OrdinalIgnoreCase)));
                var exportWithLazy = this.container.GetExportedValue<PartThatLazyImportsExportWithTypeMetadataViaTMetadata>();
                Assert.False(GetLoadedAssemblies().Any(a => a.Location.Equals(lazyLoadedAssemblyPath, StringComparison.OrdinalIgnoreCase)));
                Assert.Equal("default", exportWithLazy.ImportWithTMetadata.Metadata.SomeProperty);
                Assert.False(GetLoadedAssemblies().Any(a => a.Location.Equals(lazyLoadedAssemblyPath, StringComparison.OrdinalIgnoreCase)));
                Type type = exportWithLazy.ImportWithTMetadata.Metadata.SomeType;
                Type[] types = exportWithLazy.ImportWithTMetadata.Metadata.SomeTypes;
                Assert.Equal("YetAnotherExport", type.Name);
                types.Single(t => t.Name == "String");
                types.Single(t => t.Name == "YetAnotherExport");
                Assert.True(GetLoadedAssemblies().Any(a => a.Location.Equals(lazyLoadedAssemblyPath, StringComparison.OrdinalIgnoreCase)));
            }

            internal void TestPartThatImportsExportWithGenericTypeArg(string lazyLoadedAssemblyPath)
            {
                Assert.False(GetLoadedAssemblies().Any(a => a.Location.Equals(lazyLoadedAssemblyPath, StringComparison.OrdinalIgnoreCase)));
                var exportWithLazy = this.container.GetExportedValue<PartImportingOpenGenericExport>();
                Assert.True(GetLoadedAssemblies().Any(a => a.Location.Equals(lazyLoadedAssemblyPath, StringComparison.OrdinalIgnoreCase)));
            }

            internal void TestPartThatLazyImportsExportWithMetadataOfCustomType(string lazyLoadedAssemblyPath, bool isRuntime)
            {
                Assert.False(GetLoadedAssemblies().Any(a => a.Location.Equals(lazyLoadedAssemblyPath, StringComparison.OrdinalIgnoreCase)));
                var exportWithLazy = this.container.GetExportedValue<PartThatLazyImportsExportWithMetadataOfCustomType>();

                // This next segment we'll permit an assembly load only for code gen cases, which aren't as well tuned at present.
                Assert.False(isRuntime && GetLoadedAssemblies().Any(a => a.Location.Equals(lazyLoadedAssemblyPath, StringComparison.OrdinalIgnoreCase)));
                Assert.Equal("Value", exportWithLazy.ImportingProperty.Metadata["Simple"]);
                Assert.False(isRuntime && GetLoadedAssemblies().Any(a => a.Location.Equals(lazyLoadedAssemblyPath, StringComparison.OrdinalIgnoreCase)));

                // At this point, loading the assembly is absolutely required.
                object customEnum = exportWithLazy.ImportingProperty.Metadata["CustomValue"];
                Assert.Equal("CustomEnum", customEnum.GetType().Name);
                Assert.True(GetLoadedAssemblies().Any(a => a.Location.Equals(lazyLoadedAssemblyPath, StringComparison.OrdinalIgnoreCase)));
            }

            private static IEnumerable<Assembly> GetLoadedAssemblies()
            {
                return AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic);
            }

            private static Stream CopyStream(Stream source)
            {
                Stream copy = new MemoryStream();
                source.CopyTo(copy);
                copy.Position = 0;
                return copy;
            }

            [MethodImpl(MethodImplOptions.NoInlining)] // if this method is inlined, it defeats the point of it being a separate method in the test and causes test failure.
            private void CauseLazyLoad1(ExportProvider container)
            {
                // Actually the lazy load happens before GetExport is actually called since this method
                // references a type in that assembly.
                var export = container.GetExportedValue<ExternalExport>();
                Assert.NotNull(export);
            }

            [MethodImpl(MethodImplOptions.NoInlining)] // if this method is inlined, it defeats the point of it being a separate method in the test and causes test failure.
            private void CauseLazyLoad2(ExportProvider container)
            {
                // Actually the lazy load happens before GetExport is actually called since this method
                // references a type in that assembly.
                var export = container.GetExportedValue<YetAnotherExport>();
                Assert.NotNull(export);
            }
        }
    }
}
