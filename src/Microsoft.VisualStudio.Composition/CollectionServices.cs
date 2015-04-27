﻿// This file originated from System.ComponentModel.Composition.dll

namespace Microsoft.VisualStudio.Composition
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;

    internal static partial class CollectionServices
    {
        private static readonly ConstructorInfo collectionOfObjectCtor = typeof(CollectionOfObject<>).GetConstructors()[0];
        private static readonly Dictionary<Type, Func<object, ICollection<object>>> cachedCollectionWrapperFactories = new Dictionary<Type, Func<object, ICollection<object>>>();

        internal static ICollection<object> GetCollectionWrapper(Type itemType, object collectionObject)
        {
            Requires.NotNull(itemType, "itemType");
            Requires.NotNull(collectionObject, "collectionObject");

            var underlyingItemType = itemType.UnderlyingSystemType;

            if (underlyingItemType == typeof(object))
            {
                return (ICollection<object>)collectionObject;
            }

            // Most common .Net collections implement IList as well so for those
            // cases we can optimize the wrapping instead of using reflection to create
            // a generic type.
            if (typeof(IList).GetTypeInfo().IsAssignableFrom(collectionObject.GetType().GetTypeInfo()))
            {
                return new CollectionOfObjectList((IList)collectionObject);
            }

            Func<object, ICollection<object>> factory;
            lock (cachedCollectionWrapperFactories)
            {
                cachedCollectionWrapperFactories.TryGetValue(underlyingItemType, out factory);
            }

            if (factory == null)
            {
                Type collectionType = typeof(CollectionOfObject<>).MakeGenericType(underlyingItemType);
                var ctor = (ConstructorInfo)MethodBase.GetMethodFromHandle(collectionOfObjectCtor.MethodHandle, collectionType.TypeHandle);

                factory = collection =>
                {
                    using (var args = ArrayRental<object>.Get(1))
                    {
                        args.Value[0] = collection;
                        return (ICollection<object>)ctor.Invoke(args.Value);
                    }
                };

                lock (cachedCollectionWrapperFactories)
                {
                    cachedCollectionWrapperFactories[underlyingItemType] = factory;
                }
            }

            return factory(collectionObject);
        }

        private class CollectionOfObjectList : ICollection<object>
        {
            private readonly IList _list;

            public CollectionOfObjectList(IList list)
            {
                this._list = list;
            }

            public void Add(object item)
            {
                this._list.Add(item);
            }

            public void Clear()
            {
                this._list.Clear();
            }

            public bool Contains(object item)
            {
                throw Assumes.NotReachable();
            }

            public void CopyTo(object[] array, int arrayIndex)
            {
                throw Assumes.NotReachable();
            }

            public int Count
            {
                get { throw Assumes.NotReachable(); }
            }

            public bool IsReadOnly
            {
                get { return this._list.IsReadOnly; }
            }

            public bool Remove(object item)
            {
                throw Assumes.NotReachable();
            }

            public IEnumerator<object> GetEnumerator()
            {
                throw Assumes.NotReachable();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw Assumes.NotReachable();
            }
        }

        private class CollectionOfObject<T> : ICollection<object>
        {
            private readonly ICollection<T> _collectionOfT;

            public CollectionOfObject(object collectionOfT)
            {
                this._collectionOfT = (ICollection<T>)collectionOfT;
            }

            public void Add(object item)
            {
                this._collectionOfT.Add((T)item);
            }

            public void Clear()
            {
                this._collectionOfT.Clear();
            }

            public bool Contains(object item)
            {
                throw Assumes.NotReachable();
            }

            public void CopyTo(object[] array, int arrayIndex)
            {
                throw Assumes.NotReachable();
            }

            public int Count
            {
                get { throw Assumes.NotReachable(); }
            }

            public bool IsReadOnly
            {
                get { return this._collectionOfT.IsReadOnly; }
            }

            public bool Remove(object item)
            {
                throw Assumes.NotReachable();
            }

            public IEnumerator<object> GetEnumerator()
            {
                throw Assumes.NotReachable();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw Assumes.NotReachable();
            }
        }
    }
}
