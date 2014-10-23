﻿namespace Microsoft.VisualStudio.Composition
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Composition.Reflection;
    using Validation;

    public class RuntimeComposition : IEquatable<RuntimeComposition>
    {
        private readonly ImmutableHashSet<RuntimePart> parts;
        private readonly IReadOnlyDictionary<TypeRef, RuntimePart> partsByType;
        private readonly IReadOnlyDictionary<string, IReadOnlyCollection<RuntimeExport>> exportsByContractName;
        private readonly IReadOnlyDictionary<TypeRef, RuntimeExport> metadataViewsAndProviders;

        private RuntimeComposition(IEnumerable<RuntimePart> parts, IReadOnlyDictionary<TypeRef, RuntimeExport> metadataViewsAndProviders)
        {
            Requires.NotNull(parts, "parts");
            Requires.NotNull(metadataViewsAndProviders, "metadataViewsAndProviders");

            this.parts = ImmutableHashSet.CreateRange(parts);
            this.metadataViewsAndProviders = metadataViewsAndProviders;

            this.partsByType = this.parts.ToDictionary(p => p.Type, this.parts.Count);

            var exports =
                from part in this.parts
                from export in part.Exports
                group export by export.ContractName into exportsByContract
                select exportsByContract;
            this.exportsByContractName = exports.ToDictionary(
                e => e.Key,
                e => (IReadOnlyCollection<RuntimeExport>)e.ToImmutableArray());
        }

        public IReadOnlyCollection<RuntimePart> Parts
        {
            get { return this.parts; }
        }

        public IReadOnlyDictionary<TypeRef, RuntimeExport> MetadataViewsAndProviders
        {
            get { return this.metadataViewsAndProviders; }
        }

        public static RuntimeComposition CreateRuntimeComposition(CompositionConfiguration configuration)
        {
            Requires.NotNull(configuration, "configuration");

            // PERF/memory tip: We could create all RuntimeExports first, and then reuse them at each import site.
            var parts = configuration.Parts.Select(part => CreateRuntimePart(part, configuration));
            var metadataViewsAndProviders = ImmutableDictionary.CreateRange(
                from viewAndProvider in configuration.MetadataViewsAndProviders
                let viewTypeRef = TypeRef.Get(viewAndProvider.Key)
                let runtimeExport = CreateRuntimeExport(viewAndProvider.Value)
                select new KeyValuePair<TypeRef, RuntimeExport>(viewTypeRef, runtimeExport));
            return new RuntimeComposition(parts, metadataViewsAndProviders);
        }

        public static RuntimeComposition CreateRuntimeComposition(IEnumerable<RuntimePart> parts, IReadOnlyDictionary<TypeRef, RuntimeExport> metadataViewsAndProviders)
        {
            return new RuntimeComposition(parts, metadataViewsAndProviders);
        }

        public IExportProviderFactory CreateExportProviderFactory()
        {
            return new RuntimeExportProviderFactory(this);
        }

        public IReadOnlyCollection<RuntimeExport> GetExports(string contractName)
        {
            IReadOnlyCollection<RuntimeExport> exports;
            if (this.exportsByContractName.TryGetValue(contractName, out exports))
            {
                return exports;
            }

            return ImmutableList<RuntimeExport>.Empty;
        }

        public RuntimePart GetPart(RuntimeExport export)
        {
            Requires.NotNull(export, "export");

            return this.partsByType[export.DeclaringType];
        }

        public RuntimePart GetPart(TypeRef partType)
        {
            Requires.NotNull(partType, "partType");

            return this.partsByType[partType];
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as RuntimeComposition);
        }

        public override int GetHashCode()
        {
            int hashCode = this.parts.Count;
            foreach (var part in this.parts)
            {
                hashCode += part.GetHashCode();
            }

            return hashCode;
        }

        public bool Equals(RuntimeComposition other)
        {
            if (other == null)
            {
                return false;
            }

            return this.parts.SetEquals(other.parts)
                && ByValueEquality.Dictionary<TypeRef, RuntimeExport>().Equals(this.metadataViewsAndProviders, other.metadataViewsAndProviders);
        }

        private static RuntimePart CreateRuntimePart(ComposedPart part, CompositionConfiguration configuration)
        {
            Requires.NotNull(part, "part");

            var runtimePart = new RuntimePart(
                TypeRef.Get(part.Definition.Type),
                part.Definition.ImportingConstructorInfo != null ? new ConstructorRef(part.Definition.ImportingConstructorInfo) : default(ConstructorRef),
                part.GetImportingConstructorImports().Select(kvp => CreateRuntimeImport(kvp.Key, kvp.Value)).ToImmutableArray(),
                part.Definition.ImportingMembers.Select(idb => CreateRuntimeImport(idb, part.SatisfyingExports[idb])).ToImmutableArray(),
                part.Definition.ExportDefinitions.Select(ed => CreateRuntimeExport(ed.Value, part.Definition.Type, ed.Key)).ToImmutableArray(),
                part.Definition.OnImportsSatisfied != null ? new MethodRef(part.Definition.OnImportsSatisfied) : new MethodRef(),
                part.Definition.IsShared ? configuration.GetEffectiveSharingBoundary(part.Definition) : null);
            return runtimePart;
        }

        private static RuntimeImport CreateRuntimeImport(ImportDefinitionBinding importDefinitionBinding, IReadOnlyList<ExportDefinitionBinding> satisfyingExports)
        {
            Requires.NotNull(importDefinitionBinding, "importDefinitionBinding");
            Requires.NotNull(satisfyingExports, "satisfyingExports");

            var runtimeExports = satisfyingExports.Select(export => CreateRuntimeExport(export)).ToImmutableArray();
            if (importDefinitionBinding.ImportingMember != null)
            {
                return new RuntimeImport(
                    new MemberRef(importDefinitionBinding.ImportingMember),
                    importDefinitionBinding.ImportingSiteTypeRef,
                    importDefinitionBinding.ImportDefinition.Cardinality,
                    runtimeExports,
                    PartCreationPolicyConstraint.IsNonSharedInstanceRequired(importDefinitionBinding.ImportDefinition),
                    importDefinitionBinding.IsExportFactory,
                    importDefinitionBinding.ImportDefinition.Metadata,
                    importDefinitionBinding.ImportDefinition.ExportFactorySharingBoundaries);
            }
            else
            {
                return new RuntimeImport(
                    new ParameterRef(importDefinitionBinding.ImportingParameter),
                    importDefinitionBinding.ImportingSiteTypeRef,
                    importDefinitionBinding.ImportDefinition.Cardinality,
                    runtimeExports,
                    PartCreationPolicyConstraint.IsNonSharedInstanceRequired(importDefinitionBinding.ImportDefinition),
                    importDefinitionBinding.IsExportFactory,
                    importDefinitionBinding.ImportDefinition.Metadata,
                    importDefinitionBinding.ImportDefinition.ExportFactorySharingBoundaries);
            }
        }

        private static RuntimeExport CreateRuntimeExport(ExportDefinition exportDefinition, Type partType, MemberRef exportingMember)
        {
            Requires.NotNull(exportDefinition, "exportDefinition");

            return new RuntimeExport(
                exportDefinition.ContractName,
                TypeRef.Get(partType),
                exportingMember,
                TypeRef.Get(ReflectionHelpers.GetExportedValueType(partType, exportingMember.Resolve())),
                exportDefinition.Metadata);
        }

        private static RuntimeExport CreateRuntimeExport(ExportDefinitionBinding exportDefinitionBinding)
        {
            Requires.NotNull(exportDefinitionBinding, "exportDefinitionBinding");
            return CreateRuntimeExport(
                exportDefinitionBinding.ExportDefinition,
                exportDefinitionBinding.PartDefinition.Type,
                exportDefinitionBinding.ExportingMemberRef);
        }

        [DebuggerDisplay("{Type.ResolvedType.FullName,nq}")]
        public class RuntimePart : IEquatable<RuntimePart>
        {
            private ConstructorInfo importingConstructor;
            private MethodInfo onImportsSatisfied;

            public RuntimePart(
                TypeRef type,
                ConstructorRef importingConstructor,
                IReadOnlyList<RuntimeImport> importingConstructorArguments,
                IReadOnlyList<RuntimeImport> importingMembers,
                IReadOnlyList<RuntimeExport> exports,
                MethodRef onImportsSatisfied,
                string sharingBoundary)
            {
                this.Type = type;
                this.ImportingConstructorRef = importingConstructor;
                this.ImportingConstructorArguments = importingConstructorArguments;
                this.ImportingMembers = importingMembers;
                this.Exports = exports;
                this.OnImportsSatisfiedRef = onImportsSatisfied;
                this.SharingBoundary = sharingBoundary;
            }

            public TypeRef Type { get; private set; }

            public ConstructorRef ImportingConstructorRef { get; private set; }

            public IReadOnlyList<RuntimeImport> ImportingConstructorArguments { get; private set; }

            public IReadOnlyList<RuntimeImport> ImportingMembers { get; private set; }

            public IReadOnlyList<RuntimeExport> Exports { get; set; }

            public MethodRef OnImportsSatisfiedRef { get; private set; }

            public string SharingBoundary { get; private set; }

            public bool IsShared
            {
                get { return this.SharingBoundary != null; }
            }

            public bool IsInstantiable
            {
                get { return !this.ImportingConstructorRef.IsEmpty; }
            }

            public ConstructorInfo ImportingConstructor
            {
                get
                {
                    if (this.importingConstructor == null)
                    {
                        this.importingConstructor = this.ImportingConstructorRef.Resolve();
                    }

                    return this.importingConstructor;
                }
            }

            public MethodInfo OnImportsSatisfied
            {
                get
                {
                    if (this.onImportsSatisfied == null)
                    {
                        this.onImportsSatisfied = this.OnImportsSatisfiedRef.Resolve();
                    }

                    return this.onImportsSatisfied;
                }
            }

            public override bool Equals(object obj)
            {
                return this.Equals(obj as RuntimePart);
            }

            public override int GetHashCode()
            {
                return this.Type.GetHashCode();
            }

            public bool Equals(RuntimePart other)
            {
                if (other == null)
                {
                    return false;
                }

                bool result = this.Type.Equals(other.Type)
                    && this.ImportingConstructorRef.Equals(other.ImportingConstructorRef)
                    && this.ImportingConstructorArguments.SequenceEqual(other.ImportingConstructorArguments)
                    && ByValueEquality.EquivalentIgnoreOrder<RuntimeImport>().Equals(this.ImportingMembers, other.ImportingMembers)
                    && ByValueEquality.EquivalentIgnoreOrder<RuntimeExport>().Equals(this.Exports, other.Exports)
                    && this.OnImportsSatisfiedRef.Equals(other.OnImportsSatisfiedRef)
                    && this.SharingBoundary == other.SharingBoundary;
                return result;
            }
        }

        public class RuntimeImport : IEquatable<RuntimeImport>
        {
            private bool? isLazy;
            private Type importingSiteType;
            private Type importingSiteTypeWithoutCollection;
            private Type importingSiteElementType;
            private Func<Func<object>, object, object> lazyFactory;
            private ParameterInfo importingParameter;
            private MemberInfo importingMember;
            private volatile bool isMetadataTypeInitialized;
            private Type metadataType;

            private RuntimeImport(TypeRef importingSiteTypeRef, ImportCardinality cardinality, IReadOnlyList<RuntimeExport> satisfyingExports, bool isNonSharedInstanceRequired, bool isExportFactory, IReadOnlyDictionary<string, object> metadata, IReadOnlyCollection<string> exportFactorySharingBoundaries)
            {
                Requires.NotNull(importingSiteTypeRef, "importingSiteTypeRef");
                Requires.NotNull(satisfyingExports, "satisfyingExports");

                this.Cardinality = cardinality;
                this.SatisfyingExports = satisfyingExports;
                this.IsNonSharedInstanceRequired = isNonSharedInstanceRequired;
                this.IsExportFactory = isExportFactory;
                this.Metadata = metadata;
                this.ImportingSiteTypeRef = importingSiteTypeRef;
                this.ExportFactorySharingBoundaries = exportFactorySharingBoundaries;
            }

            public RuntimeImport(MemberRef importingMember, TypeRef importingSiteTypeRef, ImportCardinality cardinality, IReadOnlyList<RuntimeExport> satisfyingExports, bool isNonSharedInstanceRequired, bool isExportFactory, IReadOnlyDictionary<string, object> metadata, IReadOnlyCollection<string> exportFactorySharingBoundaries)
                : this(importingSiteTypeRef, cardinality, satisfyingExports, isNonSharedInstanceRequired, isExportFactory, metadata, exportFactorySharingBoundaries)
            {
                this.ImportingMemberRef = importingMember;
            }

            public RuntimeImport(ParameterRef importingParameter, TypeRef importingSiteTypeRef, ImportCardinality cardinality, IReadOnlyList<RuntimeExport> satisfyingExports, bool isNonSharedInstanceRequired, bool isExportFactory, IReadOnlyDictionary<string, object> metadata, IReadOnlyCollection<string> exportFactorySharingBoundaries)
                : this(importingSiteTypeRef, cardinality, satisfyingExports, isNonSharedInstanceRequired, isExportFactory, metadata, exportFactorySharingBoundaries)
            {
                this.ImportingParameterRef = importingParameter;
            }

            /// <summary>
            /// Gets the importing member. May be empty if the import site is an importing constructor parameter.
            /// </summary>
            public MemberRef ImportingMemberRef { get; private set; }

            /// <summary>
            /// Gets the importing parameter. May be empty if the import site is an importing field or property.
            /// </summary>
            public ParameterRef ImportingParameterRef { get; private set; }

            public TypeRef ImportingSiteTypeRef { get; private set; }

            public ImportCardinality Cardinality { get; private set; }

            public IReadOnlyCollection<RuntimeExport> SatisfyingExports { get; private set; }

            public bool IsExportFactory { get; private set; }

            public bool IsNonSharedInstanceRequired { get; private set; }

            public IReadOnlyDictionary<string, object> Metadata { get; private set; }

            public Type ExportFactory
            {
                get
                {
                    return this.IsExportFactory
                        ? this.ImportingSiteTypeWithoutCollection
                        : null;
                }
            }

            /// <summary>
            /// Gets the sharing boundaries created when the export factory is used.
            /// </summary>
            public IReadOnlyCollection<string> ExportFactorySharingBoundaries { get; private set; }

            public MemberInfo ImportingMember
            {
                get
                {
                    if (this.importingMember == null)
                    {
                        this.importingMember = this.ImportingMemberRef.Resolve();
                    }

                    return this.importingMember;
                }
            }

            public ParameterInfo ImportingParameter
            {
                get
                {
                    if (this.importingParameter == null)
                    {
                        this.importingParameter = this.ImportingParameterRef.Resolve();
                    }

                    return this.importingParameter;
                }
            }

            public bool IsLazy
            {
                get
                {
                    if (!this.isLazy.HasValue)
                    {
                        this.isLazy = this.ImportingSiteTypeWithoutCollection.IsAnyLazyType();
                    }

                    return this.isLazy.Value;
                }
            }

            public Type ImportingSiteType
            {
                get
                {
                    if (this.importingSiteType == null)
                    {
                        this.importingSiteType = this.ImportingSiteTypeRef.Resolve();
                    }

                    return this.importingSiteType;
                }
            }

            public Type ImportingSiteTypeWithoutCollection
            {
                get
                {
                    if (this.importingSiteTypeWithoutCollection == null)
                    {
                        this.importingSiteTypeWithoutCollection = this.Cardinality == ImportCardinality.ZeroOrMore
                            ? PartDiscovery.GetElementTypeFromMany(this.ImportingSiteType)
                            : this.ImportingSiteType;
                    }

                    return this.importingSiteTypeWithoutCollection;
                }
            }

            /// <summary>
            /// Gets the type of the member, with the ImportMany collection and Lazy/ExportFactory stripped off, when present.
            /// </summary>
            public Type ImportingSiteElementType
            {
                get
                {
                    if (this.importingSiteElementType == null)
                    {
                        this.importingSiteElementType = PartDiscovery.GetTypeIdentityFromImportingType(this.ImportingSiteType, this.Cardinality == ImportCardinality.ZeroOrMore);
                    }

                    return this.importingSiteElementType;
                }
            }

            public Type MetadataType
            {
                get
                {
                    if (!this.isMetadataTypeInitialized)
                    {
                        this.metadataType = this.IsLazy && this.ImportingSiteTypeWithoutCollection.GenericTypeArguments.Length == 2
                            ? this.ImportingSiteTypeWithoutCollection.GenericTypeArguments[1]
                            : null;
                        this.isMetadataTypeInitialized = true;
                    }

                    return this.metadataType;
                }
            }

            public TypeRef DeclaringType
            {
                get
                {
                    return
                        this.ImportingParameterRef.IsEmpty ? this.ImportingMemberRef.DeclaringType :
                        this.ImportingParameterRef.DeclaringType;
                }
            }

            internal Func<Func<object>, object, object> LazyFactory
            {
                get
                {
                    if (this.lazyFactory == null && this.IsLazy)
                    {
                        Type[] lazyTypeArgs = this.ImportingSiteTypeWithoutCollection.GenericTypeArguments;
                        this.lazyFactory = LazyServices.CreateStronglyTypedLazyFactory(this.ImportingSiteElementType, lazyTypeArgs.Length > 1 ? lazyTypeArgs[1] : null);
                    }

                    return this.lazyFactory;
                }
            }

            public override int GetHashCode()
            {
                return this.ImportingMemberRef.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return this.Equals(obj as RuntimeImport);
            }

            public bool Equals(RuntimeImport other)
            {
                if (other == null)
                {
                    return false;
                }

                bool result = EqualityComparer<TypeRef>.Default.Equals(this.ImportingSiteTypeRef, other.ImportingSiteTypeRef)
                    && this.Cardinality == other.Cardinality
                    && ByValueEquality.EquivalentIgnoreOrder<RuntimeExport>().Equals(this.SatisfyingExports, other.SatisfyingExports)
                    && this.IsNonSharedInstanceRequired == other.IsNonSharedInstanceRequired
                    && ByValueEquality.Metadata.Equals(this.Metadata, other.Metadata)
                    && ByValueEquality.EquivalentIgnoreOrder<string>().Equals(this.ExportFactorySharingBoundaries, other.ExportFactorySharingBoundaries)
                    && this.ImportingMemberRef.Equals(other.ImportingMemberRef)
                    && this.ImportingParameterRef.Equals(other.ImportingParameterRef);
                return result;
            }
        }

        public class RuntimeExport : IEquatable<RuntimeExport>
        {
            private MemberInfo member;

            public RuntimeExport(string contractName, TypeRef declaringType, MemberRef memberRef, TypeRef exportedValueType, IReadOnlyDictionary<string, object> metadata)
            {
                Requires.NotNull(metadata, "metadata");
                Requires.NotNullOrEmpty(contractName, "contractName");

                this.ContractName = contractName;
                this.DeclaringType = declaringType;
                this.MemberRef = memberRef;
                this.ExportedValueType = exportedValueType;
                this.Metadata = metadata;
            }

            public string ContractName { get; private set; }

            public TypeRef DeclaringType { get; private set; }

            public MemberRef MemberRef { get; private set; }

            public TypeRef ExportedValueType { get; private set; }

            public IReadOnlyDictionary<string, object> Metadata { get; private set; }

            public MemberInfo Member
            {
                get
                {
                    if (this.member == null)
                    {
                        this.member = this.MemberRef.Resolve();
                    }

                    return this.member;
                }
            }

            public override int GetHashCode()
            {
                return this.ContractName.GetHashCode() + this.DeclaringType.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return this.Equals(obj as RuntimeExport);
            }

            public bool Equals(RuntimeExport other)
            {
                if (other == null)
                {
                    return false;
                }

                bool result = this.ContractName == other.ContractName
                    && EqualityComparer<TypeRef>.Default.Equals(this.DeclaringType, other.DeclaringType)
                    && EqualityComparer<MemberRef>.Default.Equals(this.MemberRef, other.MemberRef)
                    && EqualityComparer<TypeRef>.Default.Equals(this.ExportedValueType, other.ExportedValueType)
                    && ByValueEquality.Metadata.Equals(this.Metadata, other.Metadata);
                return result;
            }
        }
    }
}
