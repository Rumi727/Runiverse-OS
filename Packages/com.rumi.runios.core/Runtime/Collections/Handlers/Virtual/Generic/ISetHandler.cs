#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace RuniOS.Collections.Handlers.Virtual.Generic
{
    [CustomCollectionHandler(typeof(ISet<>))]
    public class ISetHandler : VirtualListHandler
    {
        public ISetHandler(IEnumerable targetCollection) : base(targetCollection)
        {
            targetCollection.GetType().IsAssignableToGenericDefinition(typeof(ISet<>), out resolvedTargetType!);
            targetCollection.GetType().IsAssignableToGenericDefinition(typeof(ICollection<>), out resolvedTargetCollectionType!);
        }

        readonly Type resolvedTargetType;
        readonly Type resolvedTargetCollectionType;
        
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

        MethodInfo? clearInfo;
        MethodInfo? addInfo;
        readonly object[] addInfoParameters = new object[1];
        
        public override void UpdateSourceCollections()
        {
            clearInfo ??= AccessUtility.DeclaredMethod(resolvedTargetCollectionType, nameof(ICollection<int>.Clear));
            addInfo ??= AccessUtility.DeclaredMethod(resolvedTargetType, nameof(ISet<int>.Add));
                
            clearInfo!.Invoke(targetCollection, null);
            for (int i = 0; i < synchronizedList.Count; i++)
            {
                addInfoParameters[0] = synchronizedList[i];
                addInfo!.Invoke(targetCollection, addInfoParameters);
            }
        }
    }
}
