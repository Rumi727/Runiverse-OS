#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Services;
using RuniOS.Resource;
using System.Diagnostics;

namespace RuniOS.IO.Locations.Linux
{
    /// <summary>
    /// Opens files with the default application through xdg-open.<br/>
    /// xdg-open으로 파일을 기본 프로그램에서 엽니다.
    /// </summary>
    public sealed partial class LinuxFileOpenService : IFileOpenService
    {
        LinuxFileOpenService() { }

        /// <summary>
        /// Gets the Linux file open service instance.<br/>
        /// Linux 파일 열기 서비스 인스턴스를 가져옵니다.
        /// </summary>
        public static LinuxFileOpenService instance { get; } = new();

        /// <summary>
        /// The identifier of the Linux file open service.<br/>
        /// Linux 파일 열기 서비스의 식별자입니다.
        /// </summary>
        public static readonly Identifier id = new Identifier("runios", "file-open/linux");
        Identifier IService.id => id;

        /// <summary>
        /// Opens the specified file with the default application.<br/>
        /// 지정한 파일을 기본 프로그램으로 엽니다.
        /// </summary>
        public static UniTask OpenFile(PhysicalPath path)
        {
            ProcessStartInfo startInfo = new("xdg-open") { UseShellExecute = false };
            startInfo.ArgumentList.Add(path);

            using Process? process = Process.Start(startInfo);
            return UniTask.CompletedTask;
        }

        UniTask IFileOpenService.OpenFile(PhysicalPath path) => OpenFile(path);

#if UNITY_EDITOR_LINUX || !UNITY_EDITOR
        [Unity.Scripting.LifecycleManagement.OnAssemblyLoaded]
        static void OnAssemblyLoaded() => ServiceRegistry.Register(instance);

        [Unity.Scripting.LifecycleManagement.OnAssemblyUnloading]
        static void OnAssemblyUnloading() => ServiceRegistry.Unregister(instance);
#endif
    }
}
