using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace RuniOS.Resource
{
    public sealed class AssetRegistryList : IList<AssetRegistry>, IReadOnlyList<AssetRegistry>
    {
        readonly List<AssetRegistry> internalList = new();
        
        readonly Dictionary<Type, AssetRegistry> internalListOfType = new();
        readonly Dictionary<Type, AssetRegistry> internalListOfHandle = new();
        readonly Dictionary<Type, AssetRegistry> internalListOfScope = new();
        
        public AssetRegistry this[int index]
        {
            get => internalList[index];
            set => internalList[index] = value;
        }

        public int Count => internalList.Count;
        bool ICollection<AssetRegistry>.IsReadOnly => ((ICollection<AssetRegistry>)internalList).IsReadOnly;
        
        public void Add(AssetRegistry item)
        {
            if (!internalListOfType.ContainsKey(item.GetType()))
            {
                internalList.Add(item);

                internalListOfType.Add(item.GetType(), item);
                internalListOfHandle.Add(item.handleType, item);
                internalListOfScope.Add(item.scopeType, item);
            }
        }
        
        public void Insert(int index, AssetRegistry item)
        {
            if (!internalListOfType.ContainsKey(item.GetType()))
            {
                internalList.Insert(index, item);

                internalListOfType.Add(item.GetType(), item);
                internalListOfHandle.Add(item.handleType, item);
                internalListOfScope.Add(item.scopeType, item);
            }
        }

        public bool Remove(AssetRegistry item)
        {
            internalListOfType.Remove(item.GetType());
            internalListOfHandle.Remove(item.handleType);
            internalListOfScope.Remove(item.scopeType);
            
            return internalList.Remove(item);
        }

        public bool RemoveOfType(Type type)
        {
            if (!FindOfType(type, out AssetRegistry? item))
                return false;

            internalListOfType.Remove(type);
            internalListOfHandle.Remove(item.handleType);
            internalListOfScope.Remove(item.scopeType);
            
            return internalList.Remove(item);
        }
        
        public void RemoveAt(int index)
        {
            AssetRegistry item = internalList[index];
            
            internalListOfType.Remove(item.GetType());
            internalListOfHandle.Remove(item.handleType);
            internalListOfScope.Remove(item.scopeType);
            
            internalList.RemoveAt(index);
        }

        public void Clear()
        {
            internalListOfType.Clear();
            internalListOfHandle.Clear();
            internalListOfScope.Clear();
            
            internalList.Clear();
        }

        public bool Contains(AssetRegistry item) => internalList.Contains(item);
        public int IndexOf(AssetRegistry item) => internalList.IndexOf(item);

        public void CopyTo(AssetRegistry[] array, int arrayIndex) => internalList.CopyTo(array, arrayIndex);
        
        public bool FindOfType<T>([NotNullWhen(true)] out AssetRegistry? value) where T : AssetRegistry => FindOfType(typeof(T), out value);
        public bool FindOfType(Type type, [NotNullWhen(true)] out AssetRegistry? value) => internalListOfType.TryGetValue(type, out value);
        
        public bool FindOfHandle<T>([NotNullWhen(true)] out AssetRegistry? value) where T : AssetHandle => FindOfHandle(typeof(T), out value);
        public bool FindOfHandle(Type handle, [NotNullWhen(true)] out AssetRegistry? value) => internalListOfHandle.TryGetValue(handle, out value);
        
        public bool FindOfScope<T>([NotNullWhen(true)] out AssetRegistry? value) where T : AssetScope => FindOfScope(typeof(T), out value);
        public bool FindOfScope(Type scope, [NotNullWhen(true)] out AssetRegistry? value) => internalListOfScope.TryGetValue(scope, out value);

        public IEnumerator<AssetRegistry> GetEnumerator() => internalList.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}