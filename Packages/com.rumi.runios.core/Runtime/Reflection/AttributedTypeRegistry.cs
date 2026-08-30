#nullable enable
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace RuniOS.Reflection
{
    /// <summary>
    /// Stores attribute-based registrations and resolves implementations for target types.<br/>
    /// 특성 기반 등록을 저장하고 대상 타입에 대한 구현 타입을 확인합니다.
    /// </summary>
    /// <typeparam name="TAttribute">
    /// The registration attribute type stored in this registry.<br/>
    /// 이 레지스트리에 저장하는 등록 특성 타입입니다.
    /// </typeparam>
    public sealed class AttributedTypeRegistry<TAttribute> : TypeRegistry where TAttribute : TypeRegistrationAttribute
    {
        readonly record struct TypeResolution(Type matchedTargetType, Type implementationType);

        /// <summary>
        /// Gets the type that registered implementations must match or derive from.<br/>
        /// 등록 구현 타입이 일치하거나 상속해야 하는 타입을 가져옵니다.
        /// </summary>
        public Type baseType { get; }

        /// <summary>
        /// Initializes a registry for implementations matching the specified base type.<br/>
        /// 지정된 기본 타입과 일치하는 구현 타입을 위한 레지스트리를 초기화합니다.
        /// </summary>
        /// <param name="baseType">
        /// The type that registered implementations must match or derive from.<br/>
        /// 등록 구현 타입이 일치하거나 상속해야 하는 타입입니다.
        /// </param>
        public AttributedTypeRegistry(Type baseType) => this.baseType = baseType;

        readonly Dictionary<Type, List<TAttribute>> registrationsByImplementationType = [];
        volatile ConcurrentDictionary<Type, TypeResolution?> resolutionCache = new();
        readonly object registrationLock = new();

        public ImmutableArray<RegistrationEntry<TAttribute>> registeredEntries
        {
            get
            {
                lock (registrationLock)
                {
                    if (entriesSnapshot.IsDefault)
                    {
                        entriesSnapshot =
                        [
                            ..registrationsByImplementationType.SelectMany(pair => pair.Value.Select(attribute => new RegistrationEntry<TAttribute>(pair.Key, attribute)))
                                .OrderByTypes(x => x.attribute.targetType, x => x.attribute.priority)
                        ];
                    }

                    return entriesSnapshot;
                }
            }
        }
        ImmutableArray<RegistrationEntry<TAttribute>> entriesSnapshot;


        public override event Action? onChanged
        {
            add
            {
                lock (onChangedLock)
                    _onChanged += value;
            }
            remove
            {
                lock (onChangedLock)
                    _onChanged -= value;
            }
        }
        Action? _onChanged;
        readonly object onChangedLock = new();

        public override void Register(Type implementationType)
        {
            if ((baseType.IsAbstract && baseType == implementationType) || !baseType.IsAssignableFrom(implementationType))
                return;

            TAttribute[] attributes = implementationType.GetCustomAttributes<TAttribute>().ToArray();
            lock (registrationLock)
            {
                if (!registrationsByImplementationType.TryGetValue(implementationType, out List<TAttribute> list))
                    registrationsByImplementationType[implementationType] = list = [];

                list.AddRange(attributes);

                entriesSnapshot = default;
                resolutionCache = [];
            }

            lock (onChangedLock)
                _onChanged?.Invoke();
        }

        /// <summary>
        /// Registers preconstructed entries as one batch without reflecting over implementation types.<br/>
        /// 구현 타입을 리플렉션으로 조회하지 않고 미리 생성된 항목을 하나의 batch로 등록합니다.
        /// </summary>
        /// <param name="entries">
        /// The entries to register without reflection or validation.<br/>
        /// 리플렉션이나 검증 없이 등록할 항목입니다.
        /// </param>
        /// <remarks>
        /// Cache invalidation occurs once, and <see cref="onChanged"/> is raised once after the registration lock is released.<br/>
        /// 캐시 무효화는 한 번 수행하며, 등록 잠금 해제 후 <see cref="onChanged"/>를 한 번 발생시킵니다.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This is for source generators only. Please use Register(Type) for manual registration.")]
        // AttributedTypeRegistrySourceGenerator가 생성한 `RegistrationEntry<TAttribute>` 항목을 이 `ReadOnlySpan<RegistrationEntry<TAttribute>>` API로 전달합니다.
        public void DirectRegisterRange(params ReadOnlySpan<RegistrationEntry<TAttribute>> entries)
        {
            lock (registrationLock)
            {
                foreach (RegistrationEntry<TAttribute> entry in entries)
                {
                    if (!registrationsByImplementationType.TryGetValue(entry.implementationType, out List<TAttribute> list))
                        registrationsByImplementationType[entry.implementationType] = list = [];

                    list.Add(entry.attribute);
                }

                entriesSnapshot = default;
                resolutionCache = [];
            }

            lock (onChangedLock)
                _onChanged?.Invoke();
        }

        public override void Unregister(Type implementationType)
        {
            lock (registrationLock)
            {
                if (!registrationsByImplementationType.Remove(implementationType))
                    return;

                entriesSnapshot = default;
                resolutionCache = [];
            }

            lock (onChangedLock)
                _onChanged?.Invoke();
        }

        public Type? Resolve(Type targetType, Func<RegistrationEntry<TAttribute>, bool>? predicate = null)
        {
            TryResolve(targetType, out _, out Type? implementationType, predicate);
            return implementationType;
        }

        public bool TryResolve(Type targetType, [NotNullWhen(true)] out Type? matchedTargetType, [NotNullWhen(true)] out Type? implementationType, Func<RegistrationEntry<TAttribute>, bool>? predicate = null)
        {
            ConcurrentDictionary<Type, TypeResolution?> cache = resolutionCache;
            if (predicate == null)
            {
                if (cache.TryGetValue(targetType, out TypeResolution? value))
                {
                    if (value == null)
                    {
                        matchedTargetType = null;
                        implementationType = null;

                        return false;
                    }

                    matchedTargetType = value.Value.matchedTargetType;
                    implementationType = value.Value.implementationType;

                    return true;
                }
            }

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (RegistrationEntry<TAttribute> registration in registeredEntries)
            {
                // 조건부 엑세스도 사용 가능하지만 가독성을 해칩니다.
                if (predicate != null && !predicate.Invoke(registration))
                    continue;

                matchedTargetType = targetType;

                if (targetType == registration.attribute.targetType || (registration.attribute.useForChildren && targetType.IsAssignableToAny(registration.attribute.targetType, out matchedTargetType)))
                {
                    implementationType = registration.implementationType;

                    if (implementationType.IsGenericTypeDefinition)
                    {
                        Type[] genericArguments = matchedTargetType.GenericTypeArguments;
                        int implementationTypeGenericParametersLength = implementationType.GetGenericArguments().Length;

                        if (genericArguments.Length != implementationTypeGenericParametersLength)
                        {
                            throw new InvalidOperationException
                            (
                                $"Cannot close generic implementation '{implementationType}' for matched target type '{matchedTargetType}'. " +
                                $"Implementation generic parameter count is {implementationTypeGenericParametersLength}, but target generic argument count is {genericArguments.Length}."
                            );
                        }

                        try
                        {
                            implementationType = implementationType.MakeGenericType(genericArguments);
                        }
                        catch (ArgumentException e)
                        {
                            throw new InvalidOperationException($"Cannot close generic implementation '{implementationType}' for matched target type '{matchedTargetType}' because target generic arguments do not satisfy implementation generic constraints.", e);
                        }
                    }

                    if (predicate == null)
                        cache.TryAdd(targetType, new TypeResolution(matchedTargetType, implementationType));

                    return true;
                }
            }

            if (predicate == null)
                cache.TryAdd(targetType, null);

            matchedTargetType = null;
            implementationType = null;

            return false;
        }
    }
}
