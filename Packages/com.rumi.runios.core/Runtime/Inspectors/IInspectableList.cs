#nullable enable
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
        
        IInspectorElement? GetElement(int index, InspectorFlags flags = InspectorFlags.All);
    }
}