namespace Microsoft.VisualStudio.Composition
{
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

    internal static class UtilityFunctions
    {
        public static string GetAttributeTypeName(CustomAttribute attribute, MetadataReader metadataReader)
        {
            var handle = metadataReader.GetAttributeTypeHandle(attribute);
            var handleType = handle.HandleType;
            
            string name = string.Empty;

            if (handleType == HandleType.Type)
            {
                var td = metadataReader.GetTypeDefinition((TypeHandle)handle);
                name = metadataReader.GetString(td.Namespace) ?? string.Empty;
                if (!string.IsNullOrEmpty(name))
                {
                    name = name + ".";
                }
                name = name + metadataReader.GetString(td.Name);
            }
            else if (handleType == HandleType.TypeReference)
            {
                var tr = metadataReader.GetTypeReference((TypeReferenceHandle)handle);
                name = metadataReader.GetString(tr.Namespace) ?? string.Empty;
                if (!string.IsNullOrEmpty(name))
                {
                    name = name + ".";
                }
                name = name + metadataReader.GetString(tr.Name);
            }

            return name;
        }

        public static void GetContractInfo(CustomAttribute attribute, 
            TypeDefinition type,
            MetadataReader metadataReader,
            out string ContractName,
            out string TypeName,
            bool typeOverridesAttribute = false)
        {
            var encoding = new System.Text.ASCIIEncoding();
            var bytes = metadataReader.GetBytes(attribute.Value);
            if (bytes[2] != 0)
            {
                var byteCount1 = bytes[2];
                var byteOffset1 = 3;
                var bytesSubset1 = new byte[byteCount1];
                for (int i = 0; i < byteCount1; i++)
                {
                    bytesSubset1[i] = bytes[i + byteOffset1];
                }
                ContractName = encoding.GetString(bytesSubset1);

                if (bytes[byteOffset1 + byteCount1] != 0)
                {
                    var byteCount2 = bytes[byteOffset1 + byteCount1];
                    var byteOffset2 = byteOffset1 + byteCount1 + 1;
                    var bytesSubset2 = new byte[byteCount2];
                    for (int i = 0; i < byteCount2; i++)
                    {
                        bytesSubset2[i] = bytes[i + byteOffset2];
                    }
                    TypeName = encoding.GetString(bytesSubset2);
                }
                else
                {
                    TypeName = typeOverridesAttribute ? ContractNameServices.GetTypeIdentity(type, metadataReader) : ContractName;
                }
            }
            else
            {
                TypeName = ContractName = ContractNameServices.GetTypeIdentity(type, metadataReader);
            }
        }

        public static bool TryCreateImportDefinition(CustomAttribute attribute, MethodHandle method, MetadataReader metadataReader, out ImportDefinition importDefinition)
        {
            importDefinition = null;
            bool toReturn = false;

            var attrType = UtilityFunctions.GetAttributeTypeName(attribute, metadataReader);
            if (attrType.Contains("ImportAttribute"))
            {
                var accessor = metadataReader.GetMethod(method);
                SignatureTypeProvider decoder = new SignatureTypeProvider(metadataReader);
                var sig = SignatureDecoder.DecodeMethodSignature(accessor.Signature, decoder);
                var importingTypeHandle = (TypeHandle)sig.ParameterTypes.First();
                var importingTypeDef = metadataReader.GetTypeDefinition(importingTypeHandle);
                string importContractName, importTypeName;
                UtilityFunctions.GetContractInfo(attribute, importingTypeDef, metadataReader, out importContractName, out importTypeName, true);
                var token = metadataReader.GetToken(importingTypeHandle);
                var importMetadata = ImmutableDictionary.CreateBuilder<string, object>();
                var additionalConstraints = ImmutableArray.CreateBuilder<IImportSatisfiabilityConstraint>();
                additionalConstraints.Add(new ExportTypeIdentityConstraint(importTypeName));
                importDefinition = new ImportDefinition(
                    importContractName,
                    ImportCardinality.ExactlyOne,
                    importMetadata.ToImmutable(),
                    additionalConstraints.ToImmutable());
                toReturn = true;
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
