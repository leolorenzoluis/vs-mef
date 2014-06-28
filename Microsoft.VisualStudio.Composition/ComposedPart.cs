﻿namespace Microsoft.VisualStudio.Composition
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using Validation;

    [DebuggerDisplay("{Definition.Type.Name}")]
    public class ComposedPart
    {
        public ComposedPart(ComposablePartDefinition definition, IReadOnlyDictionary<ImportDefinitionBinding, IReadOnlyList<ExportDefinitionBinding>> satisfyingExports, IImmutableSet<string> requiredSharingBoundaries)
        {
            Requires.NotNull(definition, "definition");
            Requires.NotNull(satisfyingExports, "satisfyingExports");
            Requires.NotNull(requiredSharingBoundaries, "requiredSharingBoundaries");

            // Make sure we have entries for every import.
            Requires.Argument(satisfyingExports.Count == definition.Imports.Count() && definition.Imports.All(d => satisfyingExports.ContainsKey(d)), "satisfyingExports", "There should be exactly one entry for every import.");
            Requires.Argument(satisfyingExports.All(kv => kv.Value != null), "satisfyingExports", "All values must be non-null.");

            this.Definition = definition;
            this.SatisfyingExports = satisfyingExports;
            this.RequiredSharingBoundaries = requiredSharingBoundaries;
        }

        public ComposablePartDefinition Definition { get; private set; }

        /// <summary>
        /// Gets a map of this part's imports, and the exports which satisfy them.
        /// </summary>
        public IReadOnlyDictionary<ImportDefinitionBinding, IReadOnlyList<ExportDefinitionBinding>> SatisfyingExports { get; private set; }

        /// <summary>
        /// Gets the set of sharing boundaries that this part must be instantiated within.
        /// </summary>
        public IImmutableSet<string> RequiredSharingBoundaries { get; private set; }

        public IEnumerable<KeyValuePair<ImportDefinitionBinding, IReadOnlyList<ExportDefinitionBinding>>> GetImportingConstructorImports()
        {
            foreach (var import in this.Definition.ImportingConstructor)
            {
                var key = this.SatisfyingExports.Keys.Single(k => k.ImportDefinition == import.ImportDefinition);
                yield return new KeyValuePair<ImportDefinitionBinding, IReadOnlyList<ExportDefinitionBinding>>(key, this.SatisfyingExports[key]);
            }
        }

        public void Validate()
        {
            var exceptions = ImmutableList.Create<Exception>();
            foreach (var pair in this.SatisfyingExports)
            {
                try
                {
                    var importDefinition = pair.Key.ImportDefinition;
                    switch (importDefinition.Cardinality)
                    {
                        case ImportCardinality.ExactlyOne:
                            if (pair.Value.Count != 1)
                            {
                                throw new CompositionFailedException(
                                    string.Format(
                                        CultureInfo.CurrentCulture,
                                        "Import of {0} expected 1 export but found {1}.",
                                        importDefinition.ContractName,
                                        pair.Value.Count));
                            }

                            break;
                        case ImportCardinality.OneOrZero:
                            if (pair.Value.Count > 1)
                            {
                                throw new CompositionFailedException(
                                    string.Format(
                                        CultureInfo.CurrentCulture,
                                        "Import of {0} expected 1 or 0 exports but found {1}.",
                                        importDefinition.ContractName,
                                        pair.Value.Count));
                            }

                            break;
                    }

                    foreach (var export in pair.Value)
                    {
                        if (!ReflectionHelpers.IsAssignableTo(pair.Key, export))
                        {
                            throw new CompositionFailedException(
                                string.Format(
                                    CultureInfo.CurrentCulture,
                                    "Exported type {4} on MEF part {0} is not assignable to {1}, as required by import found on {2}.{3}",
                                    ReflectionHelpers.GetTypeName(export.PartDefinition.Type, false, true, null, null),
                                    ReflectionHelpers.GetTypeName(pair.Key.ImportingSiteElementType, false, true, null, null),
                                    ReflectionHelpers.GetTypeName(this.Definition.Type, false, true, null, null),
                                    pair.Key.ImportingMember != null ? pair.Key.ImportingMember.Name : "ctor",
                                    ReflectionHelpers.GetTypeName(export.ExportedValueType, false, true, null, null)));
                        }
                    }
                }
                catch (CompositionFailedException ex)
                {
                    exceptions = exceptions.Add(new CompositionFailedException("Error validating MEF part: " + pair.Key.ComposablePartType.Name, ex));
                }
            }

            if (!exceptions.IsEmpty)
            {
                throw new CompositionFailedException("Error validating MEF part: " + this.Definition.Type.FullName, new AggregateException(exceptions));
            }
        }
    }
}
