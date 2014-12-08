namespace Microsoft.VisualStudio.Composition.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Sdk;
    using MefV1 = System.ComponentModel.Composition;

    public class MefV1WithV3CatalogTestCommand : FactCommand
    {
        private readonly ComposableCatalog catalog;
        private readonly CompositionEngines compositionVersions;
        private readonly bool runtime;

        public MefV1WithV3CatalogTestCommand(IMethodInfo method, ComposableCatalog catalog, CompositionEngines compositionVersions)
            : base(method)
        {
            this.catalog = catalog;
            this.compositionVersions = compositionVersions;
            this.DisplayName = "V3 catalog + V1 container";
        }

        public override MethodResult Execute(object testClass)
        {
            var v1Catalog = this.catalog.AsComposablePartCatalog();

            this.testMethod.Invoke(testClass, TestUtilities.CreateContainerV1(v1Catalog));

            return new PassedResult(this.testMethod, this.DisplayName);
        }
    }
}
