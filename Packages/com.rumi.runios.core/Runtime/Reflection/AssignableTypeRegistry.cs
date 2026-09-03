#nullable enable
using System.Collections.Immutable;
using System.ComponentModel;

namespace RuniOS.Reflection
{
    public sealed class AssignableTypeRegistry(params ImmutableArray<Type> baseTypes) : TypeRegistry
    {
        public ImmutableArray<Type> baseTypes { get; } = baseTypes;

        public ImmutableArray<Type> registeredTypes { get; private set; }
        readonly object registeredTypesLock = new();

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

        public override void Register(Type type)
        {
            if (baseTypes.Any(x => !type.IsAssignableToAny(x)))
                throw new ArgumentException($"Type {type} is not assignable from {baseTypes}");

            lock (registeredTypesLock)
                registeredTypes = registeredTypes.Add(type);

            lock (onChangedLock)
                _onChanged?.Invoke();
        }

        public override void RegisterRange(IEnumerable<Type> types)
        {
            lock (registeredTypesLock)
            {
                foreach (var item in types)
                {
                    if (baseTypes.Any(x => !item.IsAssignableToAny(x)))
                        throw new ArgumentException($"Type {item} is not assignable from {baseTypes}");
                }

                registeredTypes = registeredTypes.AddRange(types);
            }

            lock (onChangedLock)
                _onChanged?.Invoke();
        }

        public override void RegisterRange(params ReadOnlySpan<Type> types)
        {
            lock (registeredTypesLock)
            {
                for (int i = 0; i < types.Length; i++)
                {
                    Type item = types[i];
                    if (baseTypes.Any(x => !item.IsAssignableToAny(x)))
                        throw new ArgumentException($"Type {item} is not assignable from {baseTypes}");
                }

                registeredTypes = registeredTypes.AddRange(types);
            }

            lock (onChangedLock)
                _onChanged?.Invoke();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This is for source generators only. Please use Register or RegisterRange for manual registration.")]
        public void RegisterRangeUnchecked(params ReadOnlySpan<Type> types)
        {
            lock (registeredTypesLock)
                registeredTypes = registeredTypes.AddRange(types);

            lock (onChangedLock)
                _onChanged?.Invoke();
        }

        public override void Unregister(Type type)
        {
            lock (registeredTypesLock)
                registeredTypes = registeredTypes.Remove(type);

            lock (onChangedLock)
                _onChanged?.Invoke();
        }
    }
}