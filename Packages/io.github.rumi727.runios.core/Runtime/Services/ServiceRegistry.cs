#nullable enable
using RuniOS.Resource;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace RuniOS.Services
{
    /// <summary>
    /// Manages registered services and resolves them by the contract type requested by the caller.<br/>
    /// 등록된 서비스를 관리하고, 호출자가 요청한 계약 타입으로 서비스를 확인합니다.
    /// </summary>
    /// <remarks>
    /// Services do not declare a contract when they are registered. A contract is any type that a registered service is an instance of, and it is determined by the type passed to <see cref="Get(Type)"/> or <see cref="GetAll(Type)"/>.<br/>
    /// 서비스는 등록할 때 계약을 선언하지 않습니다. 계약은 등록된 서비스가 인스턴스인 모든 타입이며, <see cref="Get(Type)"/> 또는 <see cref="GetAll(Type)"/>에 전달한 타입으로 결정됩니다.
    /// </remarks>
    public static class ServiceRegistry
    {
        static readonly HashSet<IService> services = [];
        static readonly Dictionary<Identifier, IService> registriesById = [];
        static readonly Dictionary<Type, IService[]> servicesByContract = [];
        static readonly Dictionary<Type, Identifier> defaultIds = [];
        static Type[]? serviceTypes;

        /// <summary>
        /// Registers the specified service.<br/>
        /// 지정된 서비스를 등록합니다.
        /// </summary>
        /// <param name="service">
        /// The service to register.<br/>
        /// 등록할 서비스입니다.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when another registered service already uses the same <see cref="IService.id"/>.<br/>
        /// 등록된 다른 서비스가 같은 <see cref="IService.id"/>를 사용하면 발생합니다.
        /// </exception>
        public static void Register(IService service)
        {
            if (!registriesById.TryAdd(service.id, service))
                throw new InvalidOperationException($"Registry ID conflict: {service.id}");

            services.Add(service);
            servicesByContract.Clear();
            serviceTypes = null;
        }

        /// <summary>
        /// Unregisters the specified service.<br/>
        /// 지정된 서비스의 등록을 해제합니다.
        /// </summary>
        /// <param name="service">
        /// The registered service instance to unregister.<br/>
        /// 등록을 해제할 등록된 서비스 인스턴스입니다.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no service is registered with the <see cref="IService.id"/> of <paramref name="service"/>, or when the registered instance is not <paramref name="service"/>.<br/>
        /// <paramref name="service"/>의 <see cref="IService.id"/>로 등록된 서비스가 없거나, 등록된 인스턴스가 <paramref name="service"/>가 아니면 발생합니다.
        /// </exception>
        public static void Unregister(IService service)
        {
            if (Get(service.id) is not { } otherService)
                throw new InvalidOperationException($"Unregistered registry: {service.id}");
            if (service != otherService)
                throw new InvalidOperationException($"Unregistered registry instance: {service.id}");

            services.Remove(service);
            registriesById.Remove(service.id);
            servicesByContract.Clear();
            serviceTypes = null;
        }

        /// <summary>
        /// Sets the default service that <see cref="Get(Type)"/> prefers for the specified contract type.<br/>
        /// 지정된 계약 타입에 대해 <see cref="Get(Type)"/>이 우선하는 기본 서비스를 설정합니다.
        /// </summary>
        /// <param name="type">
        /// The contract type to set the default service for.<br/>
        /// 기본 서비스를 설정할 계약 타입입니다.
        /// </param>
        /// <param name="serviceId">
        /// The <see cref="IService.id"/> of the default service, or <see langword="null"/> to clear the default.<br/>
        /// 기본 서비스의 <see cref="IService.id"/>이며, 기본값을 해제하려면 <see langword="null"/>입니다.
        /// </param>
        /// <remarks>
        /// The default is only a preference and is not validated at the time of the call. It takes effect while a service with the identifier is registered, available, and an instance of <paramref name="type"/>.<br/>
        /// 기본값은 선호값일 뿐이며 호출 시점에는 검증하지 않습니다. 해당 식별자의 서비스가 등록되어 있고 사용 가능하며 <paramref name="type"/>의 인스턴스인 동안 적용됩니다.
        /// </remarks>
        public static void SetDefault(Type type, Identifier? serviceId)
        {
            if (serviceId is { } id)
                defaultIds[type] = id;
            else
                defaultIds.Remove(type);
        }

        /// <summary>
        /// Sets the default service that <see cref="Get{T}()"/> prefers for the contract type <typeparamref name="T"/>.<br/>
        /// 계약 타입 <typeparamref name="T"/>에 대해 <see cref="Get{T}()"/>이 우선하는 기본 서비스를 설정합니다.
        /// </summary>
        /// <typeparam name="T">
        /// The contract type to set the default service for.<br/>
        /// 기본 서비스를 설정할 계약 타입입니다.
        /// </typeparam>
        /// <param name="serviceId">
        /// The <see cref="IService.id"/> of the default service, or <see langword="null"/> to clear the default.<br/>
        /// 기본 서비스의 <see cref="IService.id"/>이며, 기본값을 해제하려면 <see langword="null"/>입니다.
        /// </param>
        public static void SetDefault<T>(Identifier? serviceId) where T : IService => SetDefault(typeof(T), serviceId);

        /// <summary>
        /// Gets the identifier of the default service set for the specified contract type.<br/>
        /// 지정된 계약 타입에 설정된 기본 서비스의 식별자를 가져옵니다.
        /// </summary>
        /// <param name="type">
        /// The contract type to get the default service identifier for.<br/>
        /// 기본 서비스의 식별자를 가져올 계약 타입입니다.
        /// </param>
        /// <returns>
        /// The identifier passed to <see cref="SetDefault(Type, Identifier?)"/>, or <see langword="null"/> when no default is set.<br/>
        /// <see cref="SetDefault(Type, Identifier?)"/>에 전달한 식별자를 반환하며, 설정된 기본값이 없으면 <see langword="null"/>을 반환합니다.
        /// </returns>
        /// <remarks>
        /// The identifier is returned as it was set, regardless of whether a service with the identifier is registered.<br/>
        /// 식별자는 해당 식별자의 서비스가 등록되어 있는지와 관계없이 설정된 그대로 반환됩니다.
        /// </remarks>
        public static Identifier? GetDefault(Type type) => defaultIds.TryGetValue(type, out Identifier id) ? id : (Identifier?)null;

        /// <summary>
        /// Gets the identifier of the default service set for the contract type <typeparamref name="T"/>.<br/>
        /// 계약 타입 <typeparamref name="T"/>에 설정된 기본 서비스의 식별자를 가져옵니다.
        /// </summary>
        /// <typeparam name="T">
        /// The contract type to get the default service identifier for.<br/>
        /// 기본 서비스의 식별자를 가져올 계약 타입입니다.
        /// </typeparam>
        /// <returns>
        /// The identifier passed to <see cref="SetDefault{T}(Identifier?)"/>, or <see langword="null"/> when no default is set.<br/>
        /// <see cref="SetDefault{T}(Identifier?)"/>에 전달한 식별자를 반환하며, 설정된 기본값이 없으면 <see langword="null"/>을 반환합니다.
        /// </returns>
        public static Identifier? GetDefault<T>() where T : IService => GetDefault(typeof(T));

        /// <summary>
        /// Gets the service registered with the specified identifier.<br/>
        /// 지정된 식별자로 등록된 서비스를 가져옵니다.
        /// </summary>
        /// <param name="identifier">
        /// The identifier of the service to retrieve.<br/>
        /// 가져올 서비스의 식별자입니다.
        /// </param>
        /// <returns>
        /// The matching service, or <see langword="null"/> when no service is registered with the identifier.<br/>
        /// 일치하는 서비스를 반환하며, 해당 식별자로 등록된 서비스가 없으면 <see langword="null"/>을 반환합니다.
        /// </returns>
        public static IService? Get(Identifier identifier) => registriesById.GetValueOrDefault(identifier);

        /// <summary>
        /// Gets the service for the contract type <typeparamref name="T"/>.<br/>
        /// 계약 타입 <typeparamref name="T"/>의 서비스를 가져옵니다.
        /// </summary>
        /// <typeparam name="T">
        /// The contract type that the service must be an instance of.<br/>
        /// 서비스가 인스턴스여야 하는 계약 타입입니다.
        /// </typeparam>
        /// <returns>
        /// The default service if it is registered and available; otherwise, one of the available registered services, or <see langword="null"/> when none is available.<br/>
        /// 기본 서비스가 등록되어 있고 사용 가능하면 그것을 반환하고, 그렇지 않으면 사용 가능한 등록된 서비스 중 하나를 반환하며, 사용 가능한 서비스가 없으면 <see langword="null"/>을 반환합니다.
        /// </returns>
        public static T? Get<T>() where T : IService => (T?)Get(typeof(T));

        /// <summary>
        /// Gets the service for the specified contract type.<br/>
        /// 지정된 계약 타입의 서비스를 가져옵니다.
        /// </summary>
        /// <param name="type">
        /// The contract type that the service must be an instance of.<br/>
        /// 서비스가 인스턴스여야 하는 계약 타입입니다.
        /// </param>
        /// <returns>
        /// The default service if it is registered, available, and an instance of <paramref name="type"/>; otherwise, one of the available registered services that is, or <see langword="null"/> when none is available.<br/>
        /// 기본 서비스가 등록되어 있고 사용 가능하며 <paramref name="type"/>의 인스턴스이면 그것을 반환하고, 그렇지 않으면 인스턴스인 사용 가능한 등록된 서비스 중 하나를 반환하며, 없으면 <see langword="null"/>을 반환합니다.
        /// </returns>
        /// <remarks>
        /// <see cref="IService.isAvailable"/> is evaluated on every call. A default that is currently unavailable is skipped but remains set, so it applies again once it becomes available.<br/>
        /// <see cref="IService.isAvailable"/>은 호출할 때마다 평가됩니다. 현재 사용할 수 없는 기본값은 건너뛰지만 설정은 유지되므로, 다시 사용 가능해지면 다시 적용됩니다.
        /// <br/><br/>
        /// Which service is returned when no default applies is unspecified.<br/>
        /// 적용되는 기본값이 없을 때 어떤 서비스가 반환되는지는 지정되지 않습니다.
        /// </remarks>
        public static IService? Get(Type type)
        {
            if (defaultIds.TryGetValue(type, out Identifier id) && Get(id) is { isAvailable: true } defaultService && type.IsInstanceOfType(defaultService))
                return defaultService;

            return GetAll(type).FirstOrDefault(x => x.isAvailable);
        }

        /// <summary>
        /// Tries to get the service for the contract type <typeparamref name="T"/>.<br/>
        /// 계약 타입 <typeparamref name="T"/>의 서비스를 가져오려고 시도합니다.
        /// </summary>
        /// <typeparam name="T">
        /// The contract type that the service must be an instance of.<br/>
        /// 서비스가 인스턴스여야 하는 계약 타입입니다.
        /// </typeparam>
        /// <param name="service">
        /// The service that <see cref="Get{T}()"/> returns when this method returns <see langword="true"/>; otherwise, <see langword="null"/>.<br/>
        /// 이 메서드가 <see langword="true"/>를 반환하면 <see cref="Get{T}()"/>가 반환하는 서비스이며, 그렇지 않으면 <see langword="null"/>입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if an available service was found; otherwise, <see langword="false"/>.<br/>
        /// 사용 가능한 서비스를 찾았으면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>입니다.
        /// </returns>
        public static bool TryGet<T>([NotNullWhen(true)] out T? service) where T : IService
        {
            service = Get<T>();
            return service != null;
        }

        /// <summary>
        /// Tries to get the service for the specified contract type.<br/>
        /// 지정된 계약 타입의 서비스를 가져오려고 시도합니다.
        /// </summary>
        /// <param name="type">
        /// The contract type that the service must be an instance of.<br/>
        /// 서비스가 인스턴스여야 하는 계약 타입입니다.
        /// </param>
        /// <param name="service">
        /// The service that <see cref="Get(Type)"/> returns when this method returns <see langword="true"/>; otherwise, <see langword="null"/>.<br/>
        /// 이 메서드가 <see langword="true"/>를 반환하면 <see cref="Get(Type)"/>이 반환하는 서비스이며, 그렇지 않으면 <see langword="null"/>입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if an available service was found; otherwise, <see langword="false"/>.<br/>
        /// 사용 가능한 서비스를 찾았으면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>입니다.
        /// </returns>
        public static bool TryGet(Type type, [NotNullWhen(true)] out IService? service)
        {
            service = Get(type);
            return service != null;
        }

        /// <summary>
        /// Gets all registered services.<br/>
        /// 등록된 모든 서비스를 가져옵니다.
        /// </summary>
        /// <returns>
        /// A read-only set containing all registered services.<br/>
        /// 등록된 모든 서비스를 포함하는 읽기 전용 집합을 반환합니다.
        /// </returns>
        public static ReadOnlySet<IService> GetAll() => services.AsReadOnly();

        /// <summary>
        /// Gets all registered services that are instances of the contract type <typeparamref name="T"/>.<br/>
        /// 계약 타입 <typeparamref name="T"/>의 인스턴스인 등록된 모든 서비스를 가져옵니다.
        /// </summary>
        /// <typeparam name="T">
        /// The contract type that the services must be an instance of.<br/>
        /// 서비스가 인스턴스여야 하는 계약 타입입니다.
        /// </typeparam>
        /// <returns>
        /// The matching services, or an empty sequence when none is registered.<br/>
        /// 일치하는 서비스를 반환하며, 등록된 서비스가 없으면 빈 시퀀스를 반환합니다.
        /// </returns>
        public static IEnumerable<T> GetAll<T>() where T : IService => GetAll(typeof(T)).Cast<T>();

        /// <summary>
        /// Gets all registered services that are instances of the specified contract type.<br/>
        /// 지정된 계약 타입의 인스턴스인 등록된 모든 서비스를 가져옵니다.
        /// </summary>
        /// <param name="type">
        /// The contract type that the services must be an instance of.<br/>
        /// 서비스가 인스턴스여야 하는 계약 타입입니다.
        /// </param>
        /// <returns>
        /// The matching services, or an empty list when none is registered.<br/>
        /// 일치하는 서비스를 반환하며, 등록된 서비스가 없으면 빈 목록을 반환합니다.
        /// </returns>
        /// <remarks>
        /// The result for each contract type is indexed on its first request and discarded whenever a service is registered or unregistered.<br/>
        /// 계약 타입별 결과는 처음 요청할 때 인덱싱되며, 서비스를 등록하거나 등록 해제할 때마다 폐기됩니다.
        /// </remarks>
        public static IReadOnlyList<IService> GetAll(Type type)
        {
            if (!servicesByContract.TryGetValue(type, out IService[]? array))
                servicesByContract[type] = array = [..services.Where(type.IsInstanceOfType)];

            return array;
        }

        /// <summary>
        /// Gets the contract types of the registered services.<br/>
        /// 등록된 서비스의 계약 타입을 가져옵니다.
        /// </summary>
        /// <returns>
        /// The interfaces that extend <see cref="IService"/> and are implemented by at least one registered service, or an empty list when none is registered.<br/>
        /// <see cref="IService"/>를 상속하고 등록된 서비스가 하나 이상 구현한 인터페이스를 반환하며, 등록된 서비스가 없으면 빈 목록을 반환합니다.
        /// </returns>
        /// <remarks>
        /// <see cref="IService"/> itself is not included, and every interface in an inheritance chain that extends <see cref="IService"/> is included.<br/>
        /// <see cref="IService"/> 자체는 포함되지 않으며, <see cref="IService"/>를 상속하는 상속 체인의 모든 인터페이스가 포함됩니다.
        /// <br/><br/>
        /// The result is indexed on its first request and discarded whenever a service is registered or unregistered.<br/>
        /// 결과는 처음 요청할 때 인덱싱되며, 서비스를 등록하거나 등록 해제할 때마다 폐기됩니다.
        /// </remarks>
        public static IReadOnlyList<Type> GetServiceTypes() => serviceTypes ??=
        [
            ..services.SelectMany(x => x.GetType().GetInterfaces())
                .Where(x => x != typeof(IService) && typeof(IService).IsAssignableFrom(x))
                .Distinct()
        ];
    }
}
