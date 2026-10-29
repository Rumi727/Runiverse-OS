#nullable enable
using Cysharp.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace RuniOS.IO.Locations.Windows
{
    /// <summary>
    /// Shows paths in Windows Explorer.<br/>
    /// Windows 탐색기에서 경로를 보여 줍니다.
    /// </summary>
    public sealed partial class WindowsFileManagerProvider : IFileManagerProvider
    {
        const uint coInitApartmentThreaded = 0x2;
        const int rpcEChangedMode = unchecked((int)0x80010106);

        WindowsFileManagerProvider() { }

        /// <summary>
        /// Gets the Windows file manager provider instance.<br/>
        /// Windows 파일 관리자 프로바이더 인스턴스를 가져옵니다.
        /// </summary>
        public static WindowsFileManagerProvider instance { get; } = new();

        /// <summary>
        /// Opens the specified folder in Windows Explorer.<br/>
        /// 지정한 폴더를 Windows 탐색기에서 엽니다.
        /// </summary>
        public static UniTask ShowFolder(PhysicalPath path)
        {
            // 폴더의 기본 연결 앱이 아닌 탐색기로 열도록 explorer.exe를 직접 실행합니다.
            using Process? process = Process.Start(new ProcessStartInfo("explorer.exe", $"\"{(string)path}\"") { UseShellExecute = true });
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// Opens the folders containing the specified items in Windows Explorer and selects the items.<br/>
        /// 지정한 항목이 들어 있는 폴더를 Windows 탐색기에서 열고 항목을 선택합니다.
        /// </summary>
        public static UniTask ShowItems(IEnumerable<PhysicalPath> paths)
        {
            // SHOpenFolderAndSelectItems는 한 폴더 안의 항목만 선택하므로, 상위 폴더별로 나눠서 엽니다.
            foreach (IGrouping<string?, PhysicalPath> group in paths.GroupBy(static x => Path.GetDirectoryName((string)x)))
            {
                // 드라이브 루트처럼 상위 폴더가 없는 항목은 그 폴더 자체를 엽니다.
                if (group.Key == null)
                {
                    foreach (PhysicalPath path in group)
                        ShowFolder(path);

                    continue;
                }

                OpenFolderAndSelectItems(group.Key, group);
            }

            return UniTask.CompletedTask;
        }

        static void OpenFolderAndSelectItems(string folder, IEnumerable<PhysicalPath> items)
        {
            bool shouldUninitializeCom = false;
            IntPtr folderId = IntPtr.Zero;
            List<IntPtr> itemIds = [];

            try
            {
                int initializeResult = Native.CoInitializeEx(IntPtr.Zero, coInitApartmentThreaded);
                if (initializeResult >= 0)
                    shouldUninitializeCom = true;
                else if (initializeResult != rpcEChangedMode)
                    Marshal.ThrowExceptionForHR(initializeResult);

                folderId = Native.ILCreateFromPathW(folder);
                if (folderId == IntPtr.Zero)
                    throw new DirectoryNotFoundException($"Could not find the folder '{folder}'.");

                foreach (PhysicalPath item in items)
                {
                    IntPtr itemId = Native.ILCreateFromPathW(item);
                    if (itemId != IntPtr.Zero)
                        itemIds.Add(itemId);
                }

                // 선택할 항목은 폴더 기준의 상대 ID로 넘겨야 합니다.
                IntPtr[] childIds = itemIds.Select(Native.ILFindLastID).ToArray();
                Marshal.ThrowExceptionForHR(Native.SHOpenFolderAndSelectItems(folderId, (uint)childIds.Length, childIds, 0));
            }
            finally
            {
                foreach (IntPtr itemId in itemIds)
                    Native.ILFree(itemId);

                Native.ILFree(folderId);

                if (shouldUninitializeCom)
                    Native.CoUninitialize();
            }
        }

        UniTask IFileManagerProvider.ShowFolder(PhysicalPath path) => ShowFolder(path);
        UniTask IFileManagerProvider.ShowItems(IEnumerable<PhysicalPath> paths) => ShowItems(paths);

#if UNITY_EDITOR_WIN || !UNITY_EDITOR
        [Unity.Scripting.LifecycleManagement.OnAssemblyLoaded]
        static void OnAssemblyLoaded() => IFileManagerProvider.Register(instance);

        [Unity.Scripting.LifecycleManagement.OnAssemblyUnloading]
        static void OnAssemblyUnloading() => IFileManagerProvider.Unregister(instance);
#endif

        static class Native
        {
            [DllImport("ole32.dll", ExactSpelling = true)]
            public static extern int CoInitializeEx(IntPtr reserved, uint coInit);

            [DllImport("ole32.dll", ExactSpelling = true)]
            public static extern void CoUninitialize();

            [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
            public static extern IntPtr ILCreateFromPathW(string path);

            [DllImport("shell32.dll", ExactSpelling = true)]
            public static extern IntPtr ILFindLastID(IntPtr id);

            [DllImport("shell32.dll", ExactSpelling = true)]
            public static extern void ILFree(IntPtr id);

            [DllImport("shell32.dll", ExactSpelling = true)]
            public static extern int SHOpenFolderAndSelectItems(IntPtr folderId, uint count, IntPtr[] itemIds, uint flags);
        }
    }
}
