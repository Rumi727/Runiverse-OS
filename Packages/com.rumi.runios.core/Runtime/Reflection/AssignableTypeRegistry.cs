#nullable enable
using System.Collections.Immutable;
using System.ComponentModel;

namespace RuniOS.Reflection
{
    public sealed class AssignableTypeRegistry(params ImmutableArray<Type> baseTypes) : TypeRegistry
    {
        public ImmutableArray<Type> baseTypes { get; } = baseTypes;
        public ImmutableArray<Type> registeredTypes { get; private set; }

        public override event Action? onChanged;

        public override void Register(Type type)
        {
            if (baseTypes.Any(x => !type.IsAssignableToAny(x)))
                throw new ArgumentException($"Type {type} is not assignable from {baseTypes}");

            registeredTypes = registeredTypes.Add(type);
            onChanged?.Invoke();
        }

        public override void RegisterRange(IEnumerable<Type> types)
        {
            foreach (var item in types)
            {
                if (baseTypes.Any(x => !item.IsAssignableToAny(x)))
                    throw new ArgumentException($"Type {item} is not assignable from {baseTypes}");
            }

            registeredTypes = registeredTypes.AddRange(types);
            onChanged?.Invoke();
        }

        public override void RegisterRange(params ReadOnlySpan<Type> types)
        {
            foreach (var item in types)
            {
                if (baseTypes.Any(x => !item.IsAssignableToAny(x)))
                    throw new ArgumentException($"Type {item} is not assignable from {baseTypes}");
            }

            registeredTypes = registeredTypes.AddRange(types);
            onChanged?.Invoke();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This is for source generators only. Please use Register or RegisterRange for manual registration.")]
        public void RegisterRangeUnchecked(params ReadOnlySpan<Type> types)
        {
            registeredTypes = registeredTypes.AddRange(types);
            onChanged?.Invoke();
        }

        public override void Unregister(Type type)
        {
            registeredTypes = registeredTypes.Remove(type);
            onChanged?.Invoke();
        }

        public override void UnregisterRange(IEnumerable<Type> types)
        {
            registeredTypes = registeredTypes.RemoveRange(types);
            onChanged?.Invoke();
        }

        public override void UnregisterRange(params ReadOnlySpan<Type> types)
        {
            registeredTypes = registeredTypes.RemoveRange(types);
            onChanged?.Invoke();
        }
    }
}