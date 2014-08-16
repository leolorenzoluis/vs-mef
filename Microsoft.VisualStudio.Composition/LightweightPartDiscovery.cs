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
        public abstract ComposablePartDefinition CreatePart(MetadataReader metadataReader, TypeDefinition typeDefinition, 
            HashSet<string> knownExportTypes, string assemblyName, int metadataToken);

        /// <summary>
        /// Creates MEF parts from an assembly.
        /// </summary>
        /// <param name="assemblyPath">The full path to the assembly.</param>
        /// <returns>A collection of parts.</returns>
        public DiscoveredParts CreateParts(Stream assemblyStream)
        {
            Requires.NotNull(assemblyStream, "assemblyStream");

            var parts = new List<ComposablePartDefinition>();
            var exceptions = new List<PartDiscoveryException>();

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
                            var part = this.CreatePart(metadataReader, td, null, string.Empty, 0);
                            if (part != null)
                                parts.Add(part);
                        }
                        catch (Exception ex)
                        {
                            exceptions.Add(new PartDiscoveryException("Failed reading type definition", ex));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(new PartDiscoveryException("Failed getting types from assembly stream", ex));
            }

            return new DiscoveredParts(parts, exceptions);
        }
        private static string ByteArrayToHexString(byte[] bytes, int digits = 0)
        {
            if (digits == 0)
            {
                digits = bytes.Length * 2;
            }

            char[] c = new char[digits];
            byte b;
            for (int i = 0; i < digits / 2; i++)
            {
                b = ((byte)(bytes[i] >> 4));
                c[i * 2] = (char)(b > 9 ? b + 87 : b + 0x30);
                b = ((byte)(bytes[i] & 0xF));
                c[i * 2 + 1] = (char)(b > 9 ? b + 87 : b + 0x30);
            }

            return new string(c);
        }

        public DiscoveredParts CreateParts(IEnumerable<Type> types)
        {
            Requires.NotNull(types, "types");
            var assemblies = from type in types
                             group type by type.Assembly into assembly
                             select assembly;

            var parts = new List<ComposablePartDefinition>();
            var exceptions = new List<PartDiscoveryException>();
            var knownExportTypes = new HashSet<string>()
            {
                "System.ComponentModel.Composition.ExportAttribute",
                "System.ComponentModel.Composition.InheritedExportAttribute"
            };
            foreach (var assembly in assemblies)
            {
                try
                {
                    using (Stream str = File.Open(assembly.Key.Location, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        using (var peReader = new PEReader(str))
                        {
                            var metadataReader = peReader.GetMetadataReader();
                            foreach (Type type in assembly)
                            {
                                try
                                {
                                    var definition = metadataReader.TypeDefinitions.First(td => type.MetadataToken == metadataReader.GetToken(td));
                                    var typeDefinition = metadataReader.GetTypeDefinition(definition);
                                    var part = this.CreatePart(metadataReader, typeDefinition, knownExportTypes, assembly.Key.FullName, type.MetadataToken);
                                    if (part != null)
                                        parts.Add(part);
                                }
                                catch (Exception ex)
                                {
                                    exceptions.Add(new PartDiscoveryException("Failed to scan an individual type", ex)
                                    {
                                        AssemblyPath = assembly.Key.Location,
                                    });
                                }
                            }

                        }
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(new PartDiscoveryException("Failed enumerating types in assembly", ex)
                    {
                        AssemblyPath = assembly.Key.Location,
                    });
                }
            }

            return new DiscoveredParts(parts, exceptions);
        }

        public DiscoveredParts CreateParts(IEnumerable<Assembly> assemblies)
        {
            DiscoveredParts toReturn = new DiscoveredParts(Enumerable.Empty<ComposablePartDefinition>(), Enumerable.Empty<PartDiscoveryException>());
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

            public override ComposablePartDefinition CreatePart(MetadataReader metadataReader, TypeDefinition typeDefinition,
                HashSet<string> knownExportTypes, string assemblyName, int metadataToken)
            {
                Requires.NotNull(metadataReader, "metadataReader");
            
                foreach (var discovery in this.discoveryMechanisms)
                {
                    var result = discovery.CreatePart(metadataReader, typeDefinition, knownExportTypes, assemblyName, metadataToken);
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