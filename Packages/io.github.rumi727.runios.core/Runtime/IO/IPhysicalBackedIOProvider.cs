#nullable enable
namespace RuniOS.IO
{
    /// <summary>
    /// Provides access to a target whose entries are backed by the physical local file system.<br/>
    /// 항목이 실제 로컬 파일 시스템을 바탕으로 하는 대상에 대한 접근을 제공합니다.
    /// </summary>
    /// <remarks>
    /// Implementing this interface does not imply that the provider is writable. Check <see cref="IWritableIOProvider"/> separately.<br/>
    /// 이 인터페이스를 구현한다고 해서 쓰기 가능한 프로바이더라는 뜻은 아닙니다. <see cref="IWritableIOProvider"/>는 따로 확인해야 합니다.
    /// </remarks>
    public interface IPhysicalBackedIOProvider : IIOProvider
    {
        /// <summary>
        /// Gets the physical path that the specified provider-relative path refers to.<br/>
        /// 지정한 프로바이더 기준 경로가 가리키는 실제 경로를 가져옵니다.
        /// </summary>
        /// <param name="path">
        /// The provider-relative path to convert.<br/>
        /// 변환할 프로바이더 기준 경로입니다.
        /// </param>
        /// <returns>
        /// The physical path that <paramref name="path"/> refers to. The file or directory may not exist.<br/>
        /// <paramref name="path"/>가 가리키는 실제 경로를 반환합니다. 파일이나 폴더가 없을 수 있습니다.
        /// </returns>
        PhysicalPath GetPhysicalPath(RuniPath path);
    }
}
