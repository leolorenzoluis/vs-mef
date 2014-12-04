namespace Microsoft.VisualStudio.Composition
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using MefV1 = System.ComponentModel.Composition;

    partial class NetFxAdapters
    {
        private class MefV1Catalog : MefV1.Primitives.ComposablePartCatalog
        {
            private readonly ComposableCatalog catalog;

            internal MefV1Catalog(ComposableCatalog catalog)
            {
                Requires.NotNull(catalog, "catalog");
                this.catalog = catalog;
            }
        }
    }
}
