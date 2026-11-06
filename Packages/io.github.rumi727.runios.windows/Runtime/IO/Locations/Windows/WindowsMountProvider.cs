#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using RuniOS.Providers;
using RuniOS.Resource;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

// ReSharper disable UnusedMethodReturnValue.Local
namespace RuniOS.IO.Locations.Windows
{
    /// <summary>
    /// Enumerates the volume mounts of the system by using the Windows volume APIs.<br/>
    /// Windows 볼륨 API를 사용해 시스템의 볼륨 마운트를 열거합니다.
    /// </summary>
    public sealed partial class WindowsMountProvider : IMountProvider
    {
        const int errorMoreData = 234;
        const uint fileReadOnlyVolume = 0x00080000;
        const uint driveRemote = 4;
        const uint maxVolumePathBufferLength = 1024 * 1024;

        static readonly IntPtr invalidHandle = new(-1);

        WindowsMountProvider() { }

        /// <summary>
        /// Gets the Windows mount provider instance.<br/>
        /// Windows 마운트 프로바이더 인스턴스를 가져옵니다.
        /// </summary>
        public static WindowsMountProvider instance { get; } = new();

        /// <summary>
        /// The identifier of the Windows mount provider.<br/>
        /// Windows 마운트 프로바이더의 식별자입니다.
        /// </summary>
        public static readonly Identifier id = new Identifier("runios", "mount/windows");
        Identifier IProvider.id => id;

        /// <summary>
        /// Enumerates the volume mounts of the system.<br/>
        /// 시스템의 볼륨 마운트를 열거합니다.
        /// </summary>
        /// <returns>
        /// A collection containing the volume mounts, or an empty collection when the volume APIs are unavailable or no volume is found.<br/>
        /// 볼륨 마운트를 포함하는 컬렉션을 반환하며, 볼륨 API를 사용할 수 없거나 볼륨을 찾지 못하면 빈 컬렉션을 반환합니다.
        /// </returns>
        public static IEnumerable<WindowsMount> EnumerateMounts()
        {
            List<WindowsMount> mounts = [];
            IntPtr searchHandle = invalidHandle;

            try
            {
                char[] volumeName = new char[1024];
                searchHandle = Native.FindFirstVolumeW(volumeName, (uint)volumeName.Length);
                if (searchHandle == invalidHandle)
                    return mounts;

                do
                {
                    string currentVolumeName = ReadString(volumeName);
                    if (!string.IsNullOrEmpty(currentVolumeName))
                        AddVolumeMounts(currentVolumeName, mounts);

                    Array.Clear(volumeName);
                }
                while (Native.FindNextVolumeW(searchHandle, volumeName, (uint)volumeName.Length));
            }
            catch (DllNotFoundException) { }
            catch (EntryPointNotFoundException) { }
            finally
            {
                if (searchHandle != invalidHandle)
                {
                    try
                    {
                        Native.FindVolumeClose(searchHandle);
                    }
                    catch (DllNotFoundException) { }
                    catch (EntryPointNotFoundException) { }
                }
            }

            return mounts;
        }

        IUniTaskAsyncEnumerable<Mount> IMountProvider.EnumerateMounts() => EnumerateMounts().ToUniTaskAsyncEnumerable();

        static void AddVolumeMounts(string volumeName, List<WindowsMount> mounts)
        {
            char[] fileSystemName = new char[256];
            if
            (
                !Native.GetVolumeInformationW
                (
                    volumeName,
                    IntPtr.Zero,
                    0,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    out uint fileSystemFlags,
                    fileSystemName,
                    (uint)fileSystemName.Length
                )
            )
                return;

            string fileSystemTypeText = ReadString(fileSystemName);
            string? fileSystemType = fileSystemTypeText.Length == 0 ? null : fileSystemTypeText;
            bool isReadOnly = (fileSystemFlags & fileReadOnlyVolume) != 0;

            if (!TryGetVolumePaths(volumeName, out string[] mountPaths))
                return;

            foreach (string mountPath in mountPaths)
            {
                if (IsNetworkMountPoint(mountPath))
                    continue;

                PhysicalPath physicalPath;
                try
                {
                    physicalPath = PhysicalPath.From(mountPath);
                }
                catch (ArgumentException) { continue; }
                catch (IOException) { continue; }
                catch (NotSupportedException) { continue; }

                mounts.Add(new WindowsMount(volumeName, physicalPath, fileSystemType, isReadOnly));
            }
        }

        static bool TryGetVolumePaths(string volumeName, out string[] mountPaths)
        {
            mountPaths = [];

            bool firstCallSucceeded = Native.GetVolumePathNamesForVolumeNameW(volumeName, null, 0, out uint requiredLength);
            if (!firstCallSucceeded && Marshal.GetLastWin32Error() != errorMoreData)
                return false;

            if (firstCallSucceeded && requiredLength == 0)
                return true;

            uint bufferLength = requiredLength == 0 ? 256 : requiredLength;
            while (bufferLength <= maxVolumePathBufferLength)
            {
                char[] buffer = new char[(int)bufferLength];
                if (Native.GetVolumePathNamesForVolumeNameW(volumeName, buffer, bufferLength, out uint returnedLength))
                {
                    if (returnedLength > bufferLength)
                        return false;

                    return TryParseMultiString(buffer, (int)returnedLength, out mountPaths);
                }

                if (Marshal.GetLastWin32Error() != errorMoreData)
                    return false;

                if (returnedLength > bufferLength)
                    bufferLength = returnedLength;
                else if (bufferLength < maxVolumePathBufferLength)
                    bufferLength = Math.Min(bufferLength * 2, maxVolumePathBufferLength);
                else
                    return false;
            }

            return false;
        }

        // 네이티브가 채운 버퍼에서 첫 null 문자 앞까지만 문자열로 읽습니다.
        static string ReadString(ReadOnlySpan<char> buffer)
        {
            int end = buffer.IndexOf('\0');
            return new string(end < 0 ? buffer : buffer[..end]);
        }

        static bool TryParseMultiString(char[] buffer, int length, out string[] values)
        {
            List<string> result = [];
            int index = 0;

            while (index < length)
            {
                if (buffer[index] == '\0')
                {
                    if (index == 0 && (length < 2 || buffer[1] != '\0'))
                    {
                        values = [];
                        return false;
                    }

                    values = [.. result];
                    return true;
                }

                int end = Array.IndexOf(buffer, '\0', index, length - index);
                if (end < 0)
                {
                    values = [];
                    return false;
                }

                result.Add(new string(buffer, index, end - index));
                index = end + 1;
            }

            values = [];
            return false;
        }

        static bool IsNetworkMountPoint(string mountPath)
        {
            if (mountPath.StartsWith(@"\\", StringComparison.Ordinal))
                return true;

            try
            {
                return Native.GetDriveTypeW(mountPath) == driveRemote;
            }
            catch (DllNotFoundException) { return false; }
            catch (EntryPointNotFoundException) { return false; }
        }

#if UNITY_EDITOR_WIN || !UNITY_EDITOR
        [Unity.Scripting.LifecycleManagement.OnAssemblyLoaded]
        static void OnAssemblyLoaded() => ProviderRegistry.Register(instance);

        [Unity.Scripting.LifecycleManagement.OnAssemblyUnloading]
        static void OnAssemblyUnloading() => ProviderRegistry.Unregister(instance);
#endif

        static partial class Native
        {
            [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
            public static partial IntPtr FindFirstVolumeW([Out, MarshalUsing(CountElementName = nameof(bufferLength))] char[] volumeName, uint bufferLength);

            [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static partial bool FindNextVolumeW(IntPtr findVolume, [Out, MarshalUsing(CountElementName = nameof(bufferLength))] char[] volumeName, uint bufferLength);

            [LibraryImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static partial bool FindVolumeClose(IntPtr findVolume);

            [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static partial bool GetVolumePathNamesForVolumeNameW
            (
                string volumeName,
                [Out, MarshalUsing(CountElementName = nameof(bufferLength))] char[]? volumePathNames,
                uint bufferLength,
                out uint returnedLength
            );

            [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static partial bool GetVolumeInformationW
            (
                string rootPathName,
                IntPtr volumeNameBuffer,
                uint volumeNameSize,
                IntPtr volumeSerialNumber,
                IntPtr maximumComponentLength,
                out uint fileSystemFlags,
                [Out, MarshalUsing(CountElementName = nameof(fileSystemNameSize))] char[] fileSystemNameBuffer,
                uint fileSystemNameSize
            );

            [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
            public static partial uint GetDriveTypeW(string rootPathName);
        }
    }
}
