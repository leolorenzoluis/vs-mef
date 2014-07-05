namespace Microsoft.VisualStudio.Composition
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Validation;

    public abstract class LightweightPartDiscovery
    {
        public abstract ComposablePartDefinition CreatePart(INamedTypeSymbol typeSymbol);

        /// <summary>
        /// Reflects over an assembly and produces MEF parts for every applicable type.
        /// </summary>
        /// <param name="assembly">The assembly to search for MEF parts.</param>
        /// <returns>A set of generated parts.</returns>
        public IReadOnlyCollection<ComposablePartDefinition> CreateParts(IAssemblySymbol assembly)
        {
            Requires.NotNull(assembly, "assembly");

            var parts = ImmutableHashSet.CreateBuilder<ComposablePartDefinition>();
            var typeFinder = new TypeFinder(this, parts);
            assembly.Accept(typeFinder);

            return parts.ToImmutable();
        }

        /// <summary>
        /// Creates MEF parts from an assembly.
        /// </summary>
        /// <param name="assemblyPath">The full path to the assembly.</param>
        /// <param name="referenceProvider">Usually MetadataFileReferenceProvider.Default, available from Roslyn in the Desktop platform.</param>
        /// <returns>A collection of parts.</returns>
        public IReadOnlyCollection<ComposablePartDefinition> CreateParts(string assemblyPath, MetadataReferenceProvider referenceProvider)
        {
            Requires.NotNullOrEmpty(assemblyPath, "assemblyPath");

            var compilation = CSharpCompilation.Create("LMR")
                .AddReferences(referenceProvider.GetReference(assemblyPath, MetadataReferenceProperties.Assembly));
            var anything = compilation.GlobalNamespace.GetMembers().FirstOrDefault();
            if (anything != null)
            {
                return this.CreateParts(anything.ContainingAssembly);
            }
            else
            {
                return ImmutableHashSet<ComposablePartDefinition>.Empty;
            }
        }

        private class TypeFinder : SymbolVisitor
        {
            private readonly LightweightPartDiscovery partDiscovery;
            private readonly ISet<ComposablePartDefinition> parts;

            internal TypeFinder(LightweightPartDiscovery partDiscovery, ISet<ComposablePartDefinition> parts)
            {
                Requires.NotNull(partDiscovery, "partDiscovery");
                Requires.NotNull(parts, "parts");

                this.partDiscovery = partDiscovery;
                this.parts = parts;
            }

            public override void VisitNamedType(INamedTypeSymbol symbol)
            {
                var part = this.partDiscovery.CreatePart(symbol);
                if (part != null)
                {
                    this.parts.Add(part);
                }
            }

            public override void VisitNamespace(INamespaceSymbol symbol)
            {
                foreach (var member in symbol.GetMembers())
                {
                    this.Visit(member);
                }
            }
        }
    }
}
