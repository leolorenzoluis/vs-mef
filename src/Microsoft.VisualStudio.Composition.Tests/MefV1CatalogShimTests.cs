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
        [Fact]
        public async Task SimpleTypeAsV1Catalog()
        {
            var v2Discovery = new AttributedPartDiscovery();
            var discoveryResults = await v2Discovery.CreatePartsAsync(typeof(Apple));
            var catalog = ComposableCatalog.Create(discoveryResults);
            var v1Catalog = catalog.AsComposablePartCatalog();

            var v1Container = new MefV1.Hosting.CompositionContainer(v1Catalog);
            var apple = v1Container.GetExportedValue<Apple>();
            Assert.NotNull(apple);
        }

        [Export]
        public class Apple { }
    }
}
