#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace RuniOS.Collections.Handlers.Virtual.Generic
{
    [CustomCollectionHandler(typeof(Queue<>))]
    public class QueueHandler : VirtualListHandler
    {
        public QueueHandler(IEnumerable targetCollection) : base(targetCollection) => targetCollection.GetType().IsAssignableToGenericDefinition(typeof(Queue<>), out resolvedTargetType!);

        readonly Type resolvedTargetType;

        public override bool isReadOnly => false;
        
        public override bool isFixedSize => false;

        MethodInfo? clearInfo;
        MethodInfo? addInfo;
        readonly object[] addInfoParameters = new object[1];
        
        public override void UpdateSourceCollections()
        {
            clearInfo ??= AccessUtility.DeclaredMethod(resolvedTargetType, nameof(Queue<int>.Clear));
            addInfo ??= AccessUtility.DeclaredMethod(resolvedTargetType, nameof(Queue<int>.Enqueue));
            
            clearInfo!.Invoke(targetCollection, null);
            for (int i = 0; i < synchronizedList.Count; i++)
            {
                addInfoParameters[0] = synchronizedList[i];
                addInfo!.Invoke(targetCollection, addInfoParameters);
            }
        }
    }
}
