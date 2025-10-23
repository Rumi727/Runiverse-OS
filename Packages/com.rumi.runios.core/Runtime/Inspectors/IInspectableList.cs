#nullable enable
using System;
using System.Collections;
using System.Reflection;

namespace RuniOS.Inspectors
{
    public interface IInspectableList : IInspectable, IList
    {
        /// <remarks>
        /// null을 반환하는 경우, 리스트가 모든 타입 형식을 허용한다는 의미입니다.
        /// </remarks>
        string? inspectionElementDisplayName { get; }
        
        NullabilityInfo? nullabilityInfo { get; }
        
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
    }
}