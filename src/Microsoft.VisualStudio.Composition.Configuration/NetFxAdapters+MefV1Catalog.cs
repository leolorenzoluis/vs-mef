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

            private class MefV1ComposablePart : MefV1.Primitives.ComposablePart
            {
                private readonly MefV1ComposablePartDefinition definition;

                /// <summary>
                /// The actual instantiated part that we retrieve exports from and set imports to.
                /// </summary>
                private object value;

                internal MefV1ComposablePart(MefV1ComposablePartDefinition definition)
                {
                    Requires.NotNull(definition, "definition");

                    this.definition = definition;
                }

                public override IEnumerable<MefV1.Primitives.ExportDefinition> ExportDefinitions
                {
                    get { return this.definition.ExportDefinitions; }
                }

                public override IEnumerable<MefV1.Primitives.ImportDefinition> ImportDefinitions
                {
                    get { return this.definition.ImportDefinitions; }
                }

                public override object GetExportedValue(MefV1.Primitives.ExportDefinition definition)
                {
                    object value = this.GetInstanceActivatingIfNeeded();
                    return value;
                }

                public override void SetImport(MefV1.Primitives.ImportDefinition definition, IEnumerable<MefV1.Primitives.Export> exports)
                {
                    object value = this.GetInstanceActivatingIfNeeded();

                    var importDefinition = ((MefV1ImportDefinition)definition).ImportDefinitionBinding;
                    ReflectionHelpers.SetMember(value, importDefinition.ImportingMember, exports.First().Value);
                }

                private object GetInstanceActivatingIfNeeded()
                {
                    if (this.value == null && this.definition.PartDefinition.IsInstantiable)
                    {
                        this.value = this.definition.PartDefinition.ImportingConstructorInfo.Invoke(new object[0]);
                    }

                    return this.value;
                }
            }

            private class MefV1ComposablePartDefinition : MefV1.Primitives.ComposablePartDefinition
            {
                private readonly ComposablePartDefinition partDefinition;

                internal MefV1ComposablePartDefinition(ComposablePartDefinition partDefinition)
                    : base()
                {
                    Requires.NotNull(partDefinition, "partDefinition");
                    this.partDefinition = partDefinition;
                }

                public override IEnumerable<MefV1.Primitives.ExportDefinition> ExportDefinitions
                {
                    get { throw new NotImplementedException(); }
                }

                public override IEnumerable<MefV1.Primitives.ImportDefinition> ImportDefinitions
                {
                    get { return this.partDefinition.Imports.Select(i => new MefV1ImportDefinition(i)); }
                }

                internal ComposablePartDefinition PartDefinition
                {
                    get { return this.partDefinition; }
                }

                public override ComposablePart CreatePart()
                {
                    return new MefV1ComposablePart(this);
                }

                internal static MefV1.Primitives.ComposablePartDefinition Wrap(ComposablePartDefinition part)
                {
                    return new MefV1ComposablePartDefinition(part);
                }
            }

            private class MefV1ImportDefinition : MefV1.Primitives.ImportDefinition
            {
                private readonly ImportDefinitionBinding importDefinitionBinding;

                internal MefV1ImportDefinition(ImportDefinitionBinding importDefinition)
                {
                    this.importDefinitionBinding = importDefinition;
                }

                public override string ContractName
                {
                    get { return this.importDefinitionBinding.ImportDefinition.ContractName; }
                }

                internal ImportDefinitionBinding ImportDefinitionBinding
                {
                    get { return this.importDefinitionBinding; }
                }

                internal ImportDefinition ImportDefinition
                {
                    get { return this.importDefinitionBinding.ImportDefinition; }
                }

                public override bool IsConstraintSatisfiedBy(MefV1.Primitives.ExportDefinition exportDefinition)
                {
                    var unwrappedExportDefinition = MefV1ExportDefinition.Unwrap(exportDefinition);
                    return this.ImportDefinition.ExportConstraints.All(c => c.IsSatisfiedBy(unwrappedExportDefinition));
                }
            }

            private class MefV1ExportDefinition : MefV1.Primitives.ExportDefinition
            {
                private readonly ExportDefinition exportDefinition;

                internal MefV1ExportDefinition(ExportDefinition exportDefinition)
                {
                    Requires.NotNull(exportDefinition, "exportDefinition");
                    this.exportDefinition = exportDefinition;
                }

                public override string ContractName
                {
                    get { return this.exportDefinition.ContractName; }
                }

                internal static MefV1.Primitives.ExportDefinition Wrap(ExportDefinition exportDefinition)
                {
                    return new MefV1ExportDefinition(exportDefinition);
                }

                internal static ExportDefinition Unwrap(MefV1.Primitives.ExportDefinition exportDefinition)
                {
                    return new ExportDefinition(exportDefinition.ContractName, ImmutableDictionary.CreateRange(exportDefinition.Metadata));
                }
            }
        }
    }
}
