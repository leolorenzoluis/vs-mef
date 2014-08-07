using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Composition
{
    public interface IPartDiscovery
    {
        DiscoveredParts CreateParts(IEnumerable<Type> types);
        DiscoveredParts CreateParts(IEnumerable<Assembly> assemblies);        
    }
}
