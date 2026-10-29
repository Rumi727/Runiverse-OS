#nullable enable
using Cysharp.Threading.Tasks;

namespace RuniOS.IO.Locations
{
    /// <summary>
    /// Shows physical paths in the system file manager.<br/>
    /// 실제 경로를 시스템 파일 관리자에서 보여 줍니다.
    /// </summary>
    public interface IFileManagerProvider
    {
        public static IFileManagerProvider? instance { get; private set; } = null;

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

        public static void Register(IFileManagerProvider provider)
        {
            if (instance != null)
                throw new InvalidOperationException("The IFileManagerProvider instance must be globally unique!");

            instance = provider;
        }

        public static void Unregister(IFileManagerProvider provider)
        {
            if (instance != provider)
                throw new InvalidOperationException("This is an unregistered IFileManagerProvider instance!");

            instance = null;
        }
    }
}
