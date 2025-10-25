#nullable enable
// Source : https://source.dot.net/#System.Private.CoreLib/src/libraries/System.Private.CoreLib/src/System/Reflection/NullabilityInfo.cs

namespace RuniOS.Inspectors
{
    /// <summary>
    /// An enum that represents nullability state
    /// </summary> 
    public enum RuniNullabilityState
    {
        /// <summary>
        /// Nullability context not enabled (oblivious)
        /// </summary>
        Unknown,
        /// <summary>
        /// Non nullable value or reference type
        /// </summary>
        NotNull,
        /// <summary>
        /// Nullable value or reference type
        /// </summary>
        Nullable
    }
}