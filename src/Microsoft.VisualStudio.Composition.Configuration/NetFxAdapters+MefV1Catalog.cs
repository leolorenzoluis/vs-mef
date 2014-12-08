namespace Microsoft.VisualStudio.Composition
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.ComponentModel.Composition.Primitives;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using MefV1 = System.ComponentModel.Composition;

    partial class NetFxAdapters
    {
        private class MefV1Catalog : MefV1.Primitives.ComposablePartCatalog
        {
            private readonly ComposableCatalog catalog;
            private readonly Dictionary<ComposablePartDefinition, MefV1ComposablePartDefinition> partShimMap = new Dictionary<ComposablePartDefinition, MefV1ComposablePartDefinition>();
            private readonly Dictionary<ExportDefinition, MefV1ExportDefinition> exportShimMap = new Dictionary<ExportDefinition, MefV1ExportDefinition>();

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
                                  this.GetShim(exportDefinitionBinding.PartDefinition),
                                  this.GetShim(exportDefinitionBinding.ExportDefinition));

                return exports;
            }

            private MefV1.Primitives.ComposablePartDefinition GetShim(ComposablePartDefinition partDefinition)
            {
                MefV1ComposablePartDefinition result;
                if (!this.partShimMap.TryGetValue(partDefinition, out result))
                {
                    result = this.partShimMap[partDefinition] = new MefV1ComposablePartDefinition(partDefinition);
                }

                return result;
            }

            private MefV1.Primitives.ExportDefinition GetShim(ExportDefinition exportDefinition)
            {
                MefV1ExportDefinition result;
                if ((!this.exportShimMap.TryGetValue(exportDefinition, out result)))
                {
                    result = this.exportShimMap[exportDefinition] = new MefV1ExportDefinition(exportDefinition);
                }

                return result;
            }

            private class MefV1ComposablePart : MefV1.Primitives.ComposablePart
            {
                private readonly MefV1ComposablePartDefinition definition;

                /// <summary>
                /// Collects the exports required to instantiate the MEF part.
                /// </summary>
                private IEnumerable<MefV1.Primitives.Export>[] importingConstructorExports;

                /// <summary>
                /// The actual instantiated part that we retrieve exports from and set imports to.
                /// </summary>
                private object value;

                internal MefV1ComposablePart(MefV1ComposablePartDefinition definition)
                {
                    Requires.NotNull(definition, "definition");

                    this.definition = definition;
                    this.importingConstructorExports = new IEnumerable<MefV1.Primitives.Export>[definition.PartDefinition.ImportingConstructorImports.Count];
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
                    var importDefinition = ((MefV1ImportDefinition)definition).ImportDefinitionBinding;

                    if (importDefinition.ImportingParameter != null)
                    {
                        this.importingConstructorExports[importDefinition.ImportingParameter.Position] = exports;
                    }
                    else
                    {
                        object value = this.GetInstanceActivatingIfNeeded();
                        ReflectionHelpers.SetMember(value, importDefinition.ImportingMember, exports.First().Value);
                    }
                }

                private object GetInstanceActivatingIfNeeded()
                {
                    if (this.value == null && this.definition.PartDefinition.IsInstantiable)
                    {
                        object[] args = new object[this.importingConstructorExports.Length];
                        for (int i = 0; i < this.importingConstructorExports.Length; i++)
                        {
                            args[i] = this.GetValueForImportSite(
                                this.definition.PartDefinition.ImportingConstructorImports[i],
                                this.importingConstructorExports[i]);
                        }

                        this.value = this.definition.PartDefinition.ImportingConstructorInfo.Invoke(args);

                        // We don't need this any more, so free memory.
                        this.importingConstructorExports = null;
                    }

                    return this.value;
                }

                private object GetValueForImportSite(ImportDefinitionBinding importBinding, IEnumerable<MefV1.Primitives.Export> exports)
                {
                    return exports.First().Value;
                }
            }

            private class MefV1ComposablePartDefinition : MefV1.Primitives.ComposablePartDefinition
            {
                private readonly ComposablePartDefinition partDefinition;
                private readonly ImmutableArray<MefV1ImportDefinition> importDefinitions;

                internal MefV1ComposablePartDefinition(ComposablePartDefinition partDefinition)
                    : base()
                {
                    Requires.NotNull(partDefinition, "partDefinition");
                    this.partDefinition = partDefinition;
                    this.importDefinitions = this.partDefinition.Imports.Select(i => new MefV1ImportDefinition(i)).ToImmutableArray();
                }

                public override IEnumerable<MefV1.Primitives.ExportDefinition> ExportDefinitions
                {
                    get { throw new NotImplementedException(); }
                }

                public override IEnumerable<MefV1.Primitives.ImportDefinition> ImportDefinitions
                {
                    get { return this.importDefinitions; }
                }

                internal ComposablePartDefinition PartDefinition
                {
                    get { return this.partDefinition; }
                }

                public override MefV1.Primitives.ComposablePart CreatePart()
                {
                    return new MefV1ComposablePart(this);
                }
            }

            private class MefV1ImportDefinition : MefV1.Primitives.ImportDefinition
            {
                private readonly ImportDefinitionBinding importDefinitionBinding;
                private Expression<Func<MefV1.Primitives.ExportDefinition, bool>> constraint;

                internal MefV1ImportDefinition(ImportDefinitionBinding importDefinition)
                {
                    this.importDefinitionBinding = importDefinition;
                }

                public override string ContractName
                {
                    get { return this.importDefinitionBinding.ImportDefinition.ContractName; }
                }

                public override Expression<Func<MefV1.Primitives.ExportDefinition, bool>> Constraint
                {
                    get
                    {
                        if (this.constraint == null)
                        {
                            this.constraint = ed => this.IsConstraintSatisfiedBy(ed);
                        }

                        return this.constraint;
                    }
                }

                public override bool IsPrerequisite
                {
                    get { return !this.importDefinitionBinding.ImportingParameterRef.IsEmpty; }
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

                internal static ExportDefinition Unwrap(MefV1.Primitives.ExportDefinition exportDefinition)
                {
                    return new ExportDefinition(exportDefinition.ContractName, ImmutableDictionary.CreateRange(exportDefinition.Metadata));
                }
            }
        }
    }
}
