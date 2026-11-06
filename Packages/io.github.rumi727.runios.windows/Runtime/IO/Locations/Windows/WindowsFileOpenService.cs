#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Services;
using RuniOS.Resource;
using System.Diagnostics;

namespace RuniOS.IO.Locations.Windows
{
    /// <summary>
    /// Opens files with the application associated with them in the Windows shell.<br/>
    /// Windows 셸에 연결된 프로그램으로 파일을 엽니다.
    /// </summary>
    public sealed class WindowsFileOpenService : IFileOpenService
    {
        WindowsFileOpenService() { }

        /// <summary>
        /// Gets the Windows file open service instance.<br/>
        /// Windows 파일 열기 서비스 인스턴스를 가져옵니다.
        /// </summary>
        public static WindowsFileOpenService instance { get; } = new();

        /// <summary>
        /// The identifier of the Windows file open service.<br/>
        /// Windows 파일 열기 서비스의 식별자입니다.
        /// </summary>
        public static readonly Identifier id = new Identifier("runios", "file-open/windows");
        Identifier IService.id => id;

        /// <summary>
        /// Opens the specified file with its associated application.<br/>
        /// 지정한 파일을 연결된 프로그램으로 엽니다.
        /// </summary>
        public static UniTask OpenFile(PhysicalPath path)
        {
            using Process? process = Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            return UniTask.CompletedTask;
        }

        UniTask IFileOpenService.OpenFile(PhysicalPath path) => OpenFile(path);

#if UNITY_EDITOR_WIN || !UNITY_EDITOR
        [Unity.Scripting.LifecycleManagement.OnAssemblyLoaded]
        static void OnAssemblyLoaded() => ServiceRegistry.Register(instance);

        [Unity.Scripting.LifecycleManagement.OnAssemblyUnloading]
        static void OnAssemblyUnloading() => ServiceRegistry.Unregister(instance);
#endif
    }
}
