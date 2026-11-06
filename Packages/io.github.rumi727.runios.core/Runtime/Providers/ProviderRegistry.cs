#nullable enable
using RuniOS.Resource;
using System.Collections.ObjectModel;

namespace RuniOS.Providers
{
    /// <summary>
    /// Manages registered providers and resolves them by the contract type requested by the caller.<br/>
    /// 등록된 프로바이더를 관리하고, 호출자가 요청한 계약 타입으로 프로바이더를 확인합니다.
    /// </summary>
    /// <remarks>
    /// Providers do not declare a contract when they are registered. A contract is any type that a registered provider is an instance of, and it is determined by the type passed to <see cref="Get(Type)"/> or <see cref="GetAll(Type)"/>.<br/>
    /// 프로바이더는 등록할 때 계약을 선언하지 않습니다. 계약은 등록된 프로바이더가 인스턴스인 모든 타입이며, <see cref="Get(Type)"/> 또는 <see cref="GetAll(Type)"/>에 전달한 타입으로 결정됩니다.
    /// </remarks>
    public static class ProviderRegistry
    {
        static readonly HashSet<IProvider> providers = [];
        static readonly Dictionary<Identifier, IProvider> registriesById = [];
        static readonly Dictionary<Type, IProvider[]> providersByContract = [];
        static readonly Dictionary<Type, Identifier> defaultIds = [];
        static Type[]? providerTypes;

        /// <summary>
        /// Registers the specified provider.<br/>
        /// 지정된 프로바이더를 등록합니다.
        /// </summary>
        /// <param name="provider">
        /// The provider to register.<br/>
        /// 등록할 프로바이더입니다.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when another registered provider already uses the same <see cref="IProvider.id"/>.<br/>
        /// 등록된 다른 프로바이더가 같은 <see cref="IProvider.id"/>를 사용하면 발생합니다.
        /// </exception>
        public static void Register(IProvider provider)
        {
            if (!registriesById.TryAdd(provider.id, provider))
                throw new InvalidOperationException($"Registry ID conflict: {provider.id}");

            providers.Add(provider);
            providersByContract.Clear();
            providerTypes = null;
        }

        /// <summary>
        /// Unregisters the specified provider.<br/>
        /// 지정된 프로바이더의 등록을 해제합니다.
        /// </summary>
        /// <param name="provider">
        /// The registered provider instance to unregister.<br/>
        /// 등록을 해제할 등록된 프로바이더 인스턴스입니다.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no provider is registered with the <see cref="IProvider.id"/> of <paramref name="provider"/>, or when the registered instance is not <paramref name="provider"/>.<br/>
        /// <paramref name="provider"/>의 <see cref="IProvider.id"/>로 등록된 프로바이더가 없거나, 등록된 인스턴스가 <paramref name="provider"/>가 아니면 발생합니다.
        /// </exception>
        public static void Unregister(IProvider provider)
        {
            if (Get(provider.id) is not { } otherProvider)
                throw new InvalidOperationException($"Unregistered registry: {provider.id}");
            if (provider != otherProvider)
                throw new InvalidOperationException($"Unregistered registry instance: {provider.id}");

            providers.Remove(provider);
            registriesById.Remove(provider.id);
            providersByContract.Clear();
            providerTypes = null;
        }

        /// <summary>
        /// Sets the default provider that <see cref="Get(Type)"/> prefers for the specified contract type.<br/>
        /// 지정된 계약 타입에 대해 <see cref="Get(Type)"/>이 우선하는 기본 프로바이더를 설정합니다.
        /// </summary>
        /// <param name="type">
        /// The contract type to set the default provider for.<br/>
        /// 기본 프로바이더를 설정할 계약 타입입니다.
        /// </param>
        /// <param name="providerId">
        /// The <see cref="IProvider.id"/> of the default provider, or <see langword="null"/> to clear the default.<br/>
        /// 기본 프로바이더의 <see cref="IProvider.id"/>이며, 기본값을 해제하려면 <see langword="null"/>입니다.
        /// </param>
        /// <remarks>
        /// The default is only a preference and is not validated at the time of the call. It takes effect while a provider with the identifier is registered, available, and an instance of <paramref name="type"/>.<br/>
        /// 기본값은 선호값일 뿐이며 호출 시점에는 검증하지 않습니다. 해당 식별자의 프로바이더가 등록되어 있고 사용 가능하며 <paramref name="type"/>의 인스턴스인 동안 적용됩니다.
        /// </remarks>
        public static void SetDefault(Type type, Identifier? providerId)
        {
            if (providerId is { } id)
                defaultIds[type] = id;
            else
                defaultIds.Remove(type);
        }

        /// <summary>
        /// Sets the default provider that <see cref="Get{T}()"/> prefers for the contract type <typeparamref name="T"/>.<br/>
        /// 계약 타입 <typeparamref name="T"/>에 대해 <see cref="Get{T}()"/>이 우선하는 기본 프로바이더를 설정합니다.
        /// </summary>
        /// <typeparam name="T">
        /// The contract type to set the default provider for.<br/>
        /// 기본 프로바이더를 설정할 계약 타입입니다.
        /// </typeparam>
        /// <param name="providerId">
        /// The <see cref="IProvider.id"/> of the default provider, or <see langword="null"/> to clear the default.<br/>
        /// 기본 프로바이더의 <see cref="IProvider.id"/>이며, 기본값을 해제하려면 <see langword="null"/>입니다.
        /// </param>
        public static void SetDefault<T>(Identifier? providerId) where T : IProvider => SetDefault(typeof(T), providerId);

        /// <summary>
        /// Gets the identifier of the default provider set for the specified contract type.<br/>
        /// 지정된 계약 타입에 설정된 기본 프로바이더의 식별자를 가져옵니다.
        /// </summary>
        /// <param name="type">
        /// The contract type to get the default provider identifier for.<br/>
        /// 기본 프로바이더의 식별자를 가져올 계약 타입입니다.
        /// </param>
        /// <returns>
        /// The identifier passed to <see cref="SetDefault(Type, Identifier?)"/>, or <see langword="null"/> when no default is set.<br/>
        /// <see cref="SetDefault(Type, Identifier?)"/>에 전달한 식별자를 반환하며, 설정된 기본값이 없으면 <see langword="null"/>을 반환합니다.
        /// </returns>
        /// <remarks>
        /// The identifier is returned as it was set, regardless of whether a provider with the identifier is registered.<br/>
        /// 식별자는 해당 식별자의 프로바이더가 등록되어 있는지와 관계없이 설정된 그대로 반환됩니다.
        /// </remarks>
        public static Identifier? GetDefault(Type type) => defaultIds.TryGetValue(type, out Identifier id) ? id : (Identifier?)null;

        /// <summary>
        /// Gets the identifier of the default provider set for the contract type <typeparamref name="T"/>.<br/>
        /// 계약 타입 <typeparamref name="T"/>에 설정된 기본 프로바이더의 식별자를 가져옵니다.
        /// </summary>
        /// <typeparam name="T">
        /// The contract type to get the default provider identifier for.<br/>
        /// 기본 프로바이더의 식별자를 가져올 계약 타입입니다.
        /// </typeparam>
        /// <returns>
        /// The identifier passed to <see cref="SetDefault{T}(Identifier?)"/>, or <see langword="null"/> when no default is set.<br/>
        /// <see cref="SetDefault{T}(Identifier?)"/>에 전달한 식별자를 반환하며, 설정된 기본값이 없으면 <see langword="null"/>을 반환합니다.
        /// </returns>
        public static Identifier? GetDefault<T>() where T : IProvider => GetDefault(typeof(T));

        /// <summary>
        /// Gets the provider registered with the specified identifier.<br/>
        /// 지정된 식별자로 등록된 프로바이더를 가져옵니다.
        /// </summary>
        /// <param name="identifier">
        /// The identifier of the provider to retrieve.<br/>
        /// 가져올 프로바이더의 식별자입니다.
        /// </param>
        /// <returns>
        /// The matching provider, or <see langword="null"/> when no provider is registered with the identifier.<br/>
        /// 일치하는 프로바이더를 반환하며, 해당 식별자로 등록된 프로바이더가 없으면 <see langword="null"/>을 반환합니다.
        /// </returns>
        public static IProvider? Get(Identifier identifier) => registriesById.GetValueOrDefault(identifier);

        /// <summary>
        /// Gets the provider for the contract type <typeparamref name="T"/>.<br/>
        /// 계약 타입 <typeparamref name="T"/>의 프로바이더를 가져옵니다.
        /// </summary>
        /// <typeparam name="T">
        /// The contract type that the provider must be an instance of.<br/>
        /// 프로바이더가 인스턴스여야 하는 계약 타입입니다.
        /// </typeparam>
        /// <returns>
        /// The default provider if it is registered and available; otherwise, one of the available registered providers, or <see langword="null"/> when none is available.<br/>
        /// 기본 프로바이더가 등록되어 있고 사용 가능하면 그것을 반환하고, 그렇지 않으면 사용 가능한 등록된 프로바이더 중 하나를 반환하며, 사용 가능한 프로바이더가 없으면 <see langword="null"/>을 반환합니다.
        /// </returns>
        public static T? Get<T>() where T : IProvider => (T?)Get(typeof(T));

        /// <summary>
        /// Gets the provider for the specified contract type.<br/>
        /// 지정된 계약 타입의 프로바이더를 가져옵니다.
        /// </summary>
        /// <param name="type">
        /// The contract type that the provider must be an instance of.<br/>
        /// 프로바이더가 인스턴스여야 하는 계약 타입입니다.
        /// </param>
        /// <returns>
        /// The default provider if it is registered, available, and an instance of <paramref name="type"/>; otherwise, one of the available registered providers that is, or <see langword="null"/> when none is available.<br/>
        /// 기본 프로바이더가 등록되어 있고 사용 가능하며 <paramref name="type"/>의 인스턴스이면 그것을 반환하고, 그렇지 않으면 인스턴스인 사용 가능한 등록된 프로바이더 중 하나를 반환하며, 없으면 <see langword="null"/>을 반환합니다.
        /// </returns>
        /// <remarks>
        /// <see cref="IProvider.isAvailable"/> is evaluated on every call. A default that is currently unavailable is skipped but remains set, so it applies again once it becomes available.<br/>
        /// <see cref="IProvider.isAvailable"/>은 호출할 때마다 평가됩니다. 현재 사용할 수 없는 기본값은 건너뛰지만 설정은 유지되므로, 다시 사용 가능해지면 다시 적용됩니다.
        /// <br/><br/>
        /// Which provider is returned when no default applies is unspecified.<br/>
        /// 적용되는 기본값이 없을 때 어떤 프로바이더가 반환되는지는 지정되지 않습니다.
        /// </remarks>
        public static IProvider? Get(Type type)
        {
            if (defaultIds.TryGetValue(type, out Identifier id) && Get(id) is { isAvailable: true } defaultProvider && type.IsInstanceOfType(defaultProvider))
                return defaultProvider;

            return GetAll(type).FirstOrDefault(x => x.isAvailable);
        }

        /// <summary>
        /// Gets all registered providers.<br/>
        /// 등록된 모든 프로바이더를 가져옵니다.
        /// </summary>
        /// <returns>
        /// A read-only set containing all registered providers.<br/>
        /// 등록된 모든 프로바이더를 포함하는 읽기 전용 집합을 반환합니다.
        /// </returns>
        public static ReadOnlySet<IProvider> GetAll() => providers.AsReadOnly();

        /// <summary>
        /// Gets all registered providers that are instances of the contract type <typeparamref name="T"/>.<br/>
        /// 계약 타입 <typeparamref name="T"/>의 인스턴스인 등록된 모든 프로바이더를 가져옵니다.
        /// </summary>
        /// <typeparam name="T">
        /// The contract type that the providers must be an instance of.<br/>
        /// 프로바이더가 인스턴스여야 하는 계약 타입입니다.
        /// </typeparam>
        /// <returns>
        /// The matching providers, or an empty sequence when none is registered.<br/>
        /// 일치하는 프로바이더를 반환하며, 등록된 프로바이더가 없으면 빈 시퀀스를 반환합니다.
        /// </returns>
        public static IEnumerable<T> GetAll<T>() where T : IProvider => GetAll(typeof(T)).Cast<T>();

        /// <summary>
        /// Gets all registered providers that are instances of the specified contract type.<br/>
        /// 지정된 계약 타입의 인스턴스인 등록된 모든 프로바이더를 가져옵니다.
        /// </summary>
        /// <param name="type">
        /// The contract type that the providers must be an instance of.<br/>
        /// 프로바이더가 인스턴스여야 하는 계약 타입입니다.
        /// </param>
        /// <returns>
        /// The matching providers, or an empty list when none is registered.<br/>
        /// 일치하는 프로바이더를 반환하며, 등록된 프로바이더가 없으면 빈 목록을 반환합니다.
        /// </returns>
        /// <remarks>
        /// The result for each contract type is indexed on its first request and discarded whenever a provider is registered or unregistered.<br/>
        /// 계약 타입별 결과는 처음 요청할 때 인덱싱되며, 프로바이더를 등록하거나 등록 해제할 때마다 폐기됩니다.
        /// </remarks>
        public static IReadOnlyList<IProvider> GetAll(Type type)
        {
            if (!providersByContract.TryGetValue(type, out IProvider[]? array))
                providersByContract[type] = array = [..providers.Where(type.IsInstanceOfType)];

            return array;
        }

        /// <summary>
        /// Gets the contract types of the registered providers.<br/>
        /// 등록된 프로바이더의 계약 타입을 가져옵니다.
        /// </summary>
        /// <returns>
        /// The interfaces that extend <see cref="IProvider"/> and are implemented by at least one registered provider, or an empty list when none is registered.<br/>
        /// <see cref="IProvider"/>를 상속하고 등록된 프로바이더가 하나 이상 구현한 인터페이스를 반환하며, 등록된 프로바이더가 없으면 빈 목록을 반환합니다.
        /// </returns>
        /// <remarks>
        /// <see cref="IProvider"/> itself is not included, and every interface in an inheritance chain that extends <see cref="IProvider"/> is included.<br/>
        /// <see cref="IProvider"/> 자체는 포함되지 않으며, <see cref="IProvider"/>를 상속하는 상속 체인의 모든 인터페이스가 포함됩니다.
        /// <br/><br/>
        /// The result is indexed on its first request and discarded whenever a provider is registered or unregistered.<br/>
        /// 결과는 처음 요청할 때 인덱싱되며, 프로바이더를 등록하거나 등록 해제할 때마다 폐기됩니다.
        /// </remarks>
        public static IReadOnlyList<Type> GetProviderTypes() => providerTypes ??=
        [
            ..providers.SelectMany(x => x.GetType().GetInterfaces())
                .Where(x => x != typeof(IProvider) && typeof(IProvider).IsAssignableFrom(x))
                .Distinct()
        ];
    }
}
