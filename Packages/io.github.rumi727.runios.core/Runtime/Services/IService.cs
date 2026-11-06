#nullable enable
using RuniOS.Resource;

namespace RuniOS.Services
{
    /// <summary>
    /// Represents a service that implements a contract and can be registered in <see cref="ServiceRegistry"/>.<br/>
    /// 계약을 구현하며 <see cref="ServiceRegistry"/>에 등록할 수 있는 서비스를 나타냅니다.
    /// </summary>
    /// <remarks>
    /// A contract is an interface that extends this interface, and <see cref="ServiceRegistry"/> resolves services by the contract type that the caller requests.
    /// <br/><br/>
    /// 계약은 이 인터페이스를 상속한 인터페이스이며, <see cref="ServiceRegistry"/>는 호출자가 요청한 계약 타입으로 서비스를 확인합니다.
    /// </remarks>
    public interface IService
    {
        /// <summary>
        /// Gets the identifier of this service.<br/>
        /// 이 서비스의 식별자를 가져옵니다.
        /// </summary>
        /// <remarks>
        /// The identifier must be unique among all registered services, regardless of their contract types. <see cref="ServiceRegistry.Register(IService)"/> throws when another registered service uses the same identifier.
        /// <br/><br/>
        /// 식별자는 계약 타입과 관계없이 등록된 모든 서비스 사이에서 고유해야 합니다. 등록된 다른 서비스가 같은 식별자를 사용하면 <see cref="ServiceRegistry.Register(IService)"/>가 예외를 던집니다.
        /// </remarks>
        Identifier id { get; }

        /// <summary>
        /// Gets whether this service can currently fulfill its contract.<br/>
        /// 이 서비스가 현재 계약을 이행할 수 있는지 여부를 가져옵니다.
        /// </summary>
        /// <remarks>
        /// The value may change at runtime, and <see cref="ServiceRegistry"/> evaluates it every time it resolves a service. The default implementation always returns <see langword="true"/>.<br/>
        /// 값은 런타임에 바뀔 수 있으며, <see cref="ServiceRegistry"/>는 서비스를 확인할 때마다 이 값을 평가합니다. 기본 구현은 항상 <see langword="true"/>를 반환합니다.
        /// </remarks>
        bool isAvailable => true;
    }
}