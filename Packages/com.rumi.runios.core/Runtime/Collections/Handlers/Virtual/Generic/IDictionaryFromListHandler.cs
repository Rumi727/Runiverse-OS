#nullable enable
using RuniOS.Collections.Handlers.Entrys;
using System.Collections;
using System.Reflection;

namespace RuniOS.Collections.Handlers.Virtual.Generic
{
    [CustomCollectionHandler(typeof(IDictionary<,>))]
    public class IDictionaryFromListHandler : VirtualListHandler
    {
        public IDictionaryFromListHandler(IEnumerable targetCollection) : base(targetCollection)
        {
            targetCollection.GetType().IsAssignableToGenericDefinition(typeof(IDictionary<,>), out resolvedTargetType!);
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
        readonly object?[] addInfoParameters = new object?[2];

        public override void SynchronizeCollections()
        {
            if (IsDuplicate())
                return;

            base.SynchronizeCollections();
        }

        protected override void UpdateSourceCollections()
        {
            if (IsDuplicate())
                return;

            clearInfo ??= AccessUtility.DeclaredMethod(resolvedTargetCollectionType, nameof(ICollection<int>.Clear));
            addInfo ??= AccessUtility.DeclaredMethod(resolvedTargetType, nameof(IDictionary<int, int>.Add));

            clearInfo!.Invoke(targetCollection, null);
            for (int i = 0; i < synchronizedList.Count; i++)
            {
                KeyValuePair<object?, object?> entry = EntryHandler.FindEntry(synchronizedList[i]);

                addInfoParameters[0] = entry.Key;
                addInfoParameters[1] = entry.Value;

                addInfo!.Invoke(targetCollection, addInfoParameters);
            }
        }

        readonly HashSet<object?> tempKeyTable = new();
        bool IsDuplicate()
        {
            tempKeyTable.Clear();

            // ReSharper disable once LoopCanBeConvertedToQuery
            for (int i = 0; i < synchronizedList.Count; i++)
            {
                KeyValuePair<object?, object?> entry = EntryHandler.FindEntry(synchronizedList[i]);
                if (!tempKeyTable.Add(entry.Key))
                    return true;

            }

            return false;
        }
    }
}