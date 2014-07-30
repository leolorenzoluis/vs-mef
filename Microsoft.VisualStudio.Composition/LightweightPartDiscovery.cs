namespace Microsoft.VisualStudio.Composition
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Metadata;
    using System.Reflection.PortableExecutable;
    using System.Text;
    using System.Threading.Tasks;
    using Validation;

    public abstract class LightweightPartDiscovery
    {
        public abstract ComposablePartDefinition CreatePart(MetadataReader metadataReader, TypeDefinition typeDefinition);

        /// <summary>
        /// Creates MEF parts from an assembly.
        /// </summary>
        /// <param name="assemblyPath">The full path to the assembly.</param>
        /// <returns>A collection of parts.</returns>
        public IReadOnlyCollection<ComposablePartDefinition> CreateParts(Stream assemblyStream)
        {
            Requires.NotNull(assemblyStream, "assemblyStream");

            using (var peReader = new PEReader(assemblyStream))
            {
                var metadataReader = peReader.GetMetadataReader();
                var typeNames = from typeHandle in metadataReader.TypeDefinitions
                                let typeDef = metadataReader.GetTypeDefinition(typeHandle)
                                let part = this.CreatePart(metadataReader, typeDef)
                                let ns = metadataReader.GetString(typeDef.Namespace)
                                let name = metadataReader.GetString(typeDef.Name)
                                select ns + "." + name;
                string[] typeNamesArray = typeNames.ToArray();

                return ImmutableHashSet<ComposablePartDefinition>.Empty;
            }
        }
    }
}
