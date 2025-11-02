#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace RuniOS.Collections.Handlers
{
    [CustomCollectionHandler(typeof(Stack<>))]
    public class GenericStackHandler : IEnumerableHandler
    {
        public GenericStackHandler(Type resolvedTargetType, IEnumerable targetCollection) : base(resolvedTargetType, targetCollection) { }
        
        public override bool isReadOnly => false;
        
        public override bool isFixedSize => false;

        MethodInfo? clearMethod;
        MethodInfo? addMethod;
        readonly object[] addMethodParameters = new object[1];
        
        public override void UpdateSourceCollections()
        {
            clearMethod ??= AccessUtility.DeclaredMethod(resolvedTargetType, nameof(Stack<int>.Clear));
            addMethod ??= AccessUtility.DeclaredMethod(resolvedTargetType, nameof(Stack<int>.Push));
            
            clearMethod?.Invoke(targetCollection, null);
            for (int i = synchronizedList.Count - 1; i >= 0; i--)
            {
                addMethodParameters[0] = synchronizedList[i];
                addMethod?.Invoke(targetCollection, addMethodParameters);
            }
        }
    }
}
