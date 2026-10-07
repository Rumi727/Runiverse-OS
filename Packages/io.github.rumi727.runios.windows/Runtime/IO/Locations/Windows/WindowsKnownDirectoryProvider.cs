#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using System.IO;
using System.Runtime.InteropServices;

// ReSharper disable CommentTypo
namespace RuniOS.IO.Locations.Windows
{
    public sealed partial class WindowsKnownDirectoryProvider : IKnownDirectoryProvider
    {
        const uint kfFlagDontVerify = 0x00004000;
        const uint coInitMultithreaded = 0;
        const int rpcEChangedMode = unchecked((int)0x80010106);

        static readonly (KnownDirectoryType type, Guid id)[] knownFolders =
        [
            (KnownDirectoryType.home, new Guid("5E6C858F-0E22-4760-9AFE-EA3317B67173")),
            (KnownDirectoryType.desktop, new Guid("B4BFCC3A-DB2C-424C-B029-7FE99A87C641")),
            (KnownDirectoryType.downloads, new Guid("374DE290-123F-4565-9164-39C4925E467B")),
            (KnownDirectoryType.documents, new Guid("FDD39AD0-238F-46AF-ADB4-6C85480369C7")),
            (KnownDirectoryType.music, new Guid("4BD8D571-6D19-48D3-BE97-422220080E43")),
            (KnownDirectoryType.pictures, new Guid("33E28130-4E1E-4676-835A-98395C3BC3BB")),
            (KnownDirectoryType.videos, new Guid("18989B1D-99B5-455B-841C-AB7C74E4DDFC")),
            (KnownDirectoryType.templates, new Guid("A63293E8-664E-48DB-A079-DF759E0509F7")),
            (KnownDirectoryType.publicShare, new Guid("DFDF76A2-C82A-4D63-906A-5644AC457385")),
            (KnownDirectoryType.contacts, new Guid("56784854-C6CB-462B-8169-88E350ACB882")),
            (KnownDirectoryType.favorites, new Guid("1777F761-68AD-4D8A-87BD-30B759FA33DD")),
            (KnownDirectoryType.links, new Guid("BFB9D5E0-C6A9-404C-B2B2-AE6DB6AF4968")),
            (KnownDirectoryType.savedGames, new Guid("4C5C32FF-BB9D-43B0-B5B4-2D72E54EAAA4")),
            // The official KNOWNFOLDERID name is FOLDERID_SavedSearches.
            (KnownDirectoryType.searches, new Guid("7D1D3A04-DEBB-4115-95CF-2F29DA2920DA"))
        ];

        WindowsKnownDirectoryProvider() { }

        public static WindowsKnownDirectoryProvider instance { get; } = new();

        public static IEnumerable<KnownDirectory> EnumerateDirectories()
        {
            foreach ((KnownDirectoryType type, Guid id) in knownFolders)
            {
                if (!TryGetKnownFolderPath(id, out string path))
                    continue;

                PhysicalPath physicalPath;
                try
                {
                    physicalPath = (PhysicalPath)path;
                }
                catch (ArgumentException) { continue; }
                catch (IOException) { continue; }
                catch (NotSupportedException) { continue; }

                yield return new KnownDirectory(type, physicalPath);
            }
        }

        IUniTaskAsyncEnumerable<KnownDirectory> IKnownDirectoryProvider.EnumerateDirectories() => EnumerateDirectories().ToUniTaskAsyncEnumerable();

        static bool TryGetKnownFolderPath(Guid id, out string path)
        {
            path = string.Empty;
            IntPtr pathBuffer = IntPtr.Zero;
            bool shouldUninitializeCom = false;

            try
            {
                int initializeResult = Native.CoInitializeEx(IntPtr.Zero, coInitMultithreaded);
                if (initializeResult >= 0)
                    shouldUninitializeCom = true;
                else if (initializeResult != rpcEChangedMode)
                    return false;

                int result = Native.SHGetKnownFolderPath(ref id, kfFlagDontVerify, IntPtr.Zero, out pathBuffer);
                if (result < 0 || pathBuffer == IntPtr.Zero)
                    return false;

                path = Marshal.PtrToStringUni(pathBuffer) ?? string.Empty;
                return !string.IsNullOrEmpty(path);
            }
            catch (DllNotFoundException) { return false; }
            catch (EntryPointNotFoundException) { return false; }
            finally
            {
                if (pathBuffer != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(pathBuffer);

                if (shouldUninitializeCom)
                    Native.CoUninitialize();
            }
        }

#if UNITY_EDITOR_WIN || !UNITY_EDITOR
        [Unity.Scripting.LifecycleManagement.OnAssemblyLoaded]
        static void OnAssemblyLoaded() => IKnownDirectoryProvider.Register(instance);

        [Unity.Scripting.LifecycleManagement.OnAssemblyUnloading]
        static void OnAssemblyUnloading() => IKnownDirectoryProvider.Unregister(instance);
#endif

        static class Native
        {
            [DllImport("ole32.dll", ExactSpelling = true)]
            public static extern int CoInitializeEx(IntPtr reserved, uint coInit);

            [DllImport("ole32.dll", ExactSpelling = true)]
            public static extern void CoUninitialize();

            [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
            public static extern int SHGetKnownFolderPath(ref Guid id, uint flags, IntPtr token, out IntPtr path);
        }
    }
}