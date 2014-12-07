namespace Microsoft.VisualStudio.Composition.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;
    using MefV1 = System.ComponentModel.Composition;

    public class MefV1CatalogShimTests
    {
        [MefFact(CompositionEngines.V1WithV3Catalog | CompositionEngines.V3EmulatingV1AndV2AtOnce, typeof(Apple))]
        public void SimpleTypeAsV1Catalog(IContainer container)
        {
            var apple = container.GetExportedValue<Apple>();
            Assert.NotNull(apple);
        }

        [Export]
        public class Apple { }
    }
}
