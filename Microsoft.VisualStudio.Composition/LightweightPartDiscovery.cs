namespace Microsoft.VisualStudio.Composition
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Metadata;
    using System.Reflection.Metadata.Ecma335;
    using System.Reflection.PortableExecutable;
    using System.Text;
    using System.Threading.Tasks;
    using Validation;

    public abstract class LightweightPartDiscovery : IPartDiscovery
    {
        protected abstract ComposablePartDefinition CreatePart(MetadataReader metadataReader, TypeDefinition typeDefinition);

        /// <summary>
        /// Creates MEF parts from an assembly.
        /// </summary>
        /// <param name="assemblyPath">The full path to the assembly.</param>
        /// <returns>A collection of parts.</returns>
        public DiscoveredParts CreateParts(Stream assemblyStream)
        {
            Requires.NotNull(assemblyStream, "assemblyStream");

            var parts = new List<ComposablePartDefinition>();
            var exceptions = new List<Exception>();

            try
            {
                using (var peReader = new PEReader(assemblyStream))
                {
                    var metadataReader = peReader.GetMetadataReader();
                    var typeDefs = from typeHandle in metadataReader.TypeDefinitions
                                   let typeDef = metadataReader.GetTypeDefinition(typeHandle)
                                   select typeDef;
                    foreach (var td in typeDefs)
                    {
                        try
                        {
                            parts.Add(CreatePart(metadataReader, td));
                        }
                        catch (Exception ex)
                        {
                            exceptions.Add(ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }

            return new DiscoveredParts(parts, exceptions);
        }

        public DiscoveredParts CreateParts(IEnumerable<Type> types)
        {
            Requires.NotNull(types, "types");
            var assemblies = from type in types
                             group type by type.Assembly into assembly
                             select assembly;

            var parts = new List<ComposablePartDefinition>();
            var exceptions = new List<Exception>();
            foreach (var assembly in assemblies)
            {
                try
                {
                    using (Stream str = File.Open(assembly.Key.Location, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        using (var peReader = new PEReader(str))
                        {
                            try
                            {
                                var metadataReader = peReader.GetMetadataReader();
                                foreach (Type type in assembly)
                                {
                                    try
                                    {
                                        var definition = metadataReader.TypeDefinitions.First(td => type.MetadataToken == metadataReader.GetToken(td));
                                        var typeDefinition = metadataReader.GetTypeDefinition(definition);
                                        parts.Add(this.CreatePart(metadataReader, typeDefinition));
                                    }
                                    catch (Exception ex)
                                    {
                                        exceptions.Add(ex);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                exceptions.Add(ex);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            return new DiscoveredParts(parts, exceptions);
        }

        public DiscoveredParts CreateParts(IEnumerable<Assembly> assemblies)
        {
            DiscoveredParts toReturn = new DiscoveredParts(Enumerable.Empty<ComposablePartDefinition>(), Enumerable.Empty<Exception>());
            foreach (var assembly in assemblies)
            {
                var location = assembly.Location;
                using (Stream str = File.Open(location, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    toReturn.Merge(CreateParts(str));
                }
            }
            return toReturn;
        }

        public static IPartDiscovery Combine(params LightweightPartDiscovery[] discoveryMechanisms)
        {
            Requires.NotNull(discoveryMechanisms, "discoveryMechanisms");

            if (discoveryMechanisms.Length == 1)
            {
                return discoveryMechanisms[0];
            }

            return new CombinedLightweightPartDiscovery(discoveryMechanisms);
        }

        private class CombinedLightweightPartDiscovery : LightweightPartDiscovery
        {
            private readonly IReadOnlyList<LightweightPartDiscovery> discoveryMechanisms;

            internal CombinedLightweightPartDiscovery(IReadOnlyList<LightweightPartDiscovery> discoveryMechanisms)
            {
                Requires.NotNull(discoveryMechanisms, "discoveryMechanisms");
                this.discoveryMechanisms = discoveryMechanisms;
            }

            protected override ComposablePartDefinition CreatePart(MetadataReader metadataReader, TypeDefinition typeDefinition)
            {
                Requires.NotNull(metadataReader, "metadataReader");
            
                foreach (var discovery in this.discoveryMechanisms)
                {
                    var result = discovery.CreatePart(metadataReader, typeDefinition);
                    if (result != null)
                    {
                        return result;
                    }
                }

                return null;
            }
        }

    }
}