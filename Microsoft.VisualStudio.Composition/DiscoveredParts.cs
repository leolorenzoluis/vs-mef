﻿namespace Microsoft.VisualStudio.Composition
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Validation;

    public class DiscoveredParts
    {
        public static readonly DiscoveredParts Empty = new DiscoveredParts(ImmutableHashSet.Create<ComposablePartDefinition>(), ImmutableList.Create<Exception>());

        public DiscoveredParts(IEnumerable<ComposablePartDefinition> parts, IEnumerable<Exception> discoveryErrors)
        {
            Requires.NotNull(parts, "parts");
            Requires.NotNull(discoveryErrors, "discoveryErrors");

            this.Parts = ImmutableHashSet.CreateRange(parts);
            this.DiscoveryErrors = ImmutableList.CreateRange(discoveryErrors);
        }

        public ImmutableHashSet<ComposablePartDefinition> Parts { get; private set; }

        public ImmutableList<Exception> DiscoveryErrors { get; private set; }

        /// <summary>
        /// Returns the discovered parts if no errors occurred, otherwise throws an exception describing any discovery failures.
        /// </summary>
        /// <returns>This discovery result.</returns>
        /// <exception cref="CompositionFailedException">Thrown if <see cref="DiscoveryErrors"/> is non-empty.</exception>
        /// <remarks>
        /// This method returns <c>this</c> so that it may be used in a 'fluent API' expression.
        /// </remarks>
        public DiscoveredParts ThrowOnErrors()
        {
            if (this.DiscoveryErrors.Count == 0)
            {
                return this;
            }

            throw new CompositionFailedException("Errors occurred during discovery.", new AggregateException(this.DiscoveryErrors));
        }

        internal DiscoveredParts Merge(DiscoveredParts other)
        {
            Requires.NotNull(other, "other");

            if (other.Parts.Count == 0 && other.DiscoveryErrors.Count == 0)
            {
                return this;
            }

            return new DiscoveredParts(this.Parts.Union(other.Parts), this.DiscoveryErrors.AddRange(other.DiscoveryErrors));
        }
    }
}
