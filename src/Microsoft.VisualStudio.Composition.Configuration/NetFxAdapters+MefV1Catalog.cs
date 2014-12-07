namespace Microsoft.VisualStudio.Composition
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.ComponentModel.Composition.Primitives;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using MefV1 = System.ComponentModel.Composition;

    partial class NetFxAdapters
    {
        private class MefV1Catalog : MefV1.Primitives.ComposablePartCatalog
        {
            private readonly ComposableCatalog catalog;

            internal MefV1Catalog(ComposableCatalog catalog)
            {
                Requires.NotNull(catalog, "catalog");
                this.catalog = catalog;
            }

            public override IEnumerable<Tuple<MefV1.Primitives.ComposablePartDefinition, MefV1.Primitives.ExportDefinition>> GetExports(MefV1.Primitives.ImportDefinition definition)
            {
                var importDefinition = WrapImportDefinition(definition);
                var exports = from exportDefinitionBinding in this.catalog.GetExports(importDefinition)
                              select Tuple.Create(
                                  MefV1ComposablePartDefinition.Wrap(exportDefinitionBinding.PartDefinition),
                                  MefV1ExportDefinition.Wrap(exportDefinitionBinding.ExportDefinition));

                return exports;
            }
        }

        private class MefV1ComposablePartDefinition : MefV1.Primitives.ComposablePartDefinition
        {
            internal MefV1ComposablePartDefinition()
                : base()
            {
            }

            public override IEnumerable<MefV1.Primitives.ExportDefinition> ExportDefinitions
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override IEnumerable<MefV1.Primitives.ImportDefinition> ImportDefinitions
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override ComposablePart CreatePart()
            {
                throw new NotImplementedException();
            }

            internal static MefV1.Primitives.ComposablePartDefinition Wrap(ComposablePartDefinition part)
            {
                return new MefV1ComposablePartDefinition();
            }
        }

        private class MefV1ExportDefinition : MefV1.Primitives.ExportDefinition
        {
            internal MefV1ExportDefinition(ExportDefinition exportDefinition)
            {
            }

            internal static MefV1.Primitives.ExportDefinition Wrap(ExportDefinition exportDefinition)
            {
                return new MefV1ExportDefinition(exportDefinition);
            }
        }
    }
}
