#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace RuniOS.Collections.Handlers
{
    [CustomCollectionHandler(typeof(Queue<>))]
    public class GenericQueueHandler : IEnumerableHandler
    {
        public GenericQueueHandler(Type resolvedTargetType, IEnumerable targetCollection) : base(resolvedTargetType, targetCollection) { }

        public override bool isReadOnly => false;
        
        public override bool isFixedSize => false;

        MethodInfo? clearMethod;
        MethodInfo? addMethod;
        readonly object[] addMethodParameters = new object[1];
        
        public override void UpdateSourceCollections()
        {
            clearMethod ??= AccessUtility.DeclaredMethod(resolvedTargetType, nameof(Queue<int>.Clear));
            addMethod ??= AccessUtility.DeclaredMethod(resolvedTargetType, nameof(Queue<int>.Enqueue));
            
            clearMethod?.Invoke(targetCollection, null);
            for (int i = 0; i < synchronizedList.Count; i++)
            {
                addMethodParameters[0] = synchronizedList[i];
                addMethod?.Invoke(targetCollection, addMethodParameters);
            }
        }
    }
}
