#nullable enable
// Source : https://source.dot.net/#System.Private.CoreLib/src/libraries/System.Private.CoreLib/src/System/Reflection/NullabilityInfoContext.cs
#pragma warning disable
// ReSharper disable all
namespace System.Reflection
{

#if !RUNI_ENGINE_DOTNET_INTERNAL && !RUNI_ENGINE_DOTNET_INTERNAL_NETSTANDARD_HELPERS 
    public 
#endif
        static class NetstandardHelpers
    {
        public static ReadOnlySpan<ParameterInfo> GetParametersAsSpan(this MethodBase metaMethod)
            => metaMethod.GetParameters();
    }
}