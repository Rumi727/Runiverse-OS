#nullable enable
using Cysharp.Threading.Tasks;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace RuniOS.IO.Locations.Linux
{
    /// <summary>
    /// Moves paths to the trash according to the freedesktop Trash specification.<br/>
    /// freedesktop 휴지통 규격에 따라 경로를 휴지통으로 보냅니다.
    /// </summary>
    public sealed partial class LinuxTrashProvider : ITrashProvider
    {
        const uint directoryMode = 0x1C0; // 0700
        const int writeAndExecuteAccess = 0x2 | 0x1; // W_OK | X_OK
        const int crossDeviceError = 18; // EXDEV

        LinuxTrashProvider() { }

        /// <summary>
        /// Gets the Linux trash provider instance.<br/>
        /// Linux 휴지통 프로바이더 인스턴스를 가져옵니다.
        /// </summary>
        public static LinuxTrashProvider instance { get; } = new();

        static string homeTrash => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Trash");

        /// <summary>
        /// Determines whether the specified path has a trash directory on the same mount.<br/>
        /// 지정한 경로와 같은 마운트에 휴지통 디렉터리가 있는지 확인합니다.
        /// </summary>
        public static UniTask<bool> CanMoveToTrash(PhysicalPath path)
        {
            if (GetTrashDirectory(path, LinuxMountProvider.EnumerateMounts().ToList(), out _) is not { } trash)
                return UniTask.FromResult(false);

            // 휴지통이 아직 없으면 조회만으로 디렉터리를 만들지 않도록, 만들 수 있는지만 확인합니다.
            if (Directory.Exists(trash))
                return UniTask.FromResult(!IsSymbolicLink(trash) && Native.access(trash, writeAndExecuteAccess) == 0);

            string? parent = Path.GetDirectoryName(trash);
            return UniTask.FromResult(parent != null && Native.access(parent, writeAndExecuteAccess) == 0);
        }

        /// <summary>
        /// Moves the specified items to the trash.<br/>
        /// 지정한 항목을 휴지통으로 보냅니다.
        /// </summary>
        /// <exception cref="IOException">
        /// Thrown when an item has no trash directory on its mount or cannot be renamed into it. Items before it have already been moved.<br/>
        /// 항목의 마운트에 휴지통 디렉터리가 없거나 휴지통으로 이름을 바꿀 수 없을 때 발생합니다. 그 앞의 항목은 이미 이동되었습니다.
        /// </exception>
        public static UniTask MoveToTrash(IEnumerable<PhysicalPath> paths)
        {
            List<LinuxMount> mounts = LinuxMountProvider.EnumerateMounts().ToList();
            foreach (PhysicalPath path in paths)
            {
                if (GetTrashDirectory(path, mounts, out string? topDirectory) is not { } trash)
                    throw new IOException($"The item '{(string)path}' cannot be moved to the trash.");

                MoveToTrash(path, trash, topDirectory);
            }

            return UniTask.CompletedTask;
        }

        static void MoveToTrash(PhysicalPath path, string trash, string? topDirectory)
        {
            if (Directory.Exists(trash) && IsSymbolicLink(trash))
                throw new IOException($"The trash directory '{trash}' is a symbolic link.");

            string files = Path.Combine(trash, "files");
            string info = Path.Combine(trash, "info");

            CreateDirectory(trash);
            CreateDirectory(files);
            CreateDirectory(info);

            // 마운트 루트의 휴지통은 매체를 다른 곳에 마운트해도 복원되도록 상대 경로를 기록합니다.
            string originalPath = topDirectory != null ? Path.GetRelativePath(topDirectory, path) : path;
            string infoText = "[Trash Info]\n" +
                              $"Path={Uri.EscapeDataString(originalPath).Replace("%2F", "/")}\n" +
                              $"DeletionDate={DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)}\n";

            string name = Path.GetFileName((string)path);
            string nameWithoutExtension = Path.GetFileNameWithoutExtension(name);
            string extension = Path.GetExtension(name);

            for (int i = 1; ; i++)
            {
                string trashName = i == 1 ? name : $"{nameWithoutExtension}.{i}{extension}";
                string infoPath = Path.Combine(info, trashName + ".trashinfo");
                string filePath = Path.Combine(files, trashName);

                if (File.Exists(infoPath) || File.Exists(filePath) || Directory.Exists(filePath))
                    continue;

                // 규격에 따라 info 파일을 원자적으로 새로 만들어 이름을 선점합니다.
                FileStream stream;
                try
                {
                    stream = new FileStream(infoPath, FileMode.CreateNew, FileAccess.Write);
                }
                catch (IOException) when (File.Exists(infoPath))
                {
                    continue;
                }

                using (StreamWriter writer = new(stream, new UTF8Encoding(false)))
                    writer.Write(infoText);

                // File.Move와 Directory.Move는 다른 파티션이면 복사 후 삭제로 대신할 수 있으므로 rename(2)을 직접 호출합니다.
                if (Native.rename(path, filePath) != 0)
                {
                    int error = Marshal.GetLastWin32Error();
                    File.Delete(infoPath);

                    if (error == crossDeviceError)
                        throw new IOException($"The item '{(string)path}' is not on the same file system as the trash directory '{trash}'.");

                    throw new IOException($"Could not move the item '{(string)path}' to the trash (errno {error}).");
                }

                return;
            }
        }

        static string? GetTrashDirectory(PhysicalPath path, List<LinuxMount> mounts, out string? topDirectory)
        {
            topDirectory = null;

            // 항목의 디렉터리 항목은 상위 디렉터리에 있으므로, 이름을 바꿀 수 있는지는 상위 디렉터리의 마운트로 판단합니다.
            string? parent = Path.GetDirectoryName((string)path);
            if (parent == null || FindMount((PhysicalPath)parent, mounts) is not { } mount)
                return null;

            string home = homeTrash;
            if (FindMount((PhysicalPath)Path.Combine(home, "files"), mounts)?.mountId == mount.mountId)
                return home;

            if (mount.isSystemInternal || LinuxMountUtility.IsNetworkFileSystem(mount))
                return null;

            // 관리자가 만드는 $topdir/.Trash/$uid는 sticky 비트 확인이 필요하므로 쓰지 않고 $topdir/.Trash-$uid만 씁니다.
            topDirectory = mount.mountPoint;
            return Path.Combine(topDirectory, $".Trash-{Native.getuid()}");
        }

        static LinuxMount? FindMount(PhysicalPath path, List<LinuxMount> mounts)
        {
            LinuxMount? result = null;
            foreach (LinuxMount mount in mounts)
            {
                // 같은 마운트 지점에 겹쳐 마운트되면 나중 항목이 보이는 마운트이므로 길이가 같아도 덮어씁니다.
                if (path.StartsWith(mount.mountPoint) && (result == null || mount.mountPoint.length >= result.mountPoint.length))
                    result = mount;
            }

            return result;
        }

        static void CreateDirectory(string path)
        {
            if (Native.mkdir(path, directoryMode) != 0 && !Directory.Exists(path))
                throw new IOException($"Could not create the trash directory '{path}' (errno {Marshal.GetLastWin32Error()}).");
        }

        static bool IsSymbolicLink(string path) => new FileInfo(path).LinkTarget != null;

        UniTask<bool> ITrashProvider.CanMoveToTrash(PhysicalPath path) => CanMoveToTrash(path);
        UniTask ITrashProvider.MoveToTrash(IEnumerable<PhysicalPath> paths) => MoveToTrash(paths);

#if UNITY_EDITOR_LINUX || !UNITY_EDITOR
        [Unity.Scripting.LifecycleManagement.OnAssemblyLoaded]
        static void OnAssemblyLoaded() => ITrashProvider.Register(instance);

        [Unity.Scripting.LifecycleManagement.OnAssemblyUnloading]
        static void OnAssemblyUnloading() => ITrashProvider.Unregister(instance);
#endif

        // ReSharper disable InconsistentNaming
        static class Native
        {
            [DllImport("libc")]
            public static extern uint getuid();

            [DllImport("libc", SetLastError = true)]
            public static extern int access(string path, int mode);

            [DllImport("libc", SetLastError = true)]
            public static extern int mkdir(string path, uint mode);

            [DllImport("libc", SetLastError = true)]
            public static extern int rename(string oldPath, string newPath);
        }
    }
}
