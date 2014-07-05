namespace Microsoft.VisualStudio.Composition
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class LightweightPartDiscoveryV1 : LightweightPartDiscovery
    {
        public override ComposablePartDefinition CreatePart(CodeAnalysis.INamedTypeSymbol typeSymbol)
        {
            throw new NotImplementedException();
        }
    }
}
