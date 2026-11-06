#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Services;

namespace RuniOS.IO.Locations
{
    /// <summary>
    /// Opens physical files with the application associated with them by the system.<br/>
    /// 실제 파일을 시스템에 연결된 기본 프로그램으로 엽니다.
    /// </summary>
    public interface IFileOpenService : IService
    {
        /// <summary>
        /// Opens the specified file with its associated application.<br/>
        /// 지정한 파일을 연결된 프로그램으로 엽니다.
        /// </summary>
        UniTask OpenFile(PhysicalPath path);
    }
}
