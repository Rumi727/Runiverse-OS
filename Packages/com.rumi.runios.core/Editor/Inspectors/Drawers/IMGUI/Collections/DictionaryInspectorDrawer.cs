#nullable enable
using RuniOS.Collections.Handlers.Entrys;
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;
using RuniOS.Linq;
using System.Collections;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Collections
{
    [CustomInspectorDrawer(typeof(IDictionary), true)]
    [CustomInspectorDrawer(typeof(IDictionary<,>), true)]
    public class DictionaryInspectorDrawer : ListInspectorDrawer
    {
        public DictionaryInspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }
        public DictionaryInspectorDrawer(IInspectableList inspectableList, Inspector? rootInspector = null) : base(inspectableList, rootInspector) { }

        object? defaultKey; 
        
        protected override bool IsFixedSize(InspectorFlags flags)
        {
            CheckInspectableDictionary();
            return inspectableDictionary.IsFixedSize;
        }

        protected override bool CanHeaderResize(Type? elementType, InspectorFlags flags) => false;

        protected override bool CanInsert(Type? elementType, InspectorFlags flags)
        {
            CheckInspectableDictionary();
            
            KeyValuePair<Type, Type>? elementTypePair = inspectableDictionary.inspectionElementType;
            if (elementTypePair == null)
                throw new NullReferenceException($"{nameof(elementTypePair)} is null");

            Type keyType = elementTypePair.Value.Key;
            Type valueType = elementTypePair.Value.Value;
            
            // 키 타입 인스턴스 생성 가능 여부 체크
            if (!keyType.CanGetDefaultValueNotNull(flags.HasFlagFast(InspectorFlags.NonPublic)))
                return false;
            
            defaultKey ??= keyType.GetDefaultValueNotNull(flags.HasFlagFast(InspectorFlags.NonPublic));

            // 키 중복 체크
            if (inspectableDictionary.Contains(defaultKey))
                return false;
            
            // 값 타입 Nullable 여부 체크
            if (inspectableDictionary.elementNullabilityInfo?.writeState == RuniNullabilityState.Nullable)
                return true;
            
            // 값 타입 인스턴스 생성 가능 여부 체크
            return valueType.CanGetDefaultValueNotNull(flags.HasFlagFast(InspectorFlags.NonPublic));
        }
        
        protected override object CreateElementItem(Type? elementType, InspectorFlags flags)
        {
            CheckInspectableDictionary();
            if (elementType == null)
                ExceptionUtility.ThrowIfArgumentNull(elementType, nameof(elementType));
            
            KeyValuePair<Type, Type>? elementTypePair = inspectableDictionary.inspectionElementType;
            if (elementTypePair == null)
                throw new NullReferenceException($"{nameof(elementTypePair)} is null");
            
            object key = elementTypePair.Value.Key.GetDefaultValueNotNull(flags.HasFlagFast(InspectorFlags.NonPublic));
            object? value;
            if (inspectableDictionary.elementNullabilityInfo?.writeState == RuniNullabilityState.Nullable)
                value = elementTypePair.Value.Value.GetDefaultValue(flags.HasFlagFast(InspectorFlags.NonPublic));
            else
                value = elementTypePair.Value.Value.GetDefaultValueNotNull(flags.HasFlagFast(InspectorFlags.NonPublic));

            return EntryHandler.CreateEntry(elementType, key, value);
        }

        public override GUIContent? GetElementLabel(int index) => null;

        public override void UpdateSourceCollections()
        {
            CheckInspectableList();
            
            // 키 중복 감지
            if (inspectableList.Cast<object?>().Select(x => EntryHandler.FindEntry(x).Key).GetDuplicatedItemIndices().IsEmpty())
                base.UpdateSourceCollections();
        }
    }
}