#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO.Locations.Linux.DBus;
using Tmds.DBus.Protocol;

namespace RuniOS.IO.Locations.Linux
{
    /// <summary>
    /// Shows paths in the file manager through the freedesktop FileManager1 D-Bus interface.<br/>
    /// freedesktop FileManager1 D-Bus 인터페이스로 파일 관리자에서 경로를 보여 줍니다.
    /// </summary>
    public sealed partial class LinuxFileManagerProvider : IFileManagerProvider
    {
        const string fileManager1ServiceName = "org.freedesktop.FileManager1";
        const string fileManager1Path = "/org/freedesktop/FileManager1";

        LinuxFileManagerProvider() { }

        /// <summary>
        /// Gets the Linux file manager provider instance.<br/>
        /// Linux 파일 관리자 프로바이더 인스턴스를 가져옵니다.
        /// </summary>
        public static LinuxFileManagerProvider instance { get; } = new();

        /// <summary>
        /// Asynchronously opens the specified folder in the file manager.<br/>
        /// 지정한 폴더를 파일 관리자에서 비동기로 엽니다.
        /// </summary>
        public static async UniTask ShowFolder(PhysicalPath path)
        {
            FileManager1 fileManager = await GetFileManager();
            await fileManager.ShowFoldersAsync([new Uri(path).AbsoluteUri], string.Empty);
        }

        /// <summary>
        /// Asynchronously opens the folders containing the specified items in the file manager and selects the items.<br/>
        /// 지정한 항목이 들어 있는 폴더를 파일 관리자에서 비동기로 열고 항목을 선택합니다.
        /// </summary>
        public static async UniTask ShowItems(IEnumerable<PhysicalPath> paths)
        {
            FileManager1 fileManager = await GetFileManager();
            await fileManager.ShowItemsAsync(paths.Select(static x => new Uri(x).AbsoluteUri).ToArray(), string.Empty);
        }

        static async UniTask<FileManager1> GetFileManager()
        {
            DBusConnection connection = DBusConnection.Session;
            await connection.ConnectAsync();

            return new FileManager1(connection, fileManager1ServiceName, new ObjectPath(fileManager1Path));
        }

        UniTask IFileManagerProvider.ShowFolder(PhysicalPath path) => ShowFolder(path);
        UniTask IFileManagerProvider.ShowItems(IEnumerable<PhysicalPath> paths) => ShowItems(paths);

#if UNITY_EDITOR_LINUX || !UNITY_EDITOR
        [Unity.Scripting.LifecycleManagement.OnAssemblyLoaded]
        static void OnAssemblyLoaded() => IFileManagerProvider.Register(instance);

        [Unity.Scripting.LifecycleManagement.OnAssemblyUnloading]
        static void OnAssemblyUnloading() => IFileManagerProvider.Unregister(instance);
#endif
    }
}
