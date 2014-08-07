namespace Microsoft.VisualStudio.Composition
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Validation;

    /// <summary>
    /// Provides some resilience to a MEF composition against rogue parts that have inappropriate
    /// imports for their intended sharing boundary, to help in recognizing and rejecting the
    /// parts that play a root cause role in a graph error.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class ProhibitedSharingBoundaryAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProhibitedSharingBoundaryAttribute"/> class.
        /// </summary>
        /// <param name="sharingBoundary">
        /// A sharing boundary that should never be transitively applied to this MEF part.
        /// </param>
        public ProhibitedSharingBoundaryAttribute(string sharingBoundary)
        {
            Requires.NotNull(sharingBoundary, "sharingBoundary");
            this.ProhibitedSharingBoundary = sharingBoundary;
        }

        /// <summary>
        /// Gets the sharing boundary that should never be applied to the applicable part.
        /// </summary>
        public string ProhibitedSharingBoundary { get; private set; }
    }
}
