namespace Microsoft.VisualStudio.Composition
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Metadata;
    using System.Reflection.Metadata.Ecma335;
    using System.Text;
    using System.Threading.Tasks;

    public class LightweightPartDiscoveryV2 : LightweightPartDiscovery
    {
        private static Dictionary<Handle, bool> isExportAttributeCache = new Dictionary<Handle, bool>();
        private static LightweightPartDiscoveryV1 _v1 = new LightweightPartDiscoveryV1();

        public override ComposablePartDefinition CreatePart(MetadataReader metadataReader, TypeDefinition typeDefinition,
            HashSet<string> knownExportTypes, string assemblyName, int metadataToken, bool partExplicitlyRequested)
        {
            //foreach (CustomAttributeHandle typeAttributeHandle in typeDefinition.GetCustomAttributes())
            //{
            //    CustomAttribute attribute = metadataReader.GetCustomAttribute(typeAttributeHandle);
            //    Handle attributeTypeHandle = metadataReader.GetAttributeTypeHandle(attribute);
            //    int token = metadataReader.GetToken(attributeTypeHandle);
            //    //typeof(string).GetTypeInfo().Assembly.ManifestModule

            //    ExportDefinitionBinding exportDefinitionBinding;
            //    this.TryHandleExportAttribute(metadataReader, attribute, attributeTypeHandle, out exportDefinitionBinding);
            //}

            return _v1.CreatePart(metadataReader, typeDefinition, knownExportTypes, assemblyName, metadataToken, partExplicitlyRequested);
        }

        private bool TryHandleExportAttribute(MetadataReader metadataReader, CustomAttribute customAttribute, Handle attributeTypeHandle, out ExportDefinitionBinding exportDefinitionBinding)
        {
            exportDefinitionBinding = null;
            return false;
        }
    }
}
