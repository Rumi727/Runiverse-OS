#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using RuniOS.Services;
using RuniOS.Resource;
using System.IO;
using System.Runtime.InteropServices;

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

        static unsafe List<Bookmark> ReadQuickAccess()
        {
            List<Bookmark> result = new();

            void* folder = null;
            void* enumerator = null;
            bool shouldUninitializeCom = false;

            try
            {
                int initializeResult = Native.CoInitializeEx(IntPtr.Zero, coInitMultithreaded);
                if (initializeResult >= 0)
                    shouldUninitializeCom = true;
                else if (initializeResult != rpcEChangedMode)
                    return result;

                Guid shellItemId = iidShellItem;
                if (Native.SHCreateItemFromParsingName(quickAccessPath, IntPtr.Zero, ref shellItemId, out IntPtr folderPointer) < 0)
                    return result;

                folder = (void*)folderPointer;

                // IShellItem::BindToHandler
                Guid enumItemsHandler = bhidEnumItems;
                Guid enumShellItemsId = iidEnumShellItems;
                void* rawEnumerator;
                if (((delegate* unmanaged[Stdcall]<void*, void*, Guid*, Guid*, void**, int>)GetMethod(folder, 3))(folder, null, &enumItemsHandler, &enumShellItemsId, &rawEnumerator) < 0)
                    return result;

                enumerator = rawEnumerator;

                while (true)
                {
                    // IEnumShellItems::Next
                    void* item;
                    uint fetched;
                    if (((delegate* unmanaged[Stdcall]<void*, uint, void**, uint*, int>)GetMethod(enumerator, 3))(enumerator, 1, &item, &fetched) != 0 || fetched == 0)
                        break;

                    try
                    {
                        if (TryGetDisplayName(item, sigdnFileSysPath, out string path) && Directory.Exists(path)
                            && TryGetDisplayName(item, sigdnNormalDisplay, out string name))
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
                if (enumerator != null)
                    Release(enumerator);

                if (folder != null)
                    Release(folder);

                if (shouldUninitializeCom)
                    Native.CoUninitialize();
            }

            return result;
        }

        static unsafe void* GetMethod(void* instance, int index) => (*(void***)instance)[index];

        // IUnknown::Release
        static unsafe void Release(void* instance) => ((delegate* unmanaged[Stdcall]<void*, uint>)GetMethod(instance, 2))(instance);

        // IShellItem::GetDisplayName
        static unsafe bool TryGetDisplayName(void* item, uint sigdn, out string name)
        {
            name = string.Empty;

            char* buffer;
            if (((delegate* unmanaged[Stdcall]<void*, uint, char**, int>)GetMethod(item, 5))(item, sigdn, &buffer) < 0 || buffer == null)
                return false;

            try
            {
                name = new string(buffer);
                return name.Length > 0;
            }
            finally
            {
                Marshal.FreeCoTaskMem((IntPtr)buffer);
            }
        }

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
            public static partial int SHCreateItemFromParsingName(string path, IntPtr bindContext, ref Guid iid, out IntPtr item);
        }
    }
}
