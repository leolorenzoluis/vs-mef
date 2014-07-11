namespace Microsoft.VisualStudio.Composition.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;

    public class ExportDefinitionTests
    {
        [Fact]
        public void Create_InvalidInputs()
        {
            Assert.Throws<ArgumentException>(() => ExportDefinition.Create(string.Empty, ImmutableDictionary.Create<string, object>()));
            Assert.Throws<ArgumentNullException>(() => ExportDefinition.Create(null, ImmutableDictionary.Create<string, object>()));
            Assert.Throws<ArgumentNullException>(() => ExportDefinition.Create("something", null));
        }

        [Fact]
        public void Create_ValidInputs()
        {
            var ed = ExportDefinition.Create("something", ImmutableDictionary<string, object>.Empty.Add("a", "b"));
            Assert.Equal("something", ed.ContractName);
            Assert.Equal("b", ed.Metadata["a"]);
        }

        [Fact]
        public void With()
        {
            var ed = ExportDefinition.Create("something", ImmutableDictionary<string, object>.Empty.Add("a", "b"));
            var newContractName = ed.With(contractName: "foo");
            Assert.Equal("foo", newContractName.ContractName);
            Assert.Equal("b", newContractName.Metadata["a"]);

            var newMetadata = ed.With(metadata: ed.Metadata.SetItem("a", "c"));
            Assert.Equal("something", newMetadata.ContractName);
            Assert.Equal("c", newMetadata.Metadata["a"]);
        }
    }
}
