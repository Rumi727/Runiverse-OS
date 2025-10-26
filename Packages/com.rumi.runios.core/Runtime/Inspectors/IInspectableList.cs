#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;

namespace RuniOS.Inspectors
{
    public interface IInspectableList : IInspectable, IList
    {
        /// <remarks>
        /// null을 반환하는 경우, 리스트가 모든 타입 형식을 허용한다는 의미입니다.
        /// </remarks>
        string? inspectionElementDisplayName { get; }
        
        RuniNullabilityInfo? nullabilityInfo { get; }
        
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

        new IReadOnlyList<IInspectorElement> GetElements(InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List);
        IInspectorListElement? GetElement(int index, InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List);
        
        /// <summary>
        /// 리스트의 요소 타입을 가져옵니다.
        /// </summary>
        /// <param name="type">가져온 타입입니다. null 값일 경우, 리스트가 모든 타입 형식을 허용한다는 의미입니다.</param>
        /// <returns>타입을 성공적으로 가져올 시 true를 반환합니다.</returns>
        bool TryGetInspectionElementType(out Type? type);

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
    }
}