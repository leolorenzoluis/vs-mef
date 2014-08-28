namespace Microsoft.VisualStudio.Composition
{
    using Microsoft.VisualStudio.Composition.Reflection;
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Metadata;
    using System.Reflection.Metadata.Decoding;
    using System.Reflection.Metadata.Ecma335;
    using System.Text;
    using System.Threading.Tasks;
using Validation;

    public class LightweightPartDiscoveryV1 : LightweightPartDiscovery
    {
        public override ComposablePartDefinition CreatePart(MetadataReader metadataReader, TypeDefinition typeDefinition,
            HashSet<string> knownExportTypes, string assemblyName, int metadataToken, bool typeExplicitlyRequested)
        {
            // We want to ignore abstract classes, but we want to consider static classes.
            // Static classes claim to be both abstract and sealed. So to ignore just abstract
            // ones, we check that they are not sealed.
            //// if (partType.IsAbstract && !partType.IsSealed)
            if ((typeDefinition.Attributes & (TypeAttributes.Sealed | TypeAttributes.Abstract)) == TypeAttributes.Abstract)
            {
                return null;
            }

            var attributes = typeDefinition.GetCustomAttributes().Select(metadataReader.GetCustomAttribute);
            Dictionary<string, CustomAttribute> customAttributes = new Dictionary<string, CustomAttribute>();

            foreach (var attr in attributes)
            {
                var name = UtilityFunctions.GetAttributeTypeName(attr, metadataReader);
                customAttributes[name] = attr;
            }

            if (!typeExplicitlyRequested && customAttributes.ContainsKey("System.Composition.PartNotDiscoverableAttribute"))
            {
                return null;
            }

            var partCreationPolicy = CreationPolicy.Any;
            CustomAttribute partCreationAttri;

            if (customAttributes.TryGetValue("System.ComponentModel.Composition.PartCreationPolicyAttribute", out partCreationAttri))
            {
                var blob = metadataReader.GetBytes(partCreationAttri.Value);
                partCreationPolicy = (CreationPolicy)blob[2];
            }

            var allExportsMetadata = ImmutableDictionary.CreateRange(PartCreationPolicyConstraint.GetExportMetadata(partCreationPolicy));

            var inheritedExportContractNamesFromNonInterfaces = ImmutableHashSet.CreateBuilder<string>();
            var exportsOnType = ImmutableList.CreateBuilder<ExportDefinition>();
            var exportsOnMembers = ImmutableDictionary.CreateBuilder<MemberRef, IReadOnlyCollection<ExportDefinition>>();
            var imports = ImmutableList.CreateBuilder<ImportDefinitionBinding>();
            var exportingMembers = ImmutableDictionary.CreateBuilder<MemberRef, IReadOnlyCollection<ExportDefinition>>();
            var importingMembers = ImmutableArray.CreateBuilder<ImportDefinitionBinding>();

            ////foreach (var exportAttributes in partType.GetCustomAttributesByType<ExportAttribute>())
            ////{
            if (customAttributes.Keys.Any(knownExportTypes.Contains))
            {
                var customAttribute = customAttributes.Where(k => knownExportTypes.Contains(k.Key)).First().Value;
                ////    var exportMetadataOnType = allExportsMetadata.AddRange(GetExportMetadata(exportAttributes.Key.GetCustomAttributesCached()));
                ////    foreach (var exportAttribute in exportAttributes)
                ////    {
                ////        if (exportAttributes.Key != partType && !(exportAttribute is InheritedExportAttribute))
                ////        {
                ////            // We only look at base types when the attribute we're considering is
                ////            // or derives from InheritedExportAttribute.
                ////            // Not it isn't the AttributeUsage.Inherits property.
                ////            // To match MEFv1 behavior, it's these two special attributes themselves that define the semantics.
                ////            continue;
                ////        }

                ////        var partTypeAsGenericTypeDefinition = partType.IsGenericType ? partType.GetGenericTypeDefinition() : null;
                string contractName, exportTypeName;
                ////        Type exportedType = exportAttribute.ContractType ?? partTypeAsGenericTypeDefinition ?? exportAttributes.Key;
                ////        string contractName = string.IsNullOrEmpty(exportAttribute.ContractName) ? GetContractName(exportedType) : exportAttribute.ContractName;
                UtilityFunctions.GetContractInfo(customAttribute, typeDefinition, metadataReader, out contractName, out exportTypeName);
                ////        if (exportAttribute is InheritedExportAttribute)
                ////        {
                ////            if (inheritedExportContractNamesFromNonInterfaces.Contains(contractName))
                ////            {
                ////                // We already have an export with this contract name on this type (from a more derived type)
                ////                // using InheritedExportAttribute.
                ////                continue;
                ////            }

                ////            if (!exportAttributes.Key.IsInterface)
                ////            {
                ////                inheritedExportContractNamesFromNonInterfaces.Add(contractName);
                ////            }
                ////        }

                ////        var exportMetadata = exportMetadataOnType
                ////            .Add(CompositionConstants.ExportTypeIdentityMetadataName, ContractNameServices.GetTypeIdentity(exportedType));
                var metadata = allExportsMetadata.Add("ExportTypeIdentity", exportTypeName);
                ////        var exportDefinition = new ExportDefinition(contractName, exportMetadata);
                ////        exportsOnType.Add(exportDefinition);
                exportsOnType.Add(new ExportDefinition(contractName, metadata));
                ////    }
                ////}

                var flags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

                var fieldHandles = typeDefinition.GetFields();
                var propertyHandles = typeDefinition.GetProperties();

                foreach (var propHandle in propertyHandles)
                {
                    var prop = metadataReader.GetProperty(propHandle);

                    ////foreach (var member in Enumerable.Concat<MemberInfo>(partType.EnumProperties(), partType.EnumFields()))
                    ////{
                    ////    var property = member as PropertyInfo;
                    ////    var field = member as FieldInfo;
                    ////    var propertyOrFieldType = ReflectionHelpers.GetMemberType(member);
                    var memberAttributes = prop.GetCustomAttributes().Select(metadataReader.GetCustomAttribute).GroupBy(attr => UtilityFunctions.GetAttributeTypeName(attr, metadataReader));
                    bool hasImport = false, hasImportMany = false;
                    CustomAttribute importAttribute = default(CustomAttribute), importManyAttribute;
                    IGrouping<string, CustomAttribute> exportAttributes = null;

                    foreach (var group in memberAttributes)
                    {
                        if (group.Key.Contains("ImportAttribute"))
                        {
                            hasImport = true;
                            importAttribute = group.First();
                        }
                        else if (group.Key.Contains("ImportManyAttribute"))
                        {
                            hasImportMany = true;
                            importManyAttribute = group.First();
                        }
                        else if (knownExportTypes.Contains(group.Key))
                        {
                            exportAttributes = group;
                        }
                    }

                    Requires.Argument(!(hasImport && hasImportMany), "partType", "Member \"{0}\" contains both ImportAttribute and ImportManyAttribute.", metadataReader.GetString(prop.Name));
                    Requires.Argument(!(exportAttributes != null && (hasImport || hasImportMany)), "partType", "Member \"{0}\" contains both import and export attributes.", metadataReader.GetString(prop.Name));

                    var methodHandles = prop.GetAssociatedMethods();
                    ImportDefinition importDefinition;
                    if (UtilityFunctions.TryCreateImportDefinition(importAttribute, methodHandles.Setter, metadataReader, out importDefinition))
                    {
                        var typeRefArray = ImmutableArray.CreateBuilder<TypeRef>();
                        var typeRef = Reflection.TypeRef.Get(
                            new AssemblyName(assemblyName),
                            metadataToken,
                            false,
                            0,
                            typeRefArray.ToImmutable());
                        var propertyRef = new PropertyRef(
                            typeRef,
                            metadataReader.GetToken(propHandle),
                            metadataReader.GetToken(methodHandles.Getter),
                            metadataReader.GetToken(methodHandles.Setter));
                        var memberRef = new MemberRef(propertyRef);
                        imports.Add(new ImportDefinitionBinding(importDefinition, typeRef, memberRef));
                    }
                }
                ////    else if (exportAttributes.Any())
                ////    {
                ////        Verify.Operation(!partType.IsGenericTypeDefinition, "Exports on members not allowed when the declaring type is generic.");
                ////        var exportMetadataOnMember = allExportsMetadata.AddRange(GetExportMetadata(member.GetCustomAttributesCached()));
                ////        var exportDefinitions = ImmutableList.Create<ExportDefinition>();
                ////        foreach (var exportAttribute in exportAttributes)
                ////        {
                ////            Type exportedType = exportAttribute.ContractType ?? propertyOrFieldType;
                ////            string contractName = string.IsNullOrEmpty(exportAttribute.ContractName) ? GetContractName(exportedType) : exportAttribute.ContractName;
                ////            var exportMetadata = exportMetadataOnMember
                ////                .Add(CompositionConstants.ExportTypeIdentityMetadataName, ContractNameServices.GetTypeIdentity(exportedType));
                ////            var exportDefinition = new ExportDefinition(contractName, exportMetadata);
                ////            exportDefinitions = exportDefinitions.Add(exportDefinition);
                ////        }

                ////        exportsOnMembers.Add(MemberRef.Get(member), exportDefinitions);
                ////    }
                ////}

                ////foreach (var method in partType.GetMethods(flags))
                ////{
                ////    var exportAttributes = method.GetCustomAttributesCached<ExportAttribute>();
                ////    if (exportAttributes.Any())
                ////    {
                ////        var exportMetadataOnMember = allExportsMetadata.AddRange(GetExportMetadata(method.GetCustomAttributesCached()));
                ////        var exportDefinitions = ImmutableList.Create<ExportDefinition>();
                ////        foreach (var exportAttribute in exportAttributes)
                ////        {
                ////            Type exportedType = exportAttribute.ContractType ?? ReflectionHelpers.GetContractTypeForDelegate(method);
                ////            string contractName = string.IsNullOrEmpty(exportAttribute.ContractName) ? GetContractName(exportedType) : exportAttribute.ContractName;
                ////            var exportMetadata = exportMetadataOnMember
                ////                .Add(CompositionConstants.ExportTypeIdentityMetadataName, ContractNameServices.GetTypeIdentity(exportedType));
                ////            var exportDefinition = new ExportDefinition(contractName, exportMetadata);
                ////            exportDefinitions = exportDefinitions.Add(exportDefinition);
                ////        }

                ////        exportsOnMembers.Add(MemberRef.Get(method), exportDefinitions);
                ////    }
                ////}

                ////MethodInfo onImportsSatisfied = null;
                ////if (typeof(IPartImportsSatisfiedNotification).IsAssignableFrom(partType))
                ////{
                ////    onImportsSatisfied = typeof(IPartImportsSatisfiedNotification).GetMethod("OnImportsSatisfied", BindingFlags.Public | BindingFlags.Instance);
                ////}

                ////if (exportsOnMembers.Count > 0 || exportsOnType.Count > 0)
                ////{
                ////    var importingConstructorParameters = ImmutableList.CreateBuilder<ImportDefinitionBinding>();
                ////    var importingCtor = GetImportingConstructor<ImportingConstructorAttribute>(partType, publicOnly: false);
                ////    if (importingCtor != null) // some parts have exports merely for metadata -- they can't be instantiated
                ////    {
                ////        foreach (var parameter in importingCtor.GetParameters())
                ////        {
                ////            var import = CreateImport(parameter, parameter.GetCustomAttributesCached());
                ////            if (import.ImportDefinition.Cardinality == ImportCardinality.ZeroOrMore)
                ////            {
                ////                Verify.Operation(PartDiscovery.IsImportManyCollectionTypeCreateable(import), "Collection must be public with a public constructor when used with an [ImportingConstructor].");
                ////            }

                ////            importingConstructorParameters.Add(import);
                ////        }
                ////    }
                var importingConstructors = ImmutableArray.CreateBuilder<ImportDefinitionBinding>();

                var refArray = ImmutableArray.CreateBuilder<TypeRef>();
                ////    return new ComposablePartDefinition(
                return new ComposablePartDefinition(
                    ////        TypeRef.Get(partType),
                    Reflection.TypeRef.Get(
                        new AssemblyName(assemblyName),
                        metadataToken,
                        false,
                        0,
                        refArray.ToImmutable()),
                    ////        exportsOnType.ToImmutable(),
                    exportsOnType.ToImmutable(),
                    ////        exportsOnMembers.ToImmutable(),
                    exportingMembers.ToImmutable(),
                    imports.ToImmutable(),
                    partCreationPolicy != CreationPolicy.NonShared ? string.Empty : null,
                    ////        MethodRef.Get(onImportsSatisfied),
                    default(MethodRef),
                    ////        importingCtor != null ? importingConstructorParameters.ToImmutable() : null, // some MEF parts are only for metadata
                    importingConstructors.ToImmutable(),
                    partCreationPolicy,
                    partCreationPolicy != CreationPolicy.NonShared);
            }
            return null;
        }
    }
}
