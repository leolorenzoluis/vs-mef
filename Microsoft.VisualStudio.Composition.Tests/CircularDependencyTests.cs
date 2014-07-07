﻿namespace Microsoft.VisualStudio.Composition.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;
    using MefV1 = System.ComponentModel.Composition;

    public class CircularDependencyTests
    {
        #region Tight loop of all shared exports

        [MefFact(CompositionEngines.V2Compat, typeof(SharedExport1), typeof(SharedExport2))]
        public void CircularDependenciesSharedExports(IContainer container)
        {
            var export1 = container.GetExportedValue<SharedExport1>();
            var export2 = container.GetExportedValue<SharedExport2>();
            Assert.Same(export1.Export2, export2);
            Assert.Same(export2.Export1, export1);
        }

        [Export, Shared]
        public class SharedExport1
        {
            [Import]
            public SharedExport2 Export2 { get; set; }
        }

        [Export, Shared]
        public class SharedExport2
        {
            [Import]
            public SharedExport1 Export1 { get; set; }
        }

        #endregion

        #region Tight loop of all shared (internal) exports

        [MefFact(CompositionEngines.V1Compat, typeof(InternalSharedExport1), typeof(InternalSharedExport2))]
        public void CircularDependenciesInternalSharedExports(IContainer container)
        {
            var export1 = container.GetExportedValue<InternalSharedExport1>();
            var export2 = container.GetExportedValue<InternalSharedExport2>();
            Assert.Same(export1.Export2, export2);
            Assert.Same(export2.Export1, export1);
        }

        [MefV1.Export]
        internal class InternalSharedExport1
        {
            [MefV1.Import]
            public InternalSharedExport2 Export2 { get; set; }
        }

        [MefV1.Export]
        internal class InternalSharedExport2
        {
            [MefV1.Import]
            public InternalSharedExport1 Export1 { get; set; }
        }

        #endregion

        #region Tight loop of all shared lazy exports

        [MefFact(CompositionEngines.V2Compat, typeof(LazySharedExport1), typeof(LazySharedExport2))]
        public void CircularDependenciesLazySharedExports(IContainer container)
        {
            var export1 = container.GetExportedValue<LazySharedExport1>();
            var export2 = container.GetExportedValue<LazySharedExport2>();
            Assert.Same(export1.Export2.Value, export2);
            Assert.Same(export2.Export1.Value, export1);
        }

        [Export, Shared]
        public class LazySharedExport1
        {
            [Import]
            public Lazy<LazySharedExport2> Export2 { get; set; }
        }

        [Export, Shared]
        public class LazySharedExport2
        {
            [Import]
            public Lazy<LazySharedExport1> Export1 { get; set; }
        }

        #endregion

        #region Loop with just one shared export

        [MefFact(CompositionEngines.V2Compat, typeof(SharedExportInLoopOfNonShared), typeof(NonSharedExportInLoopWithShared), typeof(AnotherNonSharedExportInLoopWithShared))]
        public void CircularDependenciesOneSharedExport(IContainer container)
        {
            var shared = container.GetExportedValue<SharedExportInLoopOfNonShared>();
            var nonShared = container.GetExportedValue<NonSharedExportInLoopWithShared>();
            var anotherNonShared = container.GetExportedValue<AnotherNonSharedExportInLoopWithShared>();
            Assert.NotSame(nonShared, shared.Other);
            Assert.NotSame(anotherNonShared, nonShared.Another);
            Assert.NotSame(anotherNonShared, shared.Other.Another);
            Assert.Same(shared, anotherNonShared.Shared);
        }

        [Export, Shared]
        public class SharedExportInLoopOfNonShared
        {
            [Import]
            public NonSharedExportInLoopWithShared Other { get; set; }
        }

        [Export]
        public class NonSharedExportInLoopWithShared
        {
            [Import]
            public AnotherNonSharedExportInLoopWithShared Another { get; set; }
        }

        [Export]
        public class AnotherNonSharedExportInLoopWithShared
        {
            [Import]
            public SharedExportInLoopOfNonShared Shared { get; set; }
        }

        #endregion

        #region Large loop of all non-shared exports

        [Fact]
        public void LoopOfNonSharedExports()
        {
            // There is no way to resolve this catalog. It would instantiate parts forever.
            Assert.Throws<CompositionFailedException>(() => CompositionConfiguration.Create(
                new AttributedPartDiscovery(), 
                typeof(NonSharedPart1),
                typeof(NonSharedPart2),
                typeof(NonSharedPart3)));
        }

        [Export]
        public class NonSharedPart1
        {
            [Import]
            public NonSharedPart2 Part2 { get; set; }
        }

        [Export]
        public class NonSharedPart2
        {
            [Import]
            public NonSharedPart3 Part3 { get; set; }
        }

        [Export]
        public class NonSharedPart3
        {
            [Import]
            public NonSharedPart1 Part1 { get; set; }
        }

        #endregion

        #region Imports self

        [Fact]
        public void SelfImportingNonSharedPartThrows()
        {
            Assert.Throws<CompositionFailedException>(() => TestUtilities.CreateContainer(typeof(SelfImportingNonSharedPart)));
        }

        [Fact]
        public void SelfImportingNonSharedPartThrowsV1()
        {
            var container = TestUtilities.CreateContainerV1(typeof(SelfImportingNonSharedPart));
            Assert.Throws<MefV1.CompositionException>(() => container.GetExportedValue<SelfImportingNonSharedPart>());
        }

        [Fact]
        public void SelfImportingNonSharedPartThrowsV2()
        {
            var container = TestUtilities.CreateContainerV2(typeof(SelfImportingNonSharedPart));
            Assert.Throws<CompositionFailedException>(() => container.GetExportedValue<SelfImportingNonSharedPart>());
        }

        [MefFact(CompositionEngines.V2Compat | CompositionEngines.V1Compat, typeof(SelfImportingSharedPart))]
        public void SelfImportingSharedPartSucceeds(IContainer container)
        {
            var value = container.GetExportedValue<SelfImportingSharedPart>();
            Assert.Same(value, value.Self);
            Assert.Same(value, value.LazySelf.Value);
            Assert.Same(value, value.LazySelfMetadata.Value);
            Assert.Equal(1, value.SelfMany.Length);
            Assert.Same(value, value.SelfMany[0]);
            Assert.Equal(1, value.LazySelfMany.Length);
            Assert.Same(value, value.LazySelfMany[0].Value);
            Assert.Equal(1, value.LazySelfManyMetadata.Length);
            Assert.Same(value, value.LazySelfManyMetadata[0].Value);
        }

        [Export]
        [MefV1.Export, MefV1.PartCreationPolicy(MefV1.CreationPolicy.NonShared)]
        public class SelfImportingNonSharedPart
        {
            [Import]
            [MefV1.Import]
            public SelfImportingNonSharedPart Self { get; set; }
        }

        [MefV1.Export]
        [Export, Shared]
        public class SelfImportingSharedPart
        {
            [Import]
            [MefV1.Import]
            public SelfImportingSharedPart Self { get; set; }

            [Import]
            [MefV1.Import]
            public Lazy<SelfImportingSharedPart> LazySelf { get; set; }

            [Import]
            [MefV1.Import]
            public Lazy<SelfImportingSharedPart, IDictionary<string, object>> LazySelfMetadata { get; set; }

            [ImportMany]
            [MefV1.ImportMany]
            public SelfImportingSharedPart[] SelfMany { get; set; }

            [ImportMany]
            [MefV1.ImportMany]
            public Lazy<SelfImportingSharedPart>[] LazySelfMany { get; set; }

            [ImportMany]
            [MefV1.ImportMany]
            public Lazy<SelfImportingSharedPart, IDictionary<string, object>>[] LazySelfManyMetadata { get; set; }
        }

        #endregion

        #region No loop, but with multiple paths to a common import

        [Fact]
        public void ValidMultiplePaths()
        {
            CompositionConfiguration.Create(
                new AttributedPartDiscovery(), 
                typeof(ValidMultiplePathRoot),
                typeof(ValidMultiplePathTrail1),
                typeof(ValidMultiplePathTrail2),
                typeof(ValidMultiplePathCommonImport));
        }

        [Export]
        public class ValidMultiplePathRoot
        {
            [Import]
            public ValidMultiplePathTrail1 Trail1 { get; set; }

            [Import]
            public ValidMultiplePathTrail2 Trail2 { get; set; }
        }

        [Export]
        public class ValidMultiplePathTrail1
        {
            [Import]
            public ValidMultiplePathCommonImport ImportingProperty { get; set; }
        }

        [Export]
        public class ValidMultiplePathTrail2
        {
            [Import]
            public ValidMultiplePathCommonImport ImportingProperty { get; set; }
        }

        [Export]
        public class ValidMultiplePathCommonImport { }

        #endregion
    }
}
