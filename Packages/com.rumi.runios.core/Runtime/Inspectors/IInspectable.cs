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

        Action<IEnumerable<object?>>? onValueChanged { get; set; }
        
        IEnumerable<IInspectorElement> GetElements(InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List);

        /// <summary>
        /// 검사 중인 인스턴스의 타입을 가져옵니다.
        /// </summary>
        /// <param name="type">가져온 인스턴스의 타입입니다.</param>
        /// <returns>타입을 성공적으로 가져올 시 true를 반환합니다.</returns>
        bool TryGetInspectionType([NotNullWhen(true)] out Type? type);
        
        void OnValueChangedInvoke();

        /// <summary>
        /// 복제본을 생성합니다. 검사 중인 객체의 목록까지 같이 복제합니다.<br/>
        /// 즉, 외부에서 인스턴스 목록을 교채해도, 이 복제본은 영향받지 않습니다.<br/>
        /// 언도 히스토리에 기록할 때 유용합니다.
        /// </summary>
        new IInspectable Clone();
        object ICloneable.Clone() => Clone();
    }
}