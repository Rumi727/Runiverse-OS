#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace RuniOS.Collections.Handlers
{
    [CustomCollectionHandler(typeof(IList<>))]
    public class GenericIListHandler : CollectionHandler
    {
        public GenericIListHandler(Type resolvedTargetType, IEnumerable targetCollection) : base(resolvedTargetType, targetCollection) { }

        public override object? this[int index]
        {
            get
            {
                indexerInfo ??= AccessUtility.DeclaredIndexer(resolvedTargetType);
                indexInfoIndex[0] = index;
                
                return indexerInfo!.GetValue(targetCollection, indexInfoIndex);
            }
            set
            {
                indexerInfo ??= AccessUtility.DeclaredIndexer(resolvedTargetType);
                indexInfoIndex[0] = index;
                
                indexerInfo!.SetValue(targetCollection, value, indexInfoIndex);
            }
        }
        readonly object?[] indexInfoIndex = new object?[1];
        PropertyInfo? indexerInfo;

        public override int count
        {
            get
            {
                if (countInfo == null)
                {
                    resolvedTargetType.IsAssignableToGenericDefinition(typeof(ICollection<>), out Type? type);
                    countInfo = AccessUtility.DeclaredProperty(type!, nameof(ICollection<int>.Count));
                }

                return (int)countInfo!.GetValue(targetCollection);
            }
        }
        PropertyInfo? countInfo;

        public override bool isReadOnly
        {
            get
            {
                if (isReadOnlyInfo == null)
                {
                    resolvedTargetType.IsAssignableToGenericDefinition(typeof(ICollection<>), out Type? type);
                    isReadOnlyInfo = AccessUtility.DeclaredProperty(type!, nameof(ICollection<int>.IsReadOnly));
                }

                return (bool)isReadOnlyInfo!.GetValue(targetCollection);
            }
        }
        PropertyInfo? isReadOnlyInfo;

        public override bool isFixedSize => isReadOnly;

        public override int Add(object? value)
        {
            int result = count;
            if (addInfo == null)
            {
                resolvedTargetType.IsAssignableToGenericDefinition(typeof(ICollection<>), out Type? type);
                addInfo = AccessUtility.DeclaredMethod(type!, nameof(ICollection<int>.Add));
            }
            addInfoParameters[0] = value;
            
            addInfo!.Invoke(targetCollection, addInfoParameters);
            return result;
        }
        readonly object?[] addInfoParameters = new object?[1];
        MethodInfo? addInfo;

        public override void Insert(int index, object value)
        {
            insertInfo ??= AccessUtility.DeclaredMethod(resolvedTargetType, nameof(IList<int>.Insert));
            insertInfoParameters[0] = index;
            insertInfoParameters[1] = value;
            
            insertInfo!.Invoke(targetCollection, insertInfoParameters);
        }
        readonly object?[] insertInfoParameters = new object?[2];
        MethodInfo? insertInfo;

        public override void Remove(object value)
        {
            if (removeInfo == null)
            {
                resolvedTargetType.IsAssignableToGenericDefinition(typeof(ICollection<>), out Type? type);
                removeInfo = AccessUtility.DeclaredMethod(type!, nameof(ICollection<int>.Remove));
            }
            removeInfoParameters[0] = value;
            
            removeInfo!.Invoke(targetCollection, removeInfoParameters);
        }
        readonly object?[] removeInfoParameters = new object?[1];
        MethodInfo? removeInfo;

        public override void RemoveAt(int index)
        {
            removeAtInfo ??= AccessUtility.DeclaredMethod(resolvedTargetType, nameof(IList<int>.RemoveAt));
            removeAtInfoParameters[0] = index;
            
            removeAtInfo!.Invoke(targetCollection, removeAtInfoParameters);
        }
        readonly object?[] removeAtInfoParameters = new object?[1];
        MethodInfo? removeAtInfo;

        public override void Clear()
        {
            if (clearInfo == null)
            {
                resolvedTargetType.IsAssignableToGenericDefinition(typeof(ICollection<>), out Type? type);
                clearInfo = AccessUtility.DeclaredMethod(type!, nameof(ICollection<int>.Clear));
            }
            clearInfo!.Invoke(targetCollection, null);
        }
        MethodInfo? clearInfo;

        public override bool Contains(object? value)
        {
            if (containsInfo == null)
            {
                resolvedTargetType.IsAssignableToGenericDefinition(typeof(ICollection<>), out Type? type);
                containsInfo = AccessUtility.DeclaredMethod(type!, nameof(ICollection<int>.Contains));
            }
            containsInfoParameters[0] = value;
            
            return (bool)containsInfo!.Invoke(targetCollection, containsInfoParameters);
        }
        readonly object?[] containsInfoParameters = new object?[1];
        MethodInfo? containsInfo;
        
        public override int IndexOf(object value)
        {
            indexOfInfo ??= AccessUtility.DeclaredMethod(resolvedTargetType, nameof(IList<int>.IndexOf));
            indexOfInfoParameters[0] = value;
            
            return (int)indexOfInfo!.Invoke(targetCollection, indexOfInfoParameters);
        }
        readonly object?[] indexOfInfoParameters = new object?[1];
        MethodInfo? indexOfInfo;

        public override void CopyTo(Array array, int index)
        {
            if (copyToInfo == null)
            {
                resolvedTargetType.IsAssignableToGenericDefinition(typeof(ICollection<>), out Type? type);
                copyToInfo = AccessUtility.DeclaredMethod(type!, nameof(ICollection<int>.CopyTo));
            }
            
            copyToInfoParameters[0] = array;
            copyToInfoParameters[1] = index;
            
            copyToInfo!.Invoke(targetCollection, copyToInfoParameters);
        }
        readonly object?[] copyToInfoParameters = new object?[2];
        MethodInfo? copyToInfo;
    }
}
