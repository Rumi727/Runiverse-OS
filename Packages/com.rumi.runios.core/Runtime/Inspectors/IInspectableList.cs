#nullable enable
using System.Collections;

namespace RuniOS.Inspectors;

public interface IInspectableList : IInspectable, IList
{
    /// <summary>
    /// 리스트의 요소 타입을 가져옵니다.<br/>
    /// null을 반환하는 경우, 리스트가 모든 타입 형식을 허용한다는 의미입니다.
    /// </summary>
    Type? inspectionElementType { get; }
        
    RuniNullabilityInfo? elementNullabilityInfo { get; }
        
    /// <summary>
    /// 쓰기 가능 상태인 상속된 부모 요소가 있을 때만 크기를 바꿀 수 있는 리스트인지 여부를 나타냅니다.
    /// <br/><br/>
    /// 예: 배열은 그 자채로는 크기를 바꾸지 못하며, 변수에 새로 할당해야 크기를 간접적으로 바꿀 수 있습니다.
    /// </summary>
    bool isArray { get; }

    void Move(int oldIndex, int newIndex);
        
    /// <summary>
    /// 요소가 <b>삽입되었을 때</b> 호출됩니다.
    /// </summary>
    void OnInsert(int index);
        
    /// <summary>
    /// 요소가 <b>제거되었을 때</b> 호출됩니다.
    /// </summary>
    void OnRemoveAt(int index);

    /// <summary>
    /// 요소가 위치를 <b>이동했을 때</b> 호출됩니다.
    /// </summary>
    void OnElementMoved(int oldIndex, int newIndex);
        
    /// <summary>
    /// 요소가 서로의 위치를 <b>바꿨을 때</b> 호출됩니다.
    /// </summary>
    void OnElementChanged(int oldIndex, int newIndex);
        
    /// <summary>
    /// 모든 요소가 <b>제거되었을 때</b> 호출됩니다.
    /// </summary>
    void OnClear();

    new IReadOnlyList<IInspectorListElement> GetElements(InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List);
    IEnumerable<IInspectorElement> IInspectable.GetElements(InspectorFlags flags) => GetElements(flags);
        
    IInspectorListElement? GetElement(int index, InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List);

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
        
    new IInspectableList Clone();
    IInspectable IInspectable.Clone() => Clone();

    void SynchronizeCollections();
    void UpdateSourceCollections();
}