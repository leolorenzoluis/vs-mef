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

    [Trait("ExportFactory", "")]
    public class ExportFactoryTests
    {
        public ExportFactoryTests()
        {
            NonSharedPart.InstantiationCounter = 0;
            NonSharedPart.DisposalCounter = 0;
        }

        #region V1 tests

        [MefFact(CompositionEngines.V1Compat, typeof(PartFactoryV1), typeof(NonSharedPart))]
        public void ExportFactoryForNonSharedPartCreationDisposalV1(IContainer container)
        {
            var partFactory = container.GetExportedValue<PartFactoryV1>();
            Assert.NotNull(partFactory.Factory);
            Assert.NotNull(partFactory.FactoryWithMetadata);
            Assert.Equal("V", partFactory.FactoryWithMetadata.Metadata["N"]);
            using (var exportContext = partFactory.Factory.CreateExport())
            {
                Assert.NotNull(exportContext);
                Assert.Equal(1, NonSharedPart.InstantiationCounter);

                var value = exportContext.Value;
                Assert.NotNull(value);
                Assert.Equal(0, NonSharedPart.DisposalCounter);
            }

            Assert.Equal(1, NonSharedPart.DisposalCounter);
        }

        [MefFact(CompositionEngines.V1Compat, typeof(PartFactoryV1), typeof(NonSharedPart))]
        public void ExportFactoryForNonSharedPartInstantiatesMultiplePartsV1(IContainer container)
        {
            var partFactory = container.GetExportedValue<PartFactoryV1>();
            var value1 = partFactory.Factory.CreateExport().Value;
            var value2 = partFactory.Factory.CreateExport().Value;
            Assert.NotSame(value1, value2);
        }

        [MefFact(CompositionEngines.V1Compat, typeof(PartFactoryV1WithExplicitContractType), typeof(NonSharedPart))]
        public void ExportFactoryWithExplicitContractTypeV1(IContainer container)
        {
            var partFactory = container.GetExportedValue<PartFactoryV1WithExplicitContractType>();
            Assert.NotNull(partFactory.Factory);
            using (var exportContext = partFactory.Factory.CreateExport())
            {
                Assert.NotNull(exportContext);
                Assert.Equal(1, NonSharedPart.InstantiationCounter);

                var value = exportContext.Value;
                Assert.NotNull(value);
                Assert.Equal(0, NonSharedPart.DisposalCounter);
            }

            Assert.Equal(1, NonSharedPart.DisposalCounter);
        }

        [MefFact(CompositionEngines.V1Compat, typeof(PartFactoryManyV1), typeof(NonSharedPart), typeof(NonSharedPart2))]
        public void ExportFactoryForNonSharedPartManyV1(IContainer container)
        {
            var partFactory = container.GetExportedValue<PartFactoryManyV1>();
            Assert.NotNull(partFactory.Factories);
            Assert.Equal(2, partFactory.Factories.Count());

            Assert.NotNull(partFactory.FactoriesWithMetadata);
            Assert.Equal(2, partFactory.FactoriesWithMetadata.Count());
            var factory1 = partFactory.FactoriesWithMetadata.Single(f => "V".Equals(f.Metadata["N"]));
            var factory2 = partFactory.FactoriesWithMetadata.Single(f => "V2".Equals(f.Metadata["N"]));

            using (var exportContext = factory1.CreateExport())
            {
                Assert.IsType<NonSharedPart>(exportContext.Value);
            }

            using (var exportContext = factory2.CreateExport())
            {
                Assert.IsType<NonSharedPart2>(exportContext.Value);
            }
        }

        /// <summary>
        /// Verifies a very tricky combination of export factories, explicit contract types and open generic exports.
        /// </summary>
        /// <remarks>
        /// CPS did this in Dev12 with MEFv1. I don't know why it doesn't work in this unit test (or in a console app I wrote) against MEFv1.
        /// But somehow it worked in VS. Perhaps due to some nuance in the ExportProviders CPS set up.
        /// </remarks>
        [MefFact(CompositionEngines.V3EmulatingV1, typeof(PartFactoryOfOpenGenericPart), typeof(NonSharedOpenGenericExportPart<>))]
        [Trait("GenericExports", "Open")]
        public void ExportFactoryWithOpenGenericExport(IContainer container)
        {
            var partFactory = container.GetExportedValue<PartFactoryOfOpenGenericPart>();
            Assert.NotNull(partFactory.Factory);
            using (var exportContext = partFactory.Factory.CreateExport())
            {
                var value = exportContext.Value;
                Assert.NotNull(value);
                Assert.IsType<NonSharedOpenGenericExportPart<IDisposable>>(value);
            }
        }

        [MefV1.Export]
        public class PartFactoryV1WithExplicitContractType
        {
            [MefV1.Import(typeof(NonSharedPart))]
            public MefV1.ExportFactory<IDisposable> Factory { get; set; }
        }

        [MefV1.Export]
        public class PartFactoryV1
        {
            [MefV1.Import]
            public MefV1.ExportFactory<NonSharedPart> Factory { get; set; }

            [MefV1.Import]
            public MefV1.ExportFactory<NonSharedPart, IDictionary<string, object>> FactoryWithMetadata { get; set; }
        }

        [MefV1.Export]
        public class PartFactoryManyV1
        {
            [MefV1.ImportMany]
            public IEnumerable<MefV1.ExportFactory<NonSharedPart>> Factories { get; set; }

            [MefV1.ImportMany]
            public IEnumerable<MefV1.ExportFactory<NonSharedPart, IDictionary<string, object>>> FactoriesWithMetadata { get; set; }
        }

        public interface INonSharedOpenGenericExportPart<T> { }

        [MefV1.Export(typeof(NonSharedOpenGenericExportPart<>))]
        [MefV1.PartCreationPolicy(MefV1.CreationPolicy.NonShared)]
        public class NonSharedOpenGenericExportPart<T> : INonSharedOpenGenericExportPart<T>
        {
        }

        [MefV1.Export]
        public class PartFactoryOfOpenGenericPart
        {
            [MefV1.Import(typeof(NonSharedOpenGenericExportPart<IDisposable>))]
            public MefV1.ExportFactory<INonSharedOpenGenericExportPart<IDisposable>> Factory { get; set; }
        }

        [MefFact(CompositionEngines.V1Compat, typeof(PublicFactoryOfInternalPartViaPublicInterface), typeof(InternalPart))]
        public void ExportFactoryForInternalPartViaPublicInterface(IContainer container)
        {
            var factory = container.GetExportedValue<PublicFactoryOfInternalPartViaPublicInterface>();
            var export = factory.InternalPartFactory.CreateExport();
            Assert.NotNull(export.Value);
            Assert.IsType<InternalPart>(export.Value);
        }

        [MefFact(CompositionEngines.V1Compat, typeof(NonPublicFactoryOfInternalPart), typeof(InternalPart))]
        public void ExportFactoryForInternalPart(IContainer container)
        {
            var factory = container.GetExportedValue<NonPublicFactoryOfInternalPart>();
            var export = factory.InternalPartFactory.CreateExport();
            Assert.NotNull(export.Value);
            Assert.IsType<InternalPart>(export.Value);
        }

        [MefV1.Export]
        public class PublicFactoryOfInternalPartViaPublicInterface
        {
            [MefV1.Import]
            public MefV1.ExportFactory<IDisposable> InternalPartFactory { get; set; }
        }

        [MefV1.Export]
        public class NonPublicFactoryOfInternalPart
        {
            [MefV1.Import]
            internal MefV1.ExportFactory<InternalPart> InternalPartFactory { get; set; }
        }

        [MefV1.Export(typeof(IDisposable))]
        [MefV1.Export]
        internal class InternalPart : IDisposable
        {
            public void Dispose()
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region V2 tests

        [MefFact(CompositionEngines.V2Compat, typeof(PartFactoryV2), typeof(NonSharedPart))]
        public void ExportFactoryForNonSharedPartCreationDisposalV2(IContainer container)
        {
            var partFactory = container.GetExportedValue<PartFactoryV2>();
            Assert.NotNull(partFactory.Factory);
            Assert.NotNull(partFactory.FactoryWithMetadata);
            Assert.Equal("V", partFactory.FactoryWithMetadata.Metadata["N"]);
            using (var exportContext = partFactory.Factory.CreateExport())
            {
                Assert.NotNull(exportContext);
                Assert.Equal(1, NonSharedPart.InstantiationCounter);

                var value = exportContext.Value;
                Assert.NotNull(value);
                Assert.Equal(0, NonSharedPart.DisposalCounter);
            }

            Assert.Equal(1, NonSharedPart.DisposalCounter);
        }

        [MefFact(CompositionEngines.V2Compat, typeof(PartFactoryV2), typeof(NonSharedPart))]
        public void ExportFactoryForNonSharedPartInstantiatesMultiplePartsV2(IContainer container)
        {
            var partFactory = container.GetExportedValue<PartFactoryV2>();
            var value1 = partFactory.Factory.CreateExport().Value;
            var value2 = partFactory.Factory.CreateExport().Value;
            Assert.NotSame(value1, value2);
        }

        [MefFact(CompositionEngines.V2Compat, typeof(PartFactoryManyV2), typeof(NonSharedPart), typeof(NonSharedPart2))]
        public void ExportFactoryForNonSharedPartManyV2(IContainer container)
        {
            var partFactory = container.GetExportedValue<PartFactoryManyV2>();
            Assert.NotNull(partFactory.Factories);
            Assert.Equal(2, partFactory.Factories.Count());

            Assert.NotNull(partFactory.FactoriesWithMetadata);
            Assert.Equal(2, partFactory.FactoriesWithMetadata.Count());
            var factory1 = partFactory.FactoriesWithMetadata.Single(f => "V".Equals(f.Metadata["N"]));
            var factory2 = partFactory.FactoriesWithMetadata.Single(f => "V2".Equals(f.Metadata["N"]));

            using (var exportContext = factory1.CreateExport())
            {
                Assert.IsType<NonSharedPart>(exportContext.Value);
            }

            using (var exportContext = factory2.CreateExport())
            {
                Assert.IsType<NonSharedPart2>(exportContext.Value);
            }
        }

        [Export]
        public class PartFactoryV2
        {
            [Import]
            public ExportFactory<NonSharedPart> Factory { get; set; }

            [Import]
            public ExportFactory<NonSharedPart, IDictionary<string, object>> FactoryWithMetadata { get; set; }
        }

        [Export]
        public class PartFactoryManyV2
        {
            [ImportMany]
            public IEnumerable<ExportFactory<NonSharedPart>> Factories { get; set; }

            [ImportMany]
            public IEnumerable<ExportFactory<NonSharedPart, IDictionary<string, object>>> FactoriesWithMetadata { get; set; }
        }

        #endregion

        #region Invalid configuration tests

        [MefFact(CompositionEngines.V1Compat, typeof(ExportWithSharedCreationPolicy), typeof(ExportFactoryOfSharedPartV1Part), InvalidConfiguration = true)]
        public void ExportFactoryOfSharedPartV1(IContainer container)
        {
            container.GetExportedValue<ExportFactoryOfSharedPartV1Part>();
        }

        [MefFact(CompositionEngines.V2, typeof(ExportWithSharedCreationPolicy), typeof(ExportFactoryOfSharedPartV2Part), NoCompatGoal = true)]
        public void ExportFactoryOfSharedPartV2(IContainer container)
        {
            // In V2, ExportFactory around a shared part is actually legal (oddly), and produces the *same* shared value repeatedly.
            var factory = container.GetExportedValue<ExportFactoryOfSharedPartV2Part>();
            var value1 = factory.Factory.CreateExport().Value;
            var value2 = factory.Factory.CreateExport().Value;
            Assert.Same(value1, value2);
        }

        [MefV1.Export, MefV1.PartCreationPolicy(MefV1.CreationPolicy.Shared)]
        [Export, Shared]
        public class ExportWithSharedCreationPolicy { }

        [MefV1.Export]
        public class ExportFactoryOfSharedPartV1Part
        {
            [MefV1.Import]
            public MefV1.ExportFactory<ExportWithSharedCreationPolicy> Factory { get; set; }
        }

        [Export]
        public class ExportFactoryOfSharedPartV2Part
        {
            [Import]
            public ExportFactory<ExportWithSharedCreationPolicy> Factory { get; set; }
        }

        #endregion

        #region ExportFactory with CreatePolicy == Any

        [MefFact(CompositionEngines.V1, typeof(ExportWithAnyCreationPolicy), typeof(ExportFactoryOfAnyCreationPolicyPartV1Part))]
        public void ExportFactoryOfAnyCreationPolicyPartV1(IContainer container)
        {
            var factory = container.GetExportedValue<ExportFactoryOfAnyCreationPolicyPartV1Part>();
            var value1 = factory.Factory.CreateExport().Value;
            var value2 = factory.Factory.CreateExport().Value;
            Assert.NotSame(value1, value2);
        }

        [MefV1.Export]
        [Export]
        public class ExportWithAnyCreationPolicy { }

        [MefV1.Export]
        public class ExportFactoryOfAnyCreationPolicyPartV1Part
        {
            [MefV1.Import]
            public MefV1.ExportFactory<ExportWithAnyCreationPolicy> Factory { get; set; }
        }

        #endregion

        [MefV1.Export, MefV1.PartCreationPolicy(MefV1.CreationPolicy.NonShared)]
        [MefV1.ExportMetadata("N", "V")]
        [Export]
        [ExportMetadata("N", "V")]
        public class NonSharedPart : IDisposable
        {
            internal static int InstantiationCounter;
            internal static int DisposalCounter;

            public NonSharedPart()
            {
                InstantiationCounter++;
            }

            public void Dispose()
            {
                DisposalCounter++;
            }
        }

        [MefV1.Export(typeof(NonSharedPart)), MefV1.PartCreationPolicy(MefV1.CreationPolicy.NonShared)]
        [MefV1.ExportMetadata("N", "V2")]
        [Export(typeof(NonSharedPart))]
        [ExportMetadata("N", "V2")]
        public class NonSharedPart2 : NonSharedPart
        {
        }
    }
}
