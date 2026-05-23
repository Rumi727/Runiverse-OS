#nullable enable
// Source : https://source.dot.net/#System.Private.CoreLib/src/libraries/System.Private.CoreLib/src/System/Reflection/NullabilityInfo.cs
using System.Collections.Immutable;

#pragma warning disable
namespace RuniOS.Reflection
{
    /// <summary>
    /// A class that represents nullability info
    /// </summary> 
    public sealed class NullabilityInfo(Type type, NullabilityState readState, NullabilityState writeState, NullabilityInfo? elementType, ImmutableArray<NullabilityInfo> typeArguments)
    {
        public NullabilityInfo(Type type, NullabilityState state) : this(type, state, state, null, ImmutableArray<NullabilityInfo>.Empty) { }
        public NullabilityInfo(Type type, NullabilityState state, NullabilityInfo? elementType = null) : this(type, state, state, elementType, ImmutableArray<NullabilityInfo>.Empty) { }
        public NullabilityInfo(Type type, NullabilityState readState, NullabilityState writeState, NullabilityInfo? elementType = null) : this(type, readState, writeState, elementType, ImmutableArray<NullabilityInfo>.Empty) { }
        public NullabilityInfo(Type type, NullabilityState readState, NullabilityState writeState, NullabilityInfo? elementType, params NullabilityInfo[] typeArguments) : this(type, readState, writeState, elementType, typeArguments.ToImmutableArray()) { }
        public NullabilityInfo(Type type, NullabilityState readState, NullabilityState writeState, NullabilityInfo? elementType, IEnumerable<NullabilityInfo> typeArguments) : this(type, readState, writeState, elementType, typeArguments.ToImmutableArray()) { }

        /// <summary>
        /// The <see cref="System.Type" /> of the member or generic parameter to which this NullabilityInfo belongs
        /// </summary>
        public Type type { get; } = type;

        /// <summary>
        /// The nullability read state of the member
        /// </summary>
        public NullabilityState readState { get; internal set; } = readState;

        /// <summary>
        /// The nullability write state of the member
        /// </summary>
        public NullabilityState writeState { get; internal set; } = writeState;

        /// <summary>
        /// If the member type is an array, gives the <see cref="NullabilityInfo" /> of the elements of the array, null otherwise
        /// </summary>
        public NullabilityInfo? elementType { get; } = elementType;

        /// <summary>
        /// If the member type is a generic type, gives the array of <see cref="NullabilityInfo" /> for each type parameter
        /// </summary>
        public ImmutableArray<NullabilityInfo> genericTypeArguments { get; } = [..typeArguments];
    }
}