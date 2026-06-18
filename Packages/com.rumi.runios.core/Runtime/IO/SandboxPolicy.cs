#nullable enable
namespace RuniOS.IO
{
    /// <summary>
    /// Defines how <see cref="PhysicalIOProvider"/> validates provider-relative paths before physical file-system access.<br/>
    /// <see cref="PhysicalIOProvider"/>가 실제 파일 시스템 접근 전에 프로바이더 기준 경로를 검증하는 방식을 정의합니다.
    /// </summary>
    public enum SandboxPolicy
    {
        /// <summary>
        /// Enables sandbox validation before physical file-system access.<br/>
        /// 실제 파일 시스템 접근 전에 샌드박스 검증을 수행합니다.
        /// </summary>
        Enabled,

        /// <summary>
        /// Disables sandbox validation before physical file-system access.<br/>
        /// 실제 파일 시스템 접근 전 샌드박스 검증을 수행하지 않습니다.
        /// </summary>
        Disabled
    }
}
