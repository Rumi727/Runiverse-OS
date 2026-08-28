#nullable enable

namespace RuniOS.Reflection
{
    /// <summary>
    /// Provides the common registration contract used by generated and manual type registries.<br/>
    /// 생성된 타입 레지스트리와 수동 타입 레지스트리가 사용하는 공통 등록 계약을 제공합니다.
    /// </summary>
    // RuniOS.CodeAnalysis는 `RuniOS.Reflection.TypeRegistry` 메타데이터 이름으로 이 기본 타입을 조회합니다.
    public abstract class TypeRegistry
    {
        /// <summary>
        /// Occurs after the registry contents have changed.<br/>
        /// 레지스트리 내용이 변경된 후 발생합니다.
        /// </summary>
        public abstract event Action? onChanged;

        /// <summary>
        /// Registers the specified implementation type using the registry's runtime discovery policy.<br/>
        /// 레지스트리의 런타임 검색 정책을 사용하여 지정된 구현 타입을 등록합니다.
        /// </summary>
        /// <param name="type">
        /// The implementation type to register.<br/>
        /// 등록할 구현 타입입니다.
        /// </param>
        // 생성된 등록 코드가 assembly load 시 public `Register(typeof(구현 타입))`을 호출하므로 이름과 `Type` 매개변수 계약을 유지합니다.
        public abstract void Register(Type type);

        /// <summary>
        /// Removes registrations associated with the specified implementation type.<br/>
        /// 지정된 구현 타입과 연결된 등록을 제거합니다.
        /// </summary>
        /// <param name="type">
        /// The implementation type to unregister.<br/>
        /// 등록 해제할 구현 타입입니다.
        /// </param>
        // 생성된 등록 코드가 assembly unload 시 public `Unregister(typeof(구현 타입))`을 호출하므로 이름과 `Type` 매개변수 계약을 유지합니다.
        public abstract void Unregister(Type type);
    }
}
