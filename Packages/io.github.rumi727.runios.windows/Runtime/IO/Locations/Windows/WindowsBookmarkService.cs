#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using RuniOS.Services;
using RuniOS.Resource;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

// ReSharper disable CommentTypo
namespace RuniOS.IO.Locations.Windows
{
    /// <summary>
    /// Provides the folders listed in File Explorer's Quick access, enumerated through the shell namespace.<br/>
    /// 셸 네임스페이스로 열거한 파일 탐색기의 빠른 실행 폴더를 제공합니다.
    /// </summary>
    public sealed partial class WindowsBookmarkService : IBookmarkService
    {
        const string quickAccessPath = "shell:::{679f85cb-0220-4080-b29b-5540cc05aab6}";

        const uint sigdnNormalDisplay = 0x00000000;
        const uint sigdnFileSysPath = 0x80058000;

        const uint coInitMultithreaded = 0;
        const int rpcEChangedMode = unchecked((int)0x80010106);

        static readonly Guid iidShellItem = new("43826D1E-E718-42EE-BC55-A1E261C37BFE");
        static readonly Guid iidEnumShellItems = new("70629033-E363-4A28-A567-0DB78006E6D7");
        static readonly Guid bhidEnumItems = new("94F60519-2850-4924-AA5A-D15E84868039");

        WindowsBookmarkService() { }

        /// <summary>
        /// Gets the Windows bookmark service instance.<br/>
        /// Windows 즐겨찾기 서비스 인스턴스를 가져옵니다.
        /// </summary>
        public static WindowsBookmarkService instance { get; } = new();

        /// <summary>
        /// The identifier of the Windows bookmark service.<br/>
        /// Windows 즐겨찾기 서비스의 식별자입니다.
        /// </summary>
        public static readonly Identifier id = new Identifier("runios", "bookmark/windows");
        Identifier IService.id => id;

        /// <summary>
        /// Enumerates the existing folders of Quick access. Pinned and frequent folders are not distinguished.<br/>
        /// 빠른 실행의 존재하는 폴더를 열거합니다. 고정된 폴더와 자주 사용하는 폴더는 구분하지 않습니다.
        /// </summary>
        /// <returns>
        /// A sequence containing the bookmarks, or an empty sequence when Quick access cannot be enumerated.<br/>
        /// 즐겨찾기를 포함하는 시퀀스를 반환하며, 빠른 실행을 열거할 수 없으면 빈 시퀀스를 반환합니다.
        /// </returns>
        public static IEnumerable<Bookmark> EnumerateBookmarks() => ReadQuickAccess();

        IUniTaskAsyncEnumerable<Bookmark> IBookmarkService.EnumerateBookmarks() => EnumerateBookmarks().ToUniTaskAsyncEnumerable();

        static List<Bookmark> ReadQuickAccess()
        {
            List<Bookmark> result = new();

            IShellItem? folder = null;
            IEnumShellItems? enumerator = null;
            bool shouldUninitializeCom = false;

            try
            {
                int initializeResult = Native.CoInitializeEx(IntPtr.Zero, coInitMultithreaded);
                if (initializeResult >= 0)
                    shouldUninitializeCom = true;
                else if (initializeResult != rpcEChangedMode)
                    return result;

                if (Native.SHCreateItemFromParsingName(quickAccessPath, IntPtr.Zero, in iidShellItem, out folder) < 0 || folder == null)
                    return result;

                if (folder.BindToHandler(IntPtr.Zero, in bhidEnumItems, in iidEnumShellItems, out enumerator) < 0 || enumerator == null)
                    return result;

                while (enumerator.Next(1, out IShellItem? item, out uint fetched) == 0 && fetched != 0 && item != null)
                {
                    try
                    {
                        if (item.GetDisplayName(sigdnFileSysPath, out string? path) >= 0 && !string.IsNullOrEmpty(path) && Directory.Exists(path)
                            && item.GetDisplayName(sigdnNormalDisplay, out string? name) >= 0 && !string.IsNullOrEmpty(name))
                            result.Add(new Bookmark(name, (PhysicalPath)path));
                    }
                    catch (ArgumentException) { }
                    catch (IOException) { }
                    catch (NotSupportedException) { }
                    finally
                    {
                        Release(item);
                    }
                }
            }
            catch (DllNotFoundException) { }
            catch (EntryPointNotFoundException) { }
            finally
            {
                Release(enumerator);
                Release(folder);

                // 래퍼가 해제되기 전에 COM이 먼저 종료되지 않도록 명시적으로 해제합니다.
                if (shouldUninitializeCom)
                    Native.CoUninitialize();
            }

            return result;
        }

        static void Release(object? comObject) => (comObject as ComObject)?.FinalRelease();

#if UNITY_EDITOR_WIN || !UNITY_EDITOR
        [Unity.Scripting.LifecycleManagement.OnAssemblyLoaded]
        static void OnAssemblyLoaded() => ServiceRegistry.Register(instance);

        [Unity.Scripting.LifecycleManagement.OnAssemblyUnloading]
        static void OnAssemblyUnloading() => ServiceRegistry.Unregister(instance);
#endif

        static partial class Native
        {
            [LibraryImport("ole32.dll")]
            public static partial int CoInitializeEx(IntPtr reserved, uint coInit);

            [LibraryImport("ole32.dll")]
            public static partial void CoUninitialize();

            [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
            public static partial int SHCreateItemFromParsingName(string path, IntPtr bindContext, in Guid iid, [MarshalAs(UnmanagedType.Interface)] out IShellItem? item);
        }
    }

    // 메서드 순서는 vtable 순서이므로 바꾸면 안 됩니다. 사용하지 않는 뒤쪽 메서드는 생략합니다.
    [GeneratedComInterface]
    [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
    internal partial interface IShellItem
    {
        [PreserveSig]
        int BindToHandler(IntPtr bindContext, in Guid handlerId, in Guid interfaceId, [MarshalAs(UnmanagedType.Interface)] out IEnumShellItems? enumerator);

        [PreserveSig]
        int GetParent([MarshalAs(UnmanagedType.Interface)] out IShellItem? parent);

        [PreserveSig]
        int GetDisplayName(uint sigdn, [MarshalAs(UnmanagedType.LPWStr)] out string? name);
    }

    [GeneratedComInterface]
    [Guid("70629033-E363-4A28-A567-0DB78006E6D7")]
    internal partial interface IEnumShellItems
    {
        [PreserveSig]
        int Next(uint count, [MarshalAs(UnmanagedType.Interface)] out IShellItem? item, out uint fetched);
    }
}
