#nullable enable
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace RuniOS.Reflection
{
    // RuniOS.CodeAnalysis는 `RuniOS.Reflection.AttributedTypeRegistry`2` 원본 정의와 `[0]=TBase`, `[1]=TAttribute` 순서를 전제로 합니다.
    public sealed class AttributedTypeRegistry<TBase, TAttribute> : TypeRegistry where TAttribute : TypeRegistrationAttribute
    {
        readonly record struct TypeResolution(Type matchedTargetType, Type implementationType);

        readonly Dictionary<Type, List<TAttribute>> registrationsByImplementationType = [];
        volatile ConcurrentDictionary<Type, TypeResolution?> resolutionCache = new();
        readonly object registrationLock = new();

        public ImmutableArray<RegistrationEntry<TAttribute>> registrationEntries
        {
            get
            {
                lock (registrationLock)
                {
                    if (registrationSnapshot.IsDefault)
                    {
                        registrationSnapshot =
                        [
                            ..registrationsByImplementationType.SelectMany(pair => pair.Value.Select(attribute => new RegistrationEntry<TAttribute>(pair.Key, attribute)))
                                .OrderByTypes(x => x.attribute.targetType, x => x.attribute.priority)
                        ];
                    }

                    return registrationSnapshot;
                }
            }
        }
        ImmutableArray<RegistrationEntry<TAttribute>> registrationSnapshot;


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
            if ((typeof(TBase).IsAbstract && typeof(TBase) == implementationType) || !typeof(TBase).IsAssignableFrom(implementationType))
                return;

            TAttribute[] attributes = implementationType.GetCustomAttributes<TAttribute>().ToArray();
            lock (registrationLock)
            {
                if (!registrationsByImplementationType.TryGetValue(implementationType, out List<TAttribute> list))
                    registrationsByImplementationType[implementationType] = list = [];

                list.AddRange(attributes);

                registrationSnapshot = default;
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

                registrationSnapshot = default;
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

                registrationSnapshot = default;
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
            foreach (RegistrationEntry<TAttribute> registration in registrationEntries)
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
