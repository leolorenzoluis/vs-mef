﻿namespace Microsoft.VisualStudio.Composition.Tests
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Composition;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;
    using MefV1 = System.ComponentModel.Composition;

    [Trait("Metadata", "")]
    public class ExportMetadataTests
    {
        [MefFact(CompositionEngines.V2Compat | CompositionEngines.V1Compat, typeof(ImportingPartWithMetadataDictionary), typeof(PartWithExportMetadata))]
        public void ImportWithMetadataDictionary(IContainer container)
        {
            var importingPart = container.GetExportedValue<ImportingPartWithMetadataDictionary>();
            Assert.NotNull(importingPart.ImportingProperty);
            Assert.Equal("b", importingPart.ImportingProperty.Metadata["a"]);
            Assert.False(importingPart.ImportingProperty.IsValueCreated);
            Assert.IsType<PartWithExportMetadata>(importingPart.ImportingProperty.Value);
        }

        [MefFact(CompositionEngines.V2 | CompositionEngines.V1, typeof(ImportingPartWithMetadataDictionary), typeof(PartWithExportMetadata))]
        [Trait("Efficiency", "InstanceReuse")]
        public void MetadataDictionaryInstanceSharedAcrossImports(IContainer container)
        {
            var importingPart1 = container.GetExportedValue<ImportingPartWithMetadataDictionary>();
            var importingPart2 = container.GetExportedValue<ImportingPartWithMetadataDictionary>();
            Assert.NotSame(importingPart1, importingPart2); // non-shared part is crucial to the integrity of this test.

            // Ensure that the dictionary instances are shared.
            Assert.Same(importingPart1.ImportingProperty.Metadata, importingPart2.ImportingProperty.Metadata);
        }

        [MefFact(CompositionEngines.V1Compat, typeof(ImportingPartWithMetadataDictionary), typeof(PartWithExportMetadata))]
        public void MetadataDictionaryInstanceIsImmutable(IContainer container)
        {
            var importingPart = container.GetExportedValue<ImportingPartWithMetadataDictionary>();
            Assert.Throws<NotSupportedException>(() => importingPart.ImportingProperty.Metadata["foo"] = 5);
        }

        [MefFact(CompositionEngines.V1Compat | CompositionEngines.V2Compat, typeof(ImportManyPartWithMetadataDictionary), typeof(PartWithExportMetadata))]
        public void ImportManyWithMetadataDictionary(IContainer container)
        {
            var importingPart = container.GetExportedValue<ImportManyPartWithMetadataDictionary>();
            Assert.NotNull(importingPart.ImportingProperty);
            Assert.Equal(1, importingPart.ImportingProperty.Count());
            Assert.Equal("b", importingPart.ImportingProperty.Single().Metadata["a"]);
            Assert.False(importingPart.ImportingProperty.Single().IsValueCreated);
            Assert.IsType<PartWithExportMetadata>(importingPart.ImportingProperty.Single().Value);
        }

        [MefFact(CompositionEngines.V2Compat | CompositionEngines.V1Compat, typeof(ImportingPartWithMetadataClass), typeof(PartWithExportMetadata))]
        public void ImportWithMetadataClass(IContainer container)
        {
            var importingPart = container.GetExportedValue<ImportingPartWithMetadataClass>();
            Assert.NotNull(importingPart.ImportingProperty);
            Assert.Equal("b", importingPart.ImportingProperty.Metadata.a);
            Assert.False(importingPart.ImportingProperty.IsValueCreated);
            Assert.IsType<PartWithExportMetadata>(importingPart.ImportingProperty.Value);
        }

        [MefFact(CompositionEngines.V3EmulatingV2WithNonPublic | CompositionEngines.V1Compat, typeof(ImportingPartWithNonPublicMetadataClass), typeof(PartWithExportMetadata))]
        public void ImportWithNonPublicMetadataClass(IContainer container)
        {
            var importingPart = container.GetExportedValue<ImportingPartWithNonPublicMetadataClass>();
            Assert.NotNull(importingPart.ImportingProperty);
            Assert.Equal("b", importingPart.ImportingProperty.Metadata.a);
            Assert.False(importingPart.ImportingProperty.IsValueCreated);
            Assert.IsType<PartWithExportMetadata>(importingPart.ImportingProperty.Value);
        }

        [MefFact(CompositionEngines.V2Compat | CompositionEngines.V1Compat, typeof(ImportManyPartWithMetadataClass), typeof(PartWithExportMetadata))]
        public void ImportManyWithMetadataClass(IContainer container)
        {
            var importingPart = container.GetExportedValue<ImportManyPartWithMetadataClass>();
            Assert.NotNull(importingPart.ImportingProperty);
            Assert.Equal(1, importingPart.ImportingProperty.Count());
            Assert.Equal("b", importingPart.ImportingProperty.Single().Metadata.a);
            Assert.False(importingPart.ImportingProperty.Single().IsValueCreated);
            Assert.IsType<PartWithExportMetadata>(importingPart.ImportingProperty.Single().Value);
        }

        [MefFact(CompositionEngines.V1Compat, typeof(ImportingPartWithMetadataInterface), typeof(PartWithExportMetadata))]
        public void ImportWithMetadataInterface(IContainer container)
        {
            var importingPart = container.GetExportedValue<ImportingPartWithMetadataInterface>();
            Assert.NotNull(importingPart.ImportingProperty);
            Assert.Equal("b", importingPart.ImportingProperty.Metadata.a);
            Assert.False(importingPart.ImportingProperty.IsValueCreated);
            Assert.IsType<PartWithExportMetadata>(importingPart.ImportingProperty.Value);
        }

        [MefFact(CompositionEngines.V1Compat, typeof(ImportManyPartWithMetadataInterface), typeof(PartWithExportMetadata))]
        public void ImportManyWithMetadataInterface(IContainer container)
        {
            var importingPart = container.GetExportedValue<ImportManyPartWithMetadataInterface>();
            Assert.NotNull(importingPart.ImportingProperty);
            Assert.Equal(1, importingPart.ImportingProperty.Count());
            Assert.Equal("b", importingPart.ImportingProperty.Single().Metadata.a);
            Assert.False(importingPart.ImportingProperty.Single().IsValueCreated);
            Assert.IsType<PartWithExportMetadata>(importingPart.ImportingProperty.Single().Value);
        }

        [MefFact(CompositionEngines.Unspecified, typeof(ImportingPartWithMetadataInterface), typeof(PartWithExportMetadata))]
        [Trait("Efficiency", "InstanceReuse")]
        public void MetadataViewInterfaceInstanceSharedAcrossImports(IContainer container)
        {
            var importingPart1 = container.GetExportedValue<ImportingPartWithMetadataInterface>();
            var importingPart2 = container.GetExportedValue<ImportingPartWithMetadataInterface>();
            Assert.NotSame(importingPart1, importingPart2); // non-shared part is crucial to the integrity of this test.

            // Ensure that the interface instances are shared.
            Assert.Same(importingPart1.ImportingProperty.Metadata, importingPart2.ImportingProperty.Metadata);
        }

        [MefFact(CompositionEngines.V1Compat | CompositionEngines.V2Compat, typeof(ExportWithEnumMetadata), typeof(PartThatImportsEnumMetadata))]
        public void MetadataEnumValue(IContainer container)
        {
            var importer = container.GetExportedValue<PartThatImportsEnumMetadata>();
            object metadataValue = importer.ImportingProperty.Metadata["SomeName"];
            Assert.Equal(MetadataEnum.Value2, metadataValue);
            Assert.IsType<MetadataEnum>(metadataValue);
        }

        [MefFact(CompositionEngines.V1Compat, typeof(ExportWithNonPublicEnumMetadata))]
        public void NonPublicMetadataEnumValue(IContainer container)
        {
            var part = container.GetExport<ExportWithNonPublicEnumMetadata, IDictionary<string, object>>();
            object metadataValue = part.Metadata["SomeName"];
            Assert.Equal(MetadataEnumNonPublic.Value2, metadataValue);
            Assert.IsType<MetadataEnumNonPublic>(metadataValue);
        }

        [MefFact(CompositionEngines.V1Compat | CompositionEngines.V2Compat, typeof(ExportWithTypeMetadata), typeof(PartThatImportsTypeMetadata))]
        public void MetadataTypeValue(IContainer container)
        {
            var importer = container.GetExportedValue<PartThatImportsTypeMetadata>();
            object metadataValue = importer.ImportingProperty.Metadata["SomeName"];
            Assert.Equal(typeof(int), metadataValue);
        }

        [MefFact(CompositionEngines.V1Compat | CompositionEngines.V2Compat, typeof(ExportWithCharMetadata), typeof(PartThatImportsCharMetadata))]
        public void MetadataCharValue(IContainer container)
        {
            var importer = container.GetExportedValue<PartThatImportsCharMetadata>();
            object metadataValue = importer.ImportingProperty.Metadata["SomeName"];
            Assert.Equal('a', metadataValue);
            Assert.IsType<char>(metadataValue);
            Assert.Equal('\'', importer.ImportingProperty.Metadata["Apostrophe"]);
        }

        [MefFact(CompositionEngines.V1Compat | CompositionEngines.V2Compat, typeof(ExportWithBoolMetadata), typeof(PartThatImportsBoolMetadata))]
        public void MetadataBoolValue(IContainer container)
        {
            var importer = container.GetExportedValue<PartThatImportsBoolMetadata>();
            object metadataValue = importer.ImportingProperty.Metadata["SomeName"];
            Assert.Equal(true, metadataValue);
            Assert.IsType<bool>(metadataValue);
        }

        [MefFact(CompositionEngines.V1Compat, typeof(PartWithExportMetadata), typeof(ImportingPartWithMetadataDictionary))]
        public void ExportTypeIdentityMetadataIsPresent(IContainer container)
        {
            var part = container.GetExportedValue<ImportingPartWithMetadataDictionary>();

            object metadataValue;
            Assert.True(part.ImportingProperty.Metadata.TryGetValue("ExportTypeIdentity", out metadataValue));
            Assert.IsType(typeof(string), metadataValue);
            Assert.Equal(typeof(PartWithExportMetadata).FullName, metadataValue);
        }

        #region Metaview filtering tests

        [MefFact(CompositionEngines.V1Compat, typeof(ImportingPartOfObjectWithMetadataInterface), typeof(PartWithExportMetadataA), typeof(PartWithExportMetadataB))]
        public void ImportWithMetadataViewAsFilter(IContainer container)
        {
            var importer = container.GetExportedValue<ImportingPartOfObjectWithMetadataInterface>();

            // metadata "a" is mandatory per the interface, whereas "B" is optional.
            Assert.IsType<PartWithExportMetadataA>(importer.ImportingProperty.Value);
            Assert.Equal(null, importer.ImportingProperty.Metadata.SomeStringEnum);
            Assert.Equal(4, importer.ImportingProperty.Metadata.SomeInt);
        }

        [MefFact(CompositionEngines.V1Compat, typeof(ImportingPartOfObjectWithMetadataInterface), typeof(PartWithExportMetadataSomeStringArray))]
        public void ImportWithMetadataViewAsFilterAndMetadatumWithStringArrayValue(IContainer container)
        {
            var importer = container.GetExportedValue<ImportingPartOfObjectWithMetadataInterface>();

            Assert.IsType<PartWithExportMetadataSomeStringArray>(importer.ImportingProperty.Value);
            Assert.Equal(new string[] { "alpha", "beta" }, importer.ImportingProperty.Metadata.SomeStringArray);
        }

        [MefFact(CompositionEngines.V1Compat, typeof(ImportingPartOfObjectWithMetadataInterface), typeof(PartWithExportMetadataSomeStringArray))]
        public void ImportWithMetadataViewAsFilterOfObjectArrayAndMetadatumWithStringArrayValue(IContainer container)
        {
            var importer = container.GetExportedValue<ImportingPartOfObjectWithMetadataInterface>();

            Assert.IsType<PartWithExportMetadataSomeStringArray>(importer.ImportingProperty.Value);
            Assert.Equal(new object[] { "alpha", "beta" }, importer.ImportingProperty.Metadata.SomeObjectArrayOfStrings);
        }

        [MefFact(CompositionEngines.V1Compat, typeof(ImportManyPartOfObjectWithMetadataInterface), typeof(PartWithExportMetadataA), typeof(PartWithExportMetadataB), typeof(PartWithExportMetadataAB))]
        public void ImportManyWithMetadataViewAsFilter(IContainer container)
        {
            var importer = container.GetExportedValue<ImportManyPartOfObjectWithMetadataInterface>();

            // metadata "a" is mandatory per the interface, whereas "B" is optional.
            Assert.Equal(2, importer.ImportingProperty.Count());
            Assert.Equal(1, importer.ImportingProperty.Select(v => v.Value).OfType<PartWithExportMetadataA>().Count());
            Assert.Equal(1, importer.ImportingProperty.Select(v => v.Value).OfType<PartWithExportMetadataAB>().Count());
        }

        [MefV1.Export]
        public class ImportingPartOfObjectWithMetadataInterface
        {
            [MefV1.Import("ExportWithMetadata")]
            public Lazy<object, IMetadata> ImportingProperty { get; set; }
        }

        [MefV1.Export]
        public class ImportManyPartOfObjectWithMetadataInterface
        {
            [MefV1.ImportMany("ExportWithMetadata")]
            public IEnumerable<Lazy<object, IMetadata>> ImportingProperty { get; set; }
        }

        [MefV1.Export("ExportWithMetadata", typeof(object))]
        [MefV1.ExportMetadata("a", "b")]
        public class PartWithExportMetadataA { }

        [MefV1.Export("ExportWithMetadata", typeof(object))]
        [MefV1.ExportMetadata("B", "c")]
        public class PartWithExportMetadataB { }

        [MefV1.Export("ExportWithMetadata", typeof(object))]
        [MefV1.ExportMetadata("a", "b")]
        [MefV1.ExportMetadata("B", "c")]
        public class PartWithExportMetadataAB { }

        [MefV1.Export("ExportWithMetadata", typeof(object))]
        [MefV1.ExportMetadata("a", "b")]
        [MefV1.ExportMetadata("SomeStringArray", new string[] { "alpha", "beta" })]
        [MefV1.ExportMetadata("SomeObjectArrayOfStrings", new object[] { "alpha", "beta" })]
        public class PartWithExportMetadataSomeStringArray { }

        #endregion

        #region MetadataViewWithMultipleValues test

        [MefFact(CompositionEngines.V1Compat, typeof(ExportWithMultipleMetadata), typeof(ImportOfMultipleMetadata))]
        public void MetadataViewWithMultipleValues(IContainer container)
        {
            var part = container.GetExportedValue<ImportOfMultipleMetadata>();
            IEnumerable<Type> metadataValue = part.ImportingProperty.Metadata.Name;
            Assert.Equal(2, metadataValue.Count());
            Assert.Contains(typeof(int), metadataValue);
            Assert.Contains(typeof(string), metadataValue);
        }

        public interface IMetadataViewForMultipleValues
        {
            IEnumerable<Type> Name { get; }
        }

        [MefV1.Export]
        [MefV1.ExportMetadata("Name", typeof(int), IsMultiple = true)]
        [MefV1.ExportMetadata("Name", typeof(string), IsMultiple = true)]
        public class ExportWithMultipleMetadata { }

        [MefV1.Export]
        public class ImportOfMultipleMetadata
        {
            [MefV1.Import]
            public Lazy<ExportWithMultipleMetadata, IMetadataViewForMultipleValues> ImportingProperty { get; set; }
        }

        #endregion

        #region MultipleExportMetadataTypedAppropriately test

        [MefFact(CompositionEngines.V1Compat | CompositionEngines.V3EmulatingV2, typeof(PartImportingExportsWithMultipleTypedMetadata), typeof(PartWithMultipleTypedMetadata))]
        public void MultipleExportMetadataTypedAppropriately(IContainer container)
        {
            var part = container.GetExportedValue<PartImportingExportsWithMultipleTypedMetadata>();
            object metadataValue = part.ImportingProperty.Metadata["Name"];
            Assert.IsType<string[]>(metadataValue);
            var array = (string[])metadataValue;
            Assert.Contains("hi", array);
            Assert.Equal(2, array.Where(v => v == null).Count());
        }

        [Export, ExportMetadata("Name", null), ExportMetadata("Name", "hi"), ExportMetadata("Name", null)]
        [MefV1.Export, MefV1.ExportMetadata("Name", null, IsMultiple = true), MefV1.ExportMetadata("Name", "hi", IsMultiple = true), MefV1.ExportMetadata("Name", null, IsMultiple = true)]
        public class PartWithMultipleTypedMetadata { }

        [Export]
        [MefV1.Export]
        public class PartImportingExportsWithMultipleTypedMetadata
        {
            [Import]
            [MefV1.Import]
            public Lazy<PartWithMultipleTypedMetadata, IDictionary<string, object>> ImportingProperty { get; set; }
        }

        #endregion

        #region MultipleExportMetadataWithOnlyNullTyped test

        [MefFact(CompositionEngines.V1Compat, typeof(PartImportingExportsWithMultipleNullMetadata), typeof(PartWithMultipleNullMetadata))]
        public void MultipleExportMetadataWithOnlyNullTyped(IContainer container)
        {
            var part = container.GetExportedValue<PartImportingExportsWithMultipleNullMetadata>();
            object dictionaryValue = part.ImportingPropertyWithDictionary.Metadata["Names"];
            IEnumerable<string> interfaceValue = part.ImportingPropertyWithInterface.Metadata.Names;
            Assert.Null(interfaceValue);
            Assert.Equal(new object[] { null, null }, dictionaryValue);
        }

        [Export, ExportMetadata("Names", null), ExportMetadata("Names", null)]
        [MefV1.Export, MefV1.ExportMetadata("Names", null, IsMultiple = true), MefV1.ExportMetadata("Names", null, IsMultiple = true)]
        public class PartWithMultipleNullMetadata { }

        public interface INamedMetadata
        {
            [DefaultValue(null)]
            IEnumerable<string> Names { get; }
        }

        [Export]
        [MefV1.Export]
        public class PartImportingExportsWithMultipleNullMetadata
        {
            [Import]
            [MefV1.Import]
            public Lazy<PartWithMultipleNullMetadata, INamedMetadata> ImportingPropertyWithInterface { get; set; }

            [Import]
            [MefV1.Import]
            public Lazy<PartWithMultipleNullMetadata, IDictionary<string, object>> ImportingPropertyWithDictionary { get; set; }
        }

        #endregion

        #region GetExports (plural) tests

        [MefFact(CompositionEngines.V1)]
        [Trait("Container.GetExport", "Plural")]
        public void GetNamedExportsTMetadataEmpty(IContainer container)
        {
            IEnumerable<ILazy<object, IDictionary<string, object>>> result =
                container.GetExports<object, IDictionary<string, object>>("NoOneExportsThis");
            Assert.Equal(0, result.Count());
        }

        [MefFact(CompositionEngines.V1, typeof(PartWithExportMetadataA), typeof(PartWithExportMetadataB), typeof(PartWithExportMetadataAB))]
        [Trait("Container.GetExport", "Plural")]
        public void GetNamedExportsTMetadata(IContainer container)
        {
            IEnumerable<ILazy<object, IDictionary<string, object>>> result =
                container.GetExports<object, IDictionary<string, object>>("ExportWithMetadata");
            Assert.Equal(3, result.Count());
            var a = result.Single(e => !e.Metadata.ContainsKey("B") && e.Metadata.ContainsKey("a") && "b".Equals(e.Metadata["a"]));
            var b = result.Single(e => !e.Metadata.ContainsKey("a") && e.Metadata.ContainsKey("B") && "c".Equals(e.Metadata["B"]));
            var ab = result.Single(e => e.Metadata.ContainsKey("a") && "b".Equals(e.Metadata["a"]) && e.Metadata.ContainsKey("B") && "c".Equals(e.Metadata["B"]));
            Assert.IsType<PartWithExportMetadataA>(a.Value);
            Assert.IsType<PartWithExportMetadataB>(b.Value);
            Assert.IsType<PartWithExportMetadataAB>(ab.Value);
        }

        [MefFact(CompositionEngines.V1Compat, typeof(FooExport1), typeof(FooExport2))]
        [Trait("Container.GetExport", "Plural")]
        public void GetExportsTMetadataEmpty(IContainer container)
        {
            IEnumerable<ILazy<object, IDictionary<string, object>>> result =
                container.GetExports<object, IDictionary<string, object>>();
            Assert.Equal(0, result.Count());
        }

        [MefFact(CompositionEngines.V1, typeof(FooExport1), typeof(FooExport2))]
        [Trait("Container.GetExport", "Plural")]
        public void GetExportsTMetadata(IContainer container)
        {
            IEnumerable<ILazy<IFoo, IMetadataBase>> result =
                container.GetExports<IFoo, IMetadataBase>();
            Assert.Equal(2, result.Count());

            var a = result.Single(e => e.Metadata.a == "1");
            var b = result.Single(e => e.Metadata.a == "2");

            Assert.IsType<FooExport1>(a.Value);
            Assert.IsType<FooExport2>(b.Value);
        }

        [MefFact(CompositionEngines.V1Compat, typeof(FooExport1), typeof(FooExport2))]
        [Trait("Container.GetExport", "Plural")]
        public void GetExportsDictionaryMetadata(IContainer container)
        {
            IEnumerable<ILazy<IFoo, IDictionary<string, object>>> result =
                container.GetExports<IFoo, IDictionary<string, object>>();
            Assert.Equal(2, result.Count());

            var a = result.Single(e => (string)e.Metadata["a"] == "1");
            var b = result.Single(e => (string)e.Metadata["a"] == "2");

            Assert.IsType<FooExport1>(a.Value);
            Assert.IsType<FooExport2>(b.Value);
        }

        public interface IFoo { }

        [MefV1.Export(typeof(IFoo))]
        [MefV1.ExportMetadata("a", "1")]
        public class FooExport1 : IFoo { }

        [MefV1.Export(typeof(IFoo))]
        [MefV1.ExportMetadata("a", "2")]
        public class FooExport2 : IFoo { }

        #endregion

        #region Exported Method ExportTypeIdentity test

        [MefFact(CompositionEngines.V1Compat, typeof(MethodExportingPart), typeof(PartThatImportsMethod))]
        public void ExportedMethodHasExportTypeIdentityMetadata(IContainer container)
        {
            var part = container.GetExportedValue<PartThatImportsMethod>();
            object metadataValue;
            Assert.True(part.ImportedMethod.Metadata.TryGetValue("ExportTypeIdentity", out metadataValue));
            Assert.IsType(typeof(string), metadataValue);
            Assert.Equal("System.Single(System.Int32,System.Int32)", metadataValue);
        }

        public class MethodExportingPart
        {
            [MefV1.Export]
            public float Add(int a, int b)
            {
                return a + b;
            }
        }

        [MefV1.Export]
        public class PartThatImportsMethod
        {
            [MefV1.Import]
            public Lazy<Func<int, int, float>, IDictionary<string, object>> ImportedMethod { get; set; }
        }

        #endregion

        #region Extreme values tests

        [MefFact(CompositionEngines.V1Compat | CompositionEngines.V2Compat, typeof(PartImportingExtremeValues), typeof(PartWithExtremeValues))]
        public void ExportMetadataExtremeValues(IContainer container)
        {
            var part = container.GetExportedValue<PartImportingExtremeValues>();
            Assert.Equal(double.MaxValue, part.ImportingProperty.Metadata["doubleMaxValue"]);
            Assert.Equal(double.MinValue, part.ImportingProperty.Metadata["doubleMinValue"]);
            Assert.Equal(float.MaxValue, part.ImportingProperty.Metadata["floatMaxValue"]);
            Assert.Equal(float.MinValue, part.ImportingProperty.Metadata["floatMinValue"]);
        }

        [MefV1.Export, Export]
        [MefV1.ExportMetadata("doubleMaxValue", double.MaxValue), ExportMetadata("doubleMaxValue", double.MaxValue)]
        [MefV1.ExportMetadata("doubleMinValue", double.MinValue), ExportMetadata("doubleMinValue", double.MinValue)]
        [MefV1.ExportMetadata("floatMaxValue", float.MaxValue), ExportMetadata("floatMaxValue", float.MaxValue)]
        [MefV1.ExportMetadata("floatMinValue", float.MinValue), ExportMetadata("floatMinValue", float.MinValue)]
        public class PartWithExtremeValues { }

        [MefV1.Export, Export]
        public class PartImportingExtremeValues
        {
            [MefV1.Import, Import]
            public Lazy<PartWithExtremeValues, IDictionary<string, object>> ImportingProperty { get; set; }
        }

        #endregion

        [MefV1.Export, MefV1.PartCreationPolicy(MefV1.CreationPolicy.NonShared)]
        [MefV1.ExportMetadata("a", "b")]
        [Export]
        [ExportMetadata("a", "b")]
        public class PartWithExportMetadata { }

        [MefV1.Export, MefV1.PartCreationPolicy(MefV1.CreationPolicy.NonShared)]
        [Export]
        public class ImportingPartWithMetadataDictionary
        {
            [Import, MefV1.Import]
            public Lazy<PartWithExportMetadata, IDictionary<string, object>> ImportingProperty { get; set; }
        }

        [MefV1.Export, MefV1.PartCreationPolicy(MefV1.CreationPolicy.NonShared)]
        [Export]
        public class ImportingPartWithMetadataInterface
        {
            [Import, MefV1.Import]
            public Lazy<PartWithExportMetadata, IMetadata> ImportingProperty { get; set; }
        }

        [MefV1.Export, MefV1.PartCreationPolicy(MefV1.CreationPolicy.NonShared)]
        [Export]
        public class ImportingPartWithMetadataClass
        {
            [Import, MefV1.Import]
            public Lazy<PartWithExportMetadata, MetadataClass> ImportingProperty { get; set; }
        }

        [MefV1.Export, MefV1.PartCreationPolicy(MefV1.CreationPolicy.NonShared)]
        [Export]
        public class ImportingPartWithNonPublicMetadataClass
        {
            [Import, MefV1.Import]
            internal Lazy<PartWithExportMetadata, NonPublicMetadataClass> ImportingProperty { get; set; }
        }

        [MefV1.Export, MefV1.PartCreationPolicy(MefV1.CreationPolicy.NonShared)]
        [Export]
        public class ImportManyPartWithMetadataDictionary
        {
            [ImportMany, MefV1.ImportMany]
            public IEnumerable<Lazy<PartWithExportMetadata, IDictionary<string, object>>> ImportingProperty { get; set; }
        }

        [MefV1.Export, MefV1.PartCreationPolicy(MefV1.CreationPolicy.NonShared)]
        [Export]
        public class ImportManyPartWithMetadataInterface
        {
            [ImportMany, MefV1.ImportMany]
            public IEnumerable<Lazy<PartWithExportMetadata, IMetadata>> ImportingProperty { get; set; }
        }

        [MefV1.Export, MefV1.PartCreationPolicy(MefV1.CreationPolicy.NonShared)]
        [Export]
        public class ImportManyPartWithMetadataClass
        {
            [ImportMany, MefV1.ImportMany]
            public IEnumerable<Lazy<PartWithExportMetadata, MetadataClass>> ImportingProperty { get; set; }
        }

        public interface IMetadataBase
        {
            string a { get; }
        }

        public interface IMetadata : IMetadataBase
        {
            [DefaultValue("someDefault")]
            string B { get; }

            [DefaultValue(null)]
            IEnumerable<string> SomeStringEnum { get; }

            [DefaultValue(null)]
            string[] SomeStringArray { get; }

            [DefaultValue(null)]
            object[] SomeObjectArrayOfStrings { get; }

            [DefaultValue(4)]
            int SomeInt { get; }
        }

        public class MetadataClass
        {
            // Only MEFv1 requires this constructor -- MEFv2 doesn't need it.
            public MetadataClass(IDictionary<string, object> data)
            {
                object value;
                if (data.TryGetValue("a", out value))
                {
                    this.a = (string)value;
                }
            }

            public string a { get; set; }
        }

        internal class NonPublicMetadataClass : MetadataClass
        {
            public NonPublicMetadataClass(IDictionary<string, object> data)
                : base(data)
            {
            }
        }

        public enum MetadataEnum
        {
            Value1,
            Value2
        }

        internal enum MetadataEnumNonPublic
        {
            Value1,
            Value2,
        }

        [Export]
        [MefV1.Export]
        [ExportMetadata("SomeName", MetadataEnum.Value2)]
        [MefV1.ExportMetadata("SomeName", MetadataEnum.Value2)]
        public class ExportWithEnumMetadata { }

        [Export]
        [MefV1.Export]
        [ExportMetadata("SomeName", MetadataEnumNonPublic.Value2)]
        [MefV1.ExportMetadata("SomeName", MetadataEnumNonPublic.Value2)]
        public class ExportWithNonPublicEnumMetadata { }

        [Export]
        [MefV1.Export]
        public class PartThatImportsEnumMetadata
        {
            [Import]
            [MefV1.Import]
            public Lazy<ExportWithEnumMetadata, IDictionary<string, object>> ImportingProperty { get; set; }
        }

        [Export]
        [MefV1.Export]
        [ExportMetadata("SomeName", typeof(int))]
        [MefV1.ExportMetadata("SomeName", typeof(int))]
        public class ExportWithTypeMetadata { }

        [Export]
        [MefV1.Export]
        public class PartThatImportsTypeMetadata
        {
            [Import]
            [MefV1.Import]
            public Lazy<ExportWithTypeMetadata, IDictionary<string, object>> ImportingProperty { get; set; }
        }

        [Export]
        [MefV1.Export]
        [ExportMetadata("SomeName", 'a')]
        [ExportMetadata("Apostrophe", '\'')]
        [MefV1.ExportMetadata("SomeName", 'a')]
        [MefV1.ExportMetadata("Apostrophe", '\'')]
        public class ExportWithCharMetadata { }

        [Export]
        [MefV1.Export]
        public class PartThatImportsCharMetadata
        {
            [Import]
            [MefV1.Import]
            public Lazy<ExportWithCharMetadata, IDictionary<string, object>> ImportingProperty { get; set; }
        }

        [Export]
        [MefV1.Export]
        [ExportMetadata("SomeName", true)]
        [MefV1.ExportMetadata("SomeName", true)]
        public class ExportWithBoolMetadata { }

        [Export]
        [MefV1.Export]
        public class PartThatImportsBoolMetadata
        {
            [Import]
            [MefV1.Import]
            public Lazy<ExportWithBoolMetadata, IDictionary<string, object>> ImportingProperty { get; set; }
        }
    }
}
