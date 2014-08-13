namespace Microsoft.VisualStudio.Composition
{
    using Microsoft.VisualStudio.Composition.Reflection;
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Metadata;
    using System.Reflection.Metadata.Ecma335;
    using System.Text;
    using System.Threading.Tasks;

    public class LightweightPartDiscoveryV1 : LightweightPartDiscovery
    {
        public override ComposablePartDefinition CreatePart(MetadataReader metadataReader, TypeDefinition typeDefinition,
            HashSet<string> knownExportTypes, string assemblyName, int metadataToken)
        {
            ComposablePartDefinition toReturn = null;
            var attributes = typeDefinition.GetCustomAttributes().Select(metadataReader.GetCustomAttribute);

            foreach (var attr in attributes)
            {
                var handle = metadataReader.GetAttributeTypeHandle(attr);
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

                if (knownExportTypes.Contains(name))
                {
                    if (toReturn == null)
                    {
                        var exports = ImmutableList.CreateBuilder<ExportDefinition>();
                        var metadata = ImmutableDictionary.CreateBuilder<string, object>();
                        var typeIdentity = ContractNameServices.GetTypeIdentity(typeDefinition, metadataReader);

                        metadata.Add("ExportTypeIdentity", typeIdentity);
                        metadata.Add("System.ComponentModel.Composition.CreationPolicy", "NonShared");
                        
                        exports.Add(new ExportDefinition(ContractNameServices.GetTypeIdentity(typeDefinition, metadataReader), metadata));

                        var typeRefArray = ImmutableArray.CreateBuilder<TypeRef>();
                        var exportingMembers = ImmutableDictionary.CreateBuilder<MemberRef, IReadOnlyCollection<ExportDefinition>>();
                        var importingMembers = ImmutableArray.CreateBuilder<ImportDefinitionBinding>();
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
    }
}
