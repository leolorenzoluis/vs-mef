﻿namespace Microsoft.VisualStudio.Composition
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Validation;

    /// <summary>
    /// A base class for ExportProviders that wish to intercept queries for exports
    /// to modify the query or the result.
    /// </summary>
    public abstract class DelegatingExportProvider : ExportProvider
    {
        /// <summary>
        /// The inner <see cref="ExportProvider"/> to which queries will be forwarded.
        /// </summary>
        private readonly ExportProvider inner;

        /// <summary>
        /// Initializes a new instance of the <see cref="DelegatingExportProvider"/>.
        /// </summary>
        /// <param name="inner">The instance to forward queries to.</param>
        protected DelegatingExportProvider(ExportProvider inner)
            : base(null, null)
        {
            Requires.NotNull(inner, "inner");
            this.inner = inner;
        }

        /// <summary>
        /// Forwards the exports query to the inner <see cref="ExportProvider"/>.
        /// </summary>
        /// <param name="importDefinition">A description of the exports desired.</param>
        /// <returns>The resulting exports.</returns>
        public override IEnumerable<Export> GetExports(ImportDefinition importDefinition)
        {
            return this.inner.GetExports(importDefinition);
        }

        /// <summary>
        /// Throws <see cref="NotImplementedException"/>.
        /// </summary>
        protected override IEnumerable<Export> GetExportsCore(ImportDefinition importDefinition)
        {
            // This should never be called, because our GetExports override calls the inner one instead,
            // which IS implemented.
            throw new NotImplementedException();
        }
    }
}
