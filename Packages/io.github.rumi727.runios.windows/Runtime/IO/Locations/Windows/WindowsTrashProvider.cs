#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Providers;
using RuniOS.Resource;
using System.IO;
using System.Runtime.InteropServices;

namespace RuniOS.IO.Locations.Windows
{
    /// <summary>
    /// Moves paths to the Windows Recycle Bin.<br/>
    /// Windows 휴지통으로 경로를 보냅니다.
    /// </summary>
    public sealed partial class WindowsTrashProvider : ITrashProvider
    {
        const uint coInitApartmentThreaded = 0x2;
        const int rpcEChangedMode = unchecked((int)0x80010106);

        const uint foDelete = 0x3;

        const ushort fofSilent = 0x4;
        const ushort fofNoConfirmation = 0x10;
        const ushort fofAllowUndo = 0x40;
        const ushort fofNoErrorUI = 0x400;
        const ushort fofWantNukeWarning = 0x4000;

        const int errorCancelled = 0x4C7;

        WindowsTrashProvider() { }

        /// <summary>
        /// Gets the Windows trash provider instance.<br/>
        /// Windows 휴지통 프로바이더 인스턴스를 가져옵니다.
        /// </summary>
        public static WindowsTrashProvider instance { get; } = new();

        /// <summary>
        /// The identifier of the Windows trash provider.<br/>
        /// Windows 휴지통 프로바이더의 식별자입니다.
        /// </summary>
        public static readonly Identifier id = new Identifier("runios", "trash/windows");
        Identifier IProvider.id => id;

        /// <summary>
        /// Determines whether the drive containing the specified path has a Recycle Bin.<br/>
        /// 지정한 경로가 있는 드라이브에 휴지통이 있는지 확인합니다.
        /// </summary>
        public static UniTask<bool> CanMoveToTrash(PhysicalPath path)
        {
            // 네트워크 드라이브처럼 휴지통이 없는 드라이브에서는 실패합니다.
            // shellapi.h는 x86에서 구조체를 1바이트 단위로 묶으므로, 크기만 그에 맞춰 넘깁니다.
            Native.RecycleBinInfo info = new() { size = IntPtr.Size == 4 ? 20u : 24u };
            return UniTask.FromResult(Native.SHQueryRecycleBinW(path.GetPathRoot(), ref info) == 0);
        }

        /// <summary>
        /// Moves the specified items to the Windows Recycle Bin.<br/>
        /// 지정한 항목을 Windows 휴지통으로 보냅니다.
        /// </summary>
        /// <remarks>
        /// If an item cannot be recycled (for example, it exceeds the Recycle Bin size limit), Windows asks the user before deleting it permanently.<br/>
        /// 항목을 휴지통으로 보낼 수 없으면(예: 휴지통 크기 제한 초과) Windows가 영구 삭제하기 전에 사용자에게 묻습니다.
        /// </remarks>
        public static unsafe UniTask MoveToTrash(IEnumerable<PhysicalPath> paths)
        {
            // 경로 목록은 null 문자로 구분하고 null 문자 두 개로 끝나야 합니다. 마지막 null 문자 하나는 마샬러가 붙입니다.
            string from = string.Concat(paths.Select(static x => (string)x + '\0'));
            if (from.Length == 0)
                return UniTask.CompletedTask;

            // FOF_WANTNUKEWARNING이 없으면 휴지통으로 보낼 수 없는 항목이 묻지 않고 영구 삭제됩니다.
            ushort flags = fofAllowUndo | fofNoConfirmation | fofSilent | fofNoErrorUI | fofWantNukeWarning;

            bool shouldUninitializeCom = false;
            try
            {
                int initializeResult = Native.CoInitializeEx(IntPtr.Zero, coInitApartmentThreaded);
                if (initializeResult >= 0)
                    shouldUninitializeCom = true;
                else if (initializeResult != rpcEChangedMode)
                    Marshal.ThrowExceptionForHR(initializeResult);

                int result;
                bool aborted;
                if (IntPtr.Size == 4)
                {
                    fixed (char* fromPointer = from)
                    {
                        Native.FileOperation32 operation = new() { function = foDelete, from = fromPointer, flags = flags };
                        result = Native.SHFileOperationW(ref operation);
                        aborted = operation.anyOperationsAborted != 0;
                    }
                }
                else
                {
                    fixed (char* fromPointer = from)
                    {
                        Native.FileOperation64 operation = new() { function = foDelete, from = fromPointer, flags = flags };
                        result = Native.SHFileOperationW(ref operation);
                        aborted = operation.anyOperationsAborted != 0;
                    }
                }

                if (aborted || result == errorCancelled)
                    throw new OperationCanceledException("Moving the items to the Recycle Bin was aborted.");

                // 반환값은 HRESULT가 아니라 DE_ 코드 또는 Win32 오류 코드입니다.
                if (result != 0)
                    throw new IOException($"Could not move the items to the Recycle Bin (error code 0x{result:X}).");
            }
            finally
            {
                if (shouldUninitializeCom)
                    Native.CoUninitialize();
            }

            return UniTask.CompletedTask;
        }

        UniTask<bool> ITrashProvider.CanMoveToTrash(PhysicalPath path) => CanMoveToTrash(path);
        UniTask ITrashProvider.MoveToTrash(IEnumerable<PhysicalPath> paths) => MoveToTrash(paths);

#if UNITY_EDITOR_WIN || !UNITY_EDITOR
        [Unity.Scripting.LifecycleManagement.OnAssemblyLoaded]
        static void OnAssemblyLoaded() => ProviderRegistry.Register(instance);

        [Unity.Scripting.LifecycleManagement.OnAssemblyUnloading]
        static void OnAssemblyUnloading() => ProviderRegistry.Unregister(instance);
#endif

        static unsafe partial class Native
        {
            // SHQUERYRBINFO
            [StructLayout(LayoutKind.Sequential)]
            public struct RecycleBinInfo
            {
                public uint size;
                public long totalSize;
                public long itemCount;
            }

            // SHFILEOPSTRUCTW (x86, 1바이트 단위 정렬)
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct FileOperation32
            {
                public IntPtr window;
                public uint function;
                public char* from;
                public char* to;
                public ushort flags;
                public int anyOperationsAborted;
                public IntPtr nameMappings;
                public char* progressTitle;
            }

            // SHFILEOPSTRUCTW (x64)
            [StructLayout(LayoutKind.Sequential)]
            public struct FileOperation64
            {
                public IntPtr window;
                public uint function;
                public char* from;
                public char* to;
                public ushort flags;
                public int anyOperationsAborted;
                public IntPtr nameMappings;
                public char* progressTitle;
            }

            [LibraryImport("ole32.dll")]
            public static partial int CoInitializeEx(IntPtr reserved, uint coInit);

            [LibraryImport("ole32.dll")]
            public static partial void CoUninitialize();

            [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
            public static partial int SHQueryRecycleBinW(string rootPath, ref RecycleBinInfo info);

            [LibraryImport("shell32.dll")]
            public static partial int SHFileOperationW(ref FileOperation32 operation);

            [LibraryImport("shell32.dll")]
            public static partial int SHFileOperationW(ref FileOperation64 operation);
        }
    }
}
