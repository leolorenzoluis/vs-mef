namespace Microsoft.VisualStudio.Composition
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Validation;

    /// <summary>
    /// Static factory methods for creating .NET Lazy{T} instances.
    /// </summary>
    internal static class LazyServices
    {
        private static readonly MethodInfo createStronglyTypedLazyOfTM = typeof(LazyServices).GetMethod("CreateStronglyTypedLazyOfTM", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo createStronglyTypedLazyOfT = typeof(LazyServices).GetMethod("CreateStronglyTypedLazyOfT", BindingFlags.NonPublic | BindingFlags.Static);

        internal static readonly Type DefaultMetadataViewType = typeof(IDictionary<string, object>);
        internal static readonly Type DefaultExportedValueType = typeof(object);

        /// <summary>
        /// Gets a value indicating whether a type is a Lazy`1 or Lazy`2 type.
        /// </summary>
        /// <param name="type">The type to be tested.</param>
        /// <returns><c>true</c> if <paramref name="type"/> is some Lazy type.</returns>
        internal static bool IsAnyLazyType(this Type type)
        {
            if (type.IsGenericType)
            {
                Type genericTypeDefinition = type.GetGenericTypeDefinition();
                if (genericTypeDefinition == typeof(Lazy<>) || genericTypeDefinition == typeof(Lazy<,>))
                {
                    return true;
                }
            }

            return false;
        }

        internal static Lazy<T> FromValue<T>(T value)
            where T : class
        {
            return new Lazy<T>(DelegateServices.FromValue(value), LazyThreadSafetyMode.PublicationOnly);
        }

        /// <summary>
        /// Creates a factory that takes a Func{object} and object-typed metadata
        /// and returns a strongly-typed Lazy{T, TMetadata} instance.
        /// </summary>
        /// <param name="exportType">The type of values created by the Func{object} value factories. Null is interpreted to be <c>typeof(object)</c>.</param>
        /// <param name="metadataViewType">The type of metadata passed to the lazy factory. Null is interpreted to be <c>typeof(IDictionary{string, object})</c>.</param>
        /// <returns>A function that takes a Func{object} value factory and metadata, and produces a Lazy{T, TMetadata} instance.</returns>
        internal static Func<Func<object>, object, object> CreateStronglyTypedLazyFactory(Type exportType, Type metadataViewType)
        {
            MethodInfo genericMethod;
            if (metadataViewType != null)
            {
                genericMethod = createStronglyTypedLazyOfTM.MakeGenericMethod(exportType ?? DefaultExportedValueType, metadataViewType);
            }
            else
            {
                genericMethod = createStronglyTypedLazyOfT.MakeGenericMethod(exportType ?? DefaultExportedValueType);
            }

            return (Func<Func<object>, object, object>)Delegate.CreateDelegate(typeof(Func<Func<object>, object, object>), genericMethod);
        }

        internal static Func<T> AsFunc<T>(this Lazy<T> lazy)
        {
            Requires.NotNull(lazy, "lazy");

            if (typeof(T) == typeof(object))
            {
                // This is a very specific syntax that leverages the C# compiler
                // to emit very efficient code for constructing a delegate that
                // uses the "Target" property to store the first parameter to
                // the method.
                // We have to avoid all generic type arguments in order to qualify for the fast path.
                var lazyOfObject = (Lazy<object>)(object)lazy;
                return (Func<T>)(object)new Func<object>(lazyOfObject.GetLazyValue);
            }
            else
            {
                // Just create a lambda, and the closure it requires.
                // Don't use the fancy extension method syntax to avoid the closure
                // Because our generic type argument T will cause the JIT to 
                // take a very slow path for delegate creation so although we'll avoid the closure,
                // we'll pay for it dearly in performance in other ways.
                // The slow path involves COMDelegate::DelegateConstruct, and is used
                // because the CLR has to make sure T isn't collected while the delegate lives
                // if T is defined in a collectible assembly.
                return () => lazy.Value;
            }
        }

        private static object GetLazyValue(this Lazy<object> lazy)
        {
            return lazy.Value;
        }

        private static Lazy<T> CreateStronglyTypedLazyOfT<T>(Func<object> funcOfObject, object metadata)
        {
            Requires.NotNull(funcOfObject, "funcOfObject");

            return new Lazy<T>(funcOfObject.As<T>());
        }

        private static Lazy<T, TMetadata> CreateStronglyTypedLazyOfTM<T, TMetadata>(Func<object> funcOfObject, object metadata)
        {
            Requires.NotNull(funcOfObject, "funcOfObject");
            Requires.NotNullAllowStructs(metadata, "metadata");

            return new Lazy<T, TMetadata>(funcOfObject.As<T>(), (TMetadata)metadata);
        }
    }
}
