#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace RuniOS.Resource
{
    public sealed class ReadOnlyAssetRegistryList : IReadOnlyList<AssetRegistry>
    {
        public ReadOnlyAssetRegistryList(AssetRegistryList list) => internalList = list; 
        
        readonly AssetRegistryList internalList;
        
        public AssetRegistry this[int index] => internalList[index];
        
        public int Count => internalList.Count;
        
        public bool Contains(AssetRegistry item) => internalList.Contains(item);
        public int IndexOf(AssetRegistry item) => internalList.IndexOf(item);

        public void CopyTo(AssetRegistry[] array, int arrayIndex) => internalList.CopyTo(array, arrayIndex);

        public bool FindOfType<T>([NotNullWhen(true)] out T? value) where T : AssetRegistry
        {
            bool result = internalList.FindOfType<T>(out AssetRegistry? registry);
            if (result)
                value = (T?)registry;

            value = null;
            return result;
        }
        public bool FindOfType(Type type, [NotNullWhen(true)] out AssetRegistry? value) => internalList.FindOfType(type, out value);
        
        
        public bool FindOfHandle<T>([NotNullWhen(true)] out AssetRegistry? value) where T : AssetHandle => internalList.FindOfHandle<T>(out value);
        public bool FindOfHandle(Type handle, [NotNullWhen(true)] out AssetRegistry? value) => internalList.FindOfHandle(handle, out value);
        
        
        public bool FindOfScope<T>([NotNullWhen(true)] out AssetRegistry? value) where T : AssetScope => internalList.FindOfScope<T>(out value);
        public bool FindOfScope(Type scope, [NotNullWhen(true)] out AssetRegistry? value) => internalList.FindOfScope(scope, out value);

        public IEnumerator<AssetRegistry> GetEnumerator() => internalList.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}