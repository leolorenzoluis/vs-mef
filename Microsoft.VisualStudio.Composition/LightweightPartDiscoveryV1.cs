namespace Microsoft.VisualStudio.Composition
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class LightweightPartDiscoveryV1 : LightweightPartDiscovery
    {
        protected override ComposablePartDefinition CreatePart(System.Reflection.Metadata.MetadataReader metadataReader, System.Reflection.Metadata.TypeDefinition typeDefinition)
        {
            return null;
        }
    }
}
