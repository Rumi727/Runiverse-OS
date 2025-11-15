#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace RuniOS.Inspectors
{
    public interface IInspectable : ICloneable
    {
        /// <summary>
        /// 명확한 이유가 없으면 값을 변경하지 마세요.
        /// </summary>
        IInspectorVariableElement? parentElement { get; set; }
        
        string inspectionDisplayName { get; }

        bool instancesIsEmpty { get; }
        
        bool instanceIsMultiple { get; }
        
        int instanceCount { get; }
        
        IEnumerable<IInspectorElement> GetElements(InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List);

        /// <summary>
        /// 검사 중인 인스턴스의 타입을 가져옵니다.
        /// </summary>
        /// <param name="type">가져온 인스턴스의 타입입니다.</param>
        /// <returns>타입을 성공적으로 가져올 시 true를 반환합니다.</returns>
        bool TryGetInspectionType([NotNullWhen(true)] out Type? type);

        new IInspectable Clone();
        object ICloneable.Clone() => Clone();
    }
}