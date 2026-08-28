namespace RuniOS.CodeAnalysis.Generators.TypeRegistry;

/// <summary>
/// Identifies where a registry definition was discovered.<br/>
/// 레지스트리 정의를 발견한 위치를 나타냅니다.
/// </summary>
public enum RegistryOrigin
{
    /// <summary>
    /// The registry was discovered in the current compilation.<br/>
    /// 현재 컴파일에서 레지스트리를 발견했습니다.
    /// </summary>
    currentCompilation,

    /// <summary>
    /// The registry was restored from a manifest in a referenced assembly.<br/>
    /// 참조된 어셈블리의 매니페스트에서 레지스트리를 복원했습니다.
    /// </summary>
    referencedAssemblyManifest,
}
