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

    public class DisposablePartsTests
    {
        #region Disposable part happy path test

        [MefFact(CompositionEngines.V1Compat | CompositionEngines.V2Compat, typeof(DisposableNonSharedPart), typeof(UninstantiatedNonSharedPart))]
        public void DisposableNonSharedPartDisposedWithContainerAfterDirectAcquisition(IContainer container)
        {
            var part = container.GetExportedValue<DisposableNonSharedPart>();
            Assert.False(part.IsDisposed);
            container.Dispose();
            Assert.True(part.IsDisposed);
        }

        [MefFact(CompositionEngines.V1Compat | CompositionEngines.V2Compat, typeof(DisposableNonSharedPart))]
        public void DisposableNonSharedPartDisposedWithContainerForAllInstancesAndThenReleased(IContainer container)
        {
            // The allocations have to happen in another method so that any references held by locals
            // that the compiler creates and we can't directly clear are definitely released.
            var weakRefs = DisposableNonSharedPartDisposedWithContainerForAllInstancesAndThenReleased_Helper(container);
            GC.Collect();
            Assert.True(weakRefs.All(r => !r.IsAlive));
        }

        private static WeakReference[] DisposableNonSharedPartDisposedWithContainerForAllInstancesAndThenReleased_Helper(IContainer container)
        {
            var weakRefs = new WeakReference[3];
            var parts = new DisposableNonSharedPart[weakRefs.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = container.GetExportedValue<DisposableNonSharedPart>();
            }

            Assert.True(parts.All(p => !p.IsDisposed));
            container.Dispose();
            Assert.True(parts.All(p => p.IsDisposed));

            // Verify that the container is not holding references any more.
            for (int i = 0; i < parts.Length; i++)
            {
                weakRefs[i] = new WeakReference(parts[i]);
            }

            return weakRefs;
        }

        [MefFact(CompositionEngines.V1Compat | CompositionEngines.V2Compat, typeof(DisposableSharedPart))]
        public void DisposableSharedPartDisposedWithContainerAfterDirectAcquisition(IContainer container)
        {
            var part = container.GetExportedValue<DisposableSharedPart>();
            Assert.False(part.IsDisposed);
            container.Dispose();
            Assert.True(part.IsDisposed);
        }

        [MefFact(CompositionEngines.V1Compat | CompositionEngines.V2Compat, typeof(DisposableNonSharedPart), typeof(UninstantiatedNonSharedPart), typeof(NonSharedPartThatImportsDisposableNonSharedPart))]
        public void DisposableNonSharedPartDisposedWithContainerAfterImportToAnotherPart(IContainer container)
        {
            var part = container.GetExportedValue<NonSharedPartThatImportsDisposableNonSharedPart>();
            Assert.False(part.ImportOfDisposableNonSharedPart.IsDisposed);
            container.Dispose();
            Assert.True(part.ImportOfDisposableNonSharedPart.IsDisposed);
        }

        [Export]
        [MefV1.Export, MefV1.PartCreationPolicy(MefV1.CreationPolicy.NonShared)]
        public class NonSharedPartThatImportsDisposableNonSharedPart
        {
            [Import, MefV1.Import]
            public DisposableNonSharedPart ImportOfDisposableNonSharedPart { get; set; }
        }

        [Export]
        [MefV1.Export, MefV1.PartCreationPolicy(MefV1.CreationPolicy.NonShared)]
        public class DisposableNonSharedPart : IDisposable
        {
            public bool IsDisposed { get; private set; }

            public void Dispose()
            {
                this.IsDisposed = true;
            }
        }

        [Export, Shared]
        [MefV1.Export]
        public class DisposableSharedPart : IDisposable
        {
            public bool IsDisposed { get; private set; }

            public void Dispose()
            {
                this.IsDisposed = true;
            }
        }

        [Export]
        [MefV1.Export, MefV1.PartCreationPolicy(MefV1.CreationPolicy.NonShared)]
        public class UninstantiatedNonSharedPart : IDisposable
        {
            public UninstantiatedNonSharedPart()
            {
                Assert.False(true, "This should never be instantiated.");
            }

            public void Dispose()
            {
            }
        }

        #endregion

        #region Part disposed on exception test

        [MefFact(CompositionEngines.V1Compat | CompositionEngines.V2Compat, typeof(ThrowingPart), typeof(ImportToThrowingPart))]
        public void PartDisposedWhenThrows(IContainer container)
        {
            ThrowingPart.InstantiatedCounter = 0;
            ThrowingPart.DisposedCounter = 0;

            // We don't use Assert.Throws<T> for this next bit because the containers vary in what
            // exception type they throw, and this test isn't about verifying which exception is thrown.
            try
            {
                container.GetExportedValue<ThrowingPart>();
                Assert.False(true, "An exception should have been thrown.");
            }
            catch { }

            Assert.Equal(1, ThrowingPart.InstantiatedCounter);
            Assert.Equal(0, ThrowingPart.DisposedCounter);

            container.Dispose();
            Assert.Equal(1, ThrowingPart.InstantiatedCounter);
            Assert.Equal(1, ThrowingPart.DisposedCounter);
        }

        [Export]
        [MefV1.Export, MefV1.PartCreationPolicy(MefV1.CreationPolicy.NonShared)]
        public class ThrowingPart : IDisposable
        {
            internal static int InstantiatedCounter;
            internal static int DisposedCounter;

            public ThrowingPart()
            {
                InstantiatedCounter++;
            }

            [Import]
            [MefV1.Import]
            public ImportToThrowingPart ImportProperty
            {
                set { throw new ApplicationException(); }
            }

            public void Dispose()
            {
                DisposedCounter++;
            }
        }

        [Export]
        [MefV1.Export, MefV1.PartCreationPolicy(MefV1.CreationPolicy.NonShared)]
        public class ImportToThrowingPart
        {
        }

        #endregion

        #region Internal Disposable part test

        [Trait("Access", "NonPublic")]
        [MefFact(CompositionEngines.V1Compat, typeof(InternalDisposablePart))]
        public void InternalDisposablePartDisposedWithContainer(IContainer container)
        {
            var part = container.GetExportedValue<InternalDisposablePart>();
            Assert.False(part.IsDisposed);
            container.Dispose();
            Assert.True(part.IsDisposed);
        }

        [MefV1.Export]
        internal class InternalDisposablePart : IDisposable
        {
            public bool IsDisposed { get; private set; }

            public void Dispose()
            {
                this.IsDisposed = true;
            }
        }

        #endregion

        [MefFact(CompositionEngines.V1Compat, new Type[0])]
        public void ContainerThrowsAfterDisposal(IContainer container)
        {
            container.Dispose();
            Assert.Throws<ObjectDisposedException>(() => container.GetExport<string>());
            Assert.Throws<ObjectDisposedException>(() => container.GetExport<string>(null));
            Assert.Throws<ObjectDisposedException>(() => container.GetExport<string, IDictionary<string, object>>());
            Assert.Throws<ObjectDisposedException>(() => container.GetExport<string, IDictionary<string, object>>(null));
            Assert.Throws<ObjectDisposedException>(() => container.GetExportedValue<string>());
            Assert.Throws<ObjectDisposedException>(() => container.GetExportedValue<string>(null));
            Assert.Throws<ObjectDisposedException>(() => container.GetExportedValues<string>());
            Assert.Throws<ObjectDisposedException>(() => container.GetExportedValues<string>(null));
            Assert.Throws<ObjectDisposedException>(() => container.GetExports<string>());
            Assert.Throws<ObjectDisposedException>(() => container.GetExports<string>(null));
            Assert.Throws<ObjectDisposedException>(() => container.GetExports<string, IDictionary<string, object>>());
            Assert.Throws<ObjectDisposedException>(() => container.GetExports<string, IDictionary<string, object>>(null));
            container.Dispose();
            container.ToString();
            container.GetHashCode();
        }

        #region Disposal evaluates Lazy import which then tries to import the disposed part

        [MefFact(CompositionEngines.V1Compat | CompositionEngines.V3EmulatingV2, typeof(PartWithDisposeThatEvaluatesLazyImport), typeof(PartThatImportsDisposeWithLazyImport))]
        public void DisposeEvaluatesLazyImportThatLoopsBackV1(IContainer container)
        {
            var value = container.GetExportedValue<PartWithDisposeThatEvaluatesLazyImport>();
            Assert.Throws<ObjectDisposedException>(() => container.Dispose());
        }

        [MefFact(CompositionEngines.V2, typeof(PartWithDisposeThatEvaluatesLazyImport), typeof(PartThatImportsDisposeWithLazyImport), NoCompatGoal = true)]
        public void DisposeEvaluatesLazyImportThatLoopsBackV2(IContainer container)
        {
            var value = container.GetExportedValue<PartWithDisposeThatEvaluatesLazyImport>();
            container.Dispose();
        }

        [Export, Shared]
        [MefV1.Export]
        public class PartWithDisposeThatEvaluatesLazyImport : IDisposable
        {
            [Import, MefV1.Import]
            public Lazy<PartThatImportsDisposeWithLazyImport> LazyImport { get; set; }

            public void Dispose()
            {
                var other = this.LazyImport.Value;

                // Although we may expect the above line to throw, if it didn't (like in V2)
                // we assert that the follow should be true.
                Assert.Same(this, other.ImportingProperty);
            }
        }

        [Export, Shared]
        [MefV1.Export]
        public class PartThatImportsDisposeWithLazyImport
        {
            [Import, MefV1.Import]
            public PartWithDisposeThatEvaluatesLazyImport ImportingProperty { get; set; }
        }

        #endregion

        #region Disposal of sharing boundary part evaluates Lazy import which then tries to import the disposed part

        [MefFact(CompositionEngines.V2, typeof(SharingBoundaryFactory), typeof(SharingBoundaryPartWithDisposeThatEvaluatesLazyImport), typeof(PartThatImportsSharingBoundaryDisposeWithLazyImport), NoCompatGoal = true)]
        public void DisposeOfSharingBoundaryPartEvaluatesLazyImportThatLoopsBackV2(IContainer container)
        {
            var factory = container.GetExportedValue<SharingBoundaryFactory>();
            var export = factory.Factory.CreateExport();
            export.Dispose();
        }

        [MefFact(CompositionEngines.V3EmulatingV2, typeof(SharingBoundaryFactory), typeof(SharingBoundaryPartWithDisposeThatEvaluatesLazyImport), typeof(PartThatImportsSharingBoundaryDisposeWithLazyImport))]
        public void DisposeOfSharingBoundaryPartEvaluatesLazyImportThatLoopsBackV3(IContainer container)
        {
            var factory = container.GetExportedValue<SharingBoundaryFactory>();
            var export = factory.Factory.CreateExport();
            Assert.Throws<ObjectDisposedException>(() => export.Dispose());
        }

        [Export, Shared]
        public class SharingBoundaryFactory
        {
            [Import, SharingBoundary("A")]
            public ExportFactory<SharingBoundaryPartWithDisposeThatEvaluatesLazyImport> Factory { get; set; }
        }

        [Export, Shared("A")]
        public class SharingBoundaryPartWithDisposeThatEvaluatesLazyImport : IDisposable
        {
            [Import]
            public Lazy<PartThatImportsSharingBoundaryDisposeWithLazyImport> LazyImport { get; set; }

            public void Dispose()
            {
                var other = this.LazyImport.Value;

                // Although we may expect the above line to throw, if it didn't (like in V2)
                // we assert that the follow should be true.
                Assert.Same(this, other.ImportedArgument);
            }
        }

        [Export, Shared("A")]
        public class PartThatImportsSharingBoundaryDisposeWithLazyImport
        {
            /// <summary>
            /// This is deliberately an importing constructor rather than an importing property
            /// so as to exercise the code path that was misbehaving when we wrote the test.
            /// </summary>
            [ImportingConstructor]
            public PartThatImportsSharingBoundaryDisposeWithLazyImport(SharingBoundaryPartWithDisposeThatEvaluatesLazyImport importingArg)
            {
                this.ImportedArgument = importingArg;
            }

            public SharingBoundaryPartWithDisposeThatEvaluatesLazyImport ImportedArgument { get; set; }
        }

        #endregion
    }
}
