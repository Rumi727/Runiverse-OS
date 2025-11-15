#nullable enable
using System.Collections;
using System.Reflection;

namespace RuniOS.Collections.Handlers.Generic
{
    [CustomCollectionHandler(typeof(IList<>))]
    public class IListHandler : ListHandlerBase
    {
        public IListHandler(IEnumerable targetCollection) : base(targetCollection)
        {
            targetCollection.GetType().IsAssignableToGenericDefinition(typeof(IList<>), out resolvedTargetType!);
            targetCollection.GetType().IsAssignableToGenericDefinition(typeof(ICollection<>), out resolvedTargetCollectionType!);
        }

        readonly Type resolvedTargetType;
        readonly Type resolvedTargetCollectionType;

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
                countInfo ??= AccessUtility.DeclaredProperty(resolvedTargetCollectionType, nameof(ICollection<int>.Count));
                return (int)countInfo!.GetValue(targetCollection);
            }
        }
        PropertyInfo? countInfo;

        public override bool isReadOnly
        {
            get
            {
                isReadOnlyInfo ??= AccessUtility.DeclaredProperty(resolvedTargetCollectionType, nameof(ICollection<int>.IsReadOnly));
                return (bool)isReadOnlyInfo!.GetValue(targetCollection);
            }
        }
        PropertyInfo? isReadOnlyInfo;

        public override bool isFixedSize => isReadOnly;

        public override int Add(object? value)
        {
            int result = count;
            addInfo ??= AccessUtility.DeclaredMethod(resolvedTargetCollectionType, nameof(ICollection<int>.Add));
            addInfoParameters[0] = value;
            
            addInfo!.Invoke(targetCollection, addInfoParameters);
            return result;
        }
        readonly object?[] addInfoParameters = new object?[1];
        MethodInfo? addInfo;

        public override void Insert(int index, object? value)
        {
            insertInfo ??= AccessUtility.DeclaredMethod(resolvedTargetType, nameof(IList<int>.Insert));
            insertInfoParameters[0] = index;
            insertInfoParameters[1] = value;
            
            insertInfo!.Invoke(targetCollection, insertInfoParameters);
        }
        readonly object?[] insertInfoParameters = new object?[2];
        MethodInfo? insertInfo;
        
        public override void Remove(object? value)
        {
            removeInfo ??= AccessUtility.DeclaredMethod(resolvedTargetCollectionType, nameof(ICollection<int>.Remove));
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
        readonly object[] removeAtInfoParameters = new object[1];
        MethodInfo? removeAtInfo;
        
        public override void Clear()
        {
            clearInfo ??= AccessUtility.DeclaredMethod(resolvedTargetCollectionType, nameof(ICollection<int>.Clear));
            clearInfo!.Invoke(targetCollection, null);
        }
        MethodInfo? clearInfo;

        public override bool Contains(object? value)
        {
            containsInfo ??= AccessUtility.DeclaredMethod(resolvedTargetCollectionType, nameof(ICollection<int>.Contains));
            containsInfoParameters[0] = value;
            
            return (bool)containsInfo!.Invoke(targetCollection, containsInfoParameters);
        }
        readonly object?[] containsInfoParameters = new object?[1];
        MethodInfo? containsInfo;
        
        public override int IndexOf(object? value)
        {
            indexOfInfo ??= AccessUtility.DeclaredMethod(resolvedTargetType, nameof(IList<int>.IndexOf));
            indexOfInfoParameters[0] = value;
            
            return (int)indexOfInfo!.Invoke(targetCollection, indexOfInfoParameters);
        }
        readonly object?[] indexOfInfoParameters = new object?[1];
        MethodInfo? indexOfInfo;
        
        public override void CopyTo(Array array, int index)
        {
            copyToInfo ??= AccessUtility.DeclaredMethod(resolvedTargetCollectionType, nameof(ICollection<int>.CopyTo));
            
            copyToInfoParameters[0] = array;
            copyToInfoParameters[1] = index;
            
            copyToInfo!.Invoke(targetCollection, copyToInfoParameters);
        }
        readonly object[] copyToInfoParameters = new object[2];
        MethodInfo? copyToInfo;
    }
}