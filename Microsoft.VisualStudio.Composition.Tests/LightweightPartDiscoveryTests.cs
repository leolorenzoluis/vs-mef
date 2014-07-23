namespace Microsoft.VisualStudio.Composition.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;

    public class LightweightPartDiscoveryTests
    {
        [Fact]
        public void LightweightPartDiscovery()
        {
            var discovery = new LightweightPartDiscoveryV1();
            discovery.CreateParts(File.Open(typeof(string).Assembly.Location, FileMode.Open, FileAccess.Read, FileShare.Read));
        }
    }
}
