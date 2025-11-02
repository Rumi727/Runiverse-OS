#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace RuniOS.Collections.Handlers
{
    [CustomCollectionHandler(typeof(ISet<>))]
    public class GenericISetHandler : IEnumerableHandler
    {
        public GenericISetHandler(Type resolvedTargetType, IEnumerable targetCollection) : base(resolvedTargetType, targetCollection) { }
        
        public override bool isReadOnly => false;
        
        public override bool isFixedSize => false;

        MethodInfo? clearMethod;
        MethodInfo? addMethod;
        readonly object[] addMethodParameters = new object[1];
        
        public override void UpdateSourceCollections()
        {
            if (clearMethod == null)
            {
                if (!resolvedTargetType.IsAssignableToGenericDefinition(typeof(ICollection<>), out Type? collectionType))
                    return;

                clearMethod = AccessUtility.DeclaredMethod(collectionType, nameof(ICollection<int>.Clear));
            }
            
            addMethod ??= AccessUtility.DeclaredMethod(resolvedTargetType, nameof(ISet<int>.Add));
                
            clearMethod?.Invoke(targetCollection, null);
            for (int i = 0; i < synchronizedList.Count; i++)
            {
                addMethodParameters[0] = synchronizedList[i];
                addMethod?.Invoke(targetCollection, addMethodParameters);
            }
        }
    }
}
