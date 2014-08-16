namespace Microsoft.VisualStudio.Composition
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Metadata;
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
    }
}
