#nullable enable
// Source : https://source.dot.net/#System.Private.CoreLib/src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/IsExternalInit.cs
using System.ComponentModel;

// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Reserved to be used by the compiler for tracking metadata.
    /// This class should not be used by developers in source code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
#if !RUNI_ENGINE_DOTNET_INTERNAL && !RUNI_ENGINE_DOTNET_INTERNAL_IS_EXTERNAL_INIT 
    public 
#endif
    static class IsExternalInit { }
}