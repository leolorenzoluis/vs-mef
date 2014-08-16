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

    public class LightweightPartDiscoveryV1 : LightweightPartDiscovery
    {
        public override ComposablePartDefinition CreatePart(MetadataReader metadataReader, TypeDefinition typeDefinition,
            HashSet<string> knownExportTypes, string assemblyName, int metadataToken)
        {
            SignatureTypeProvider decoder = new SignatureTypeProvider(metadataReader);
            ComposablePartDefinition toReturn = null;
            var attributes = typeDefinition.GetCustomAttributes().Select(metadataReader.GetCustomAttribute);

            foreach (var attr in attributes)
            {
                var name = UtilityFunctions.GetAttributeTypeName(attr, metadataReader);

                if (knownExportTypes.Contains(name))
                {
                    if (toReturn == null)
                    {
                        var exports = ImmutableList.CreateBuilder<ExportDefinition>();
                        var metadata = ImmutableDictionary.CreateBuilder<string, object>();
                        var typeIdentity = ContractNameServices.GetTypeIdentity(typeDefinition, metadataReader);

                        metadata.Add("ExportTypeIdentity", typeIdentity);
                        metadata.Add("System.ComponentModel.Composition.CreationPolicy", CreationPolicy.NonShared);
                        
                        exports.Add(new ExportDefinition(ContractNameServices.GetTypeIdentity(typeDefinition, metadataReader), metadata));

                        var typeRefArray = ImmutableArray.CreateBuilder<TypeRef>();

                        var fieldHandles = typeDefinition.GetFields();
                        var propertyHandles = typeDefinition.GetProperties();

                        var exportingMembers = ImmutableDictionary.CreateBuilder<MemberRef, IReadOnlyCollection<ExportDefinition>>();
                        
                        var importingMembers = ImmutableArray.CreateBuilder<ImportDefinitionBinding>();
                        foreach (var propHandle in propertyHandles)
                        {
                            var prop = metadataReader.GetProperty(propHandle);
                            foreach (var custAttr in prop.GetCustomAttributes().Select(metadataReader.GetCustomAttribute))
                            {
                                var attrType = UtilityFunctions.GetAttributeTypeName(custAttr, metadataReader);
                                if (attrType == "System.ComponentModel.Composition.ImportAttribute")
                                {
                                    var methodHandles = prop.GetAssociatedMethods();
                                    var accessor = metadataReader.GetMethod(methodHandles.Setter);
                                    var sig = SignatureDecoder.DecodeMethodSignature(accessor.Signature, decoder);
                                    var importingTypeHandle = (TypeHandle)sig.ParameterTypes.First();
                                    var importingTypeDef = metadataReader.GetTypeDefinition(importingTypeHandle);
                                    var importTypeName = ContractNameServices.GetTypeIdentity(importingTypeDef, metadataReader);
                                    var token = metadataReader.GetToken(importingTypeHandle);
                                    var importMetadata = ImmutableDictionary.CreateBuilder<string, object>();
                                    var additionalConstraints = ImmutableArray.CreateBuilder<IImportSatisfiabilityConstraint>();
                                    additionalConstraints.Add(new ExportTypeIdentityConstraint(importTypeName));

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
                                    var importDefinition = new ImportDefinition(
                                        importTypeName,
                                        ImportCardinality.ExactlyOne,
                                        importMetadata.ToImmutable(),
                                        additionalConstraints.ToImmutable());
                                    
                                    var importDefinitionBinding = new ImportDefinitionBinding(
                                        importDefinition,
                                        typeRef,
                                        memberRef);
                                    importingMembers.Add(importDefinitionBinding);
                                    break;
                                }
                            }
                        }
                        
                        var importingConstructors = ImmutableArray.CreateBuilder<ImportDefinitionBinding>();

                        toReturn = new ComposablePartDefinition(
                            Reflection.TypeRef.Get(
                                new AssemblyName(assemblyName),
                                metadataToken, 
                                false,
                                0, 
                                typeRefArray.ToImmutable()),
                            exports.ToImmutable(), 
                            exportingMembers.ToImmutable(),
                            importingMembers.ToImmutable(),
                            null,
                            default(MethodRef), 
                            importingConstructors.ToImmutable(), 
                            CreationPolicy.NonShared);
                    }
                }
            }
            
            return toReturn;
        }

        private class SignatureTypeProvider : ISignatureTypeProvider<object>
        {
            private MetadataReader _reader;
            public SignatureTypeProvider(MetadataReader reader)
            {
                _reader = reader;
            }
            public object GetFunctionPointerType(MethodSignature<object> signature)
            {
                throw new NotImplementedException();
            }

            public object GetGenericMethodParameter(int index)
            {
                throw new NotImplementedException();
            }

            public object GetGenericTypeParameter(int index)
            {
                throw new NotImplementedException();
            }

            public object GetModifiedType(object unmodifiedType, ImmutableArray<CustomModifier<object>> customModifiers)
            {
                throw new NotImplementedException();
            }

            public object GetPinnedType(object elementType)
            {
                throw new NotImplementedException();
            }

            public object GetPrimitiveType(PrimitiveTypeCode typeCode)
            {
                switch (typeCode)
                {
                    case PrimitiveTypeCode.Void:
                        return "void";
                    default:
                        return "unknown type";
                }
            }

            public object GetTypeFromDefinition(TypeHandle handle)
            {
                return handle;
            }

            public object GetTypeFromReference(TypeReferenceHandle handle)
            {
                throw new NotImplementedException();
            }

            public MetadataReader Reader
            {
                get { return _reader; }
            }

            public object GetArrayType(object elementType, ArrayShape shape)
            {
                throw new NotImplementedException();
            }

            public object GetByReferenceType(object elementType)
            {
                throw new NotImplementedException();
            }

            public object GetGenericInstance(object genericType, ImmutableArray<object> typeArguments)
            {
                throw new NotImplementedException();
            }

            public object GetPointerType(object elementType)
            {
                throw new NotImplementedException();
            }

            public object GetSZArrayType(object elementType)
            {
                throw new NotImplementedException();
            }
        }
    }
}
