#nullable enable
using RuniOS.Reflection;
using System.Collections;

namespace RuniOS.Inspectors
{
    public interface IInspectableDictionary : IInspectable, IDictionary
    {
        /// <summary>
        /// 리스트의 요소 타입을 가져옵니다.<br/>
        /// null을 반환하는 경우, 리스트가 모든 타입 형식을 허용한다는 의미입니다.
        /// </summary>
        KeyValuePair<Type, Type>? inspectionElementType { get; }
        
        NullabilityInfo? elementNullabilityInfo { get; }

        void RenameKey(object fromKey, object toKey);
        
        void OnRenamedKey(object fromKey, object toKey);
        
        new IReadOnlyDictionary<object, IInspectorDictionaryElement> GetElements(InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List);
        IEnumerable<IInspectorElement> IInspectable.GetElements(InspectorFlags flags) => GetElements(flags).Select(x => x.Value);

        IInspectorDictionaryElement? GetElement(object key, InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List);

        bool HasFlags(InspectorFlags flags)
        {
            if (flags == InspectorFlags.None)
                return false;
            
            if (!flags.HasFlagFast(InspectorFlags.Public | InspectorFlags.Instance | InspectorFlags.List))
                return false;
            
            if (IsReadOnly && !flags.HasFlagFast(InspectorFlags.ReadOnly))
                return false;

            return true;
        }
        
        new IInspectableDictionary Clone();
        IInspectable IInspectable.Clone() => Clone();

        void SynchronizeCollections();
        void UpdateSourceCollections();
    }
}