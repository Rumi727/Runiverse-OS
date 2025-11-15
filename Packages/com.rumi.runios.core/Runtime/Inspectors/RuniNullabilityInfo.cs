#nullable enable
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

// Source : https://source.dot.net/#System.Private.CoreLib/src/libraries/System.Private.CoreLib/src/System/Reflection/NullabilityInfo.cs

namespace RuniOS.Inspectors
{
    /// <summary>
    /// A class that represents nullability info
    /// </summary> 
    public sealed class RuniNullabilityInfo
    {
        public RuniNullabilityInfo(Type type, RuniNullabilityState state) : this(type, state, state, null, ImmutableArray<RuniNullabilityInfo>.Empty) { }
        public RuniNullabilityInfo(Type type, RuniNullabilityState state, RuniNullabilityInfo? elementType = null) : this(type, state, state, elementType, ImmutableArray<RuniNullabilityInfo>.Empty) { }
        public RuniNullabilityInfo(Type type, RuniNullabilityState readState, RuniNullabilityState writeState, RuniNullabilityInfo? elementType = null) : this(type, readState, writeState, elementType, ImmutableArray<RuniNullabilityInfo>.Empty) { }
        public RuniNullabilityInfo(Type type, RuniNullabilityState readState, RuniNullabilityState writeState, RuniNullabilityInfo? elementType, params RuniNullabilityInfo[] typeArguments) : this(type, readState, writeState, elementType, typeArguments.ToImmutableArray()) { }
        public RuniNullabilityInfo(Type type, RuniNullabilityState readState, RuniNullabilityState writeState, RuniNullabilityInfo? elementType, IEnumerable<RuniNullabilityInfo> typeArguments) : this(type, readState, writeState, elementType, typeArguments.ToImmutableArray()) { }
        
        public RuniNullabilityInfo(Type type, RuniNullabilityState readState, RuniNullabilityState writeState, RuniNullabilityInfo? elementType, ImmutableArray<RuniNullabilityInfo> typeArguments)
        {
            Type = type;
            
            this.readState = readState;
            this.writeState = writeState;
            this.elementType = elementType;
            
            genericTypeArguments = typeArguments.ToImmutableArray();
        }
 
        /// <summary>
        /// The <see cref="System.Type" /> of the member or generic parameter to which this NullabilityInfo belongs
        /// </summary>
        public Type Type { get; }
        
        /// <summary>
        /// The nullability read state of the member
        /// </summary>
        public RuniNullabilityState readState { get; }
        
        /// <summary>
        /// The nullability write state of the member
        /// </summary>
        public RuniNullabilityState writeState { get; }
        
        /// <summary>
        /// If the member type is an array, gives the <see cref="RuniNullabilityInfo" /> of the elements of the array, null otherwise
        /// </summary>
        public RuniNullabilityInfo? elementType { get; }
        
        /// <summary>
        /// If the member type is a generic type, gives the array of <see cref="RuniNullabilityInfo" /> for each type parameter
        /// </summary>
        public ImmutableArray<RuniNullabilityInfo> genericTypeArguments { get; }
        
        [return: NotNullIfNotNull("nullabilityInfo")]
        public static implicit operator RuniNullabilityInfo?(NullabilityInfo? nullabilityInfo)
        {
            if (nullabilityInfo == null)
                return null;
            
            return new RuniNullabilityInfo(nullabilityInfo.Type, (RuniNullabilityState)nullabilityInfo.ReadState, (RuniNullabilityState)nullabilityInfo.WriteState, nullabilityInfo.ElementType, nullabilityInfo.GenericTypeArguments.Select(x => (RuniNullabilityInfo)x));
        }
    }
}