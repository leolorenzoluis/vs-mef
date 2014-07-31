﻿namespace Microsoft.VisualStudio.Composition.Reflection
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using Validation;

    public class TypeRef : IEquatable<TypeRef>, IEquatable<Type>
    {
        public TypeRef() { }

        public TypeRef(AssemblyName assemblyName, int metadataToken, int genericTypeParameterCount, ImmutableArray<TypeRef> genericTypeArguments)
            : this()
        {
            Requires.NotNull(assemblyName, "assemblyName");

            this.AssemblyName = assemblyName;
            this.MetadataToken = metadataToken;
            this.GenericTypeParameterCount = genericTypeParameterCount;
            this.GenericTypeArguments = genericTypeArguments;
        }

        public TypeRef(Type type)
            : this()
        {
            this.AssemblyName = type.Assembly.GetName();
            this.MetadataToken = type.MetadataToken;
            this.GenericTypeParameterCount = type.GetTypeInfo().GenericTypeParameters.Length;
            this.GenericTypeArguments = type.GenericTypeArguments != null && type.GenericTypeArguments.Length > 0
                ? type.GenericTypeArguments.Select(t => new TypeRef(t)).ToImmutableArray()
                : ImmutableArray<TypeRef>.Empty;
        }

        public AssemblyName AssemblyName { get; private set; }

        public int MetadataToken { get; private set; }

        public int GenericTypeParameterCount { get; private set; }

        public ImmutableArray<TypeRef> GenericTypeArguments { get; private set; }

        public bool IsEmpty
        {
            get { return this.AssemblyName == null; }
        }

        public bool IsGenericTypeDefinition
        {
            get { return this.GenericTypeParameterCount > 0 && this.GenericTypeArguments.Length == 0; }
        }

        public TypeRef MakeGenericType(ImmutableArray<TypeRef> genericTypeArguments)
        {
            Requires.Argument(!genericTypeArguments.IsDefault, "genericTypeArguments", "Not initialized.");
            Verify.Operation(this.IsGenericTypeDefinition, "This is not a generic type definition.");
            return new Reflection.TypeRef(this.AssemblyName, this.MetadataToken, this.GenericTypeParameterCount, genericTypeArguments);
        }

        public override int GetHashCode()
        {
            return this.MetadataToken;
        }

        public override bool Equals(object obj)
        {
            return obj is TypeRef && this.Equals((TypeRef)obj);
        }

        public bool Equals(TypeRef other)
        {
            return AssemblyNameEqual(this.AssemblyName, other.AssemblyName)
                && this.MetadataToken == other.MetadataToken
                && this.GenericTypeArguments.EqualsByValue(other.GenericTypeArguments);
        }

        public bool Equals(Type other)
        {
            return this.Equals(new TypeRef(other));
        }

        private static bool AssemblyNameEqual(AssemblyName first, AssemblyName second)
        {
            if (first == null ^ second == null)
            {
                return false;
            }

            if (first == null)
            {
                return true;
            }

            return first.FullName == second.FullName;
        }
    }
}
