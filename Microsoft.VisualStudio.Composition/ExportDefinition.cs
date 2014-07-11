namespace Microsoft.VisualStudio.Composition
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Validation;

    [DebuggerDisplay("{ContractName,nq}")]
    public partial class ExportDefinition : IEquatable<ExportDefinition>
    {
        public override bool Equals(object obj)
        {
            return this.Equals(obj as ExportDefinition);
        }

        public override int GetHashCode()
        {
            return this.ContractName.GetHashCode();
        }

        public bool Equals(ExportDefinition other)
        {
            return this.ContractName == other.ContractName
                && this.Metadata.EqualsByValue(other.Metadata);
        }

        partial void Validate()
        {
            Requires.NotNullOrEmpty(this.ContractName, "ContractName");
            Requires.NotNull(this.Metadata, "Metadata");
        }
    }
}
