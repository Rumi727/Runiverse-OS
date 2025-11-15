#nullable enable
using System.Collections;
using System.Reflection;

namespace RuniOS.Collections.Handlers.Virtual.Generic
{
    [CustomCollectionHandler(typeof(Stack<>))]
    public class StackHandler : VirtualListHandler
    {
        public StackHandler(IEnumerable targetCollection) : base(targetCollection) => targetCollection.GetType().IsAssignableToGenericDefinition(typeof(Stack<>), out resolvedTargetType!);

        readonly Type resolvedTargetType;
        
        public override bool isReadOnly => false;
        
        public override bool isFixedSize => false;

        MethodInfo? clearInfo;
        MethodInfo? addInfo;
        readonly object[] addInfoParameters = new object[1];
        
        public override void UpdateSourceCollections()
        {
            clearInfo ??= AccessUtility.DeclaredMethod(resolvedTargetType, nameof(Stack<int>.Clear));
            addInfo ??= AccessUtility.DeclaredMethod(resolvedTargetType, nameof(Stack<int>.Push));
            
            clearInfo!.Invoke(targetCollection, null);
            for (int i = synchronizedList.Count - 1; i >= 0; i--)
            {
                addInfoParameters[0] = synchronizedList[i];
                addInfo!.Invoke(targetCollection, addInfoParameters);
            }
        }
    }
}