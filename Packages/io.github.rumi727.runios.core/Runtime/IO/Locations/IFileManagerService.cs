#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Services;

namespace RuniOS.IO.Locations
{
    /// <summary>
    /// Shows physical paths in the system file manager.<br/>
    /// 실제 경로를 시스템 파일 관리자에서 보여 줍니다.
    /// </summary>
    public interface IFileManagerService : IService
    {
        /// <summary>
        /// Opens the specified folder in the system file manager.<br/>
        /// 지정한 폴더를 시스템 파일 관리자에서 엽니다.
        /// </summary>
        UniTask ShowFolder(PhysicalPath path);

        /// <summary>
        /// Opens the folders containing the specified items in the system file manager and selects the items.<br/>
        /// 지정한 항목이 들어 있는 폴더를 시스템 파일 관리자에서 열고 항목을 선택합니다.
        /// </summary>
        UniTask ShowItems(IEnumerable<PhysicalPath> paths);
    }
}
