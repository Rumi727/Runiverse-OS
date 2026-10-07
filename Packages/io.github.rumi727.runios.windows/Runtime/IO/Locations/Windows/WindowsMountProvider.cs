#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace RuniOS.IO.Locations.Windows
{
    public sealed partial class WindowsMountProvider : IMountProvider
    {
        const int errorMoreData = 234;
        const uint fileReadOnlyVolume = 0x00080000;
        const uint driveRemote = 4;
        const uint maxVolumePathBufferLength = 1024 * 1024;

        static readonly IntPtr invalidHandle = new(-1);

        WindowsMountProvider() { }

        public static WindowsMountProvider instance { get; } = new();

        public static IEnumerable<WindowsMount> EnumerateMounts()
        {
            List<WindowsMount> mounts = [];
            IntPtr searchHandle = invalidHandle;

            try
            {
                StringBuilder volumeName = new(1024);
                searchHandle = Native.FindFirstVolumeW(volumeName, (uint)volumeName.Capacity);
                if (searchHandle == invalidHandle)
                    return mounts;

                do
                {
                    string currentVolumeName = volumeName.ToString();
                    if (!string.IsNullOrEmpty(currentVolumeName))
                        AddVolumeMounts(currentVolumeName, mounts);

                    volumeName.Clear();
                }
                while (Native.FindNextVolumeW(searchHandle, volumeName, (uint)volumeName.Capacity));
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
            StringBuilder fileSystemName = new(256);
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
                    (uint)fileSystemName.Capacity
                )
            )
                return;

            string? fileSystemType = fileSystemName.Length == 0 ? null : fileSystemName.ToString();
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
        static void OnAssemblyLoaded() => IMountProvider.Register(instance);

        [Unity.Scripting.LifecycleManagement.OnAssemblyUnloading]
        static void OnAssemblyUnloading() => IMountProvider.Unregister(instance);
#endif

        static class Native
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
            public static extern IntPtr FindFirstVolumeW(StringBuilder volumeName, uint bufferLength);

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool FindNextVolumeW(IntPtr findVolume, StringBuilder volumeName, uint bufferLength);

            [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool FindVolumeClose(IntPtr findVolume);

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GetVolumePathNamesForVolumeNameW
            (
                string volumeName,
                [Out, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U2, SizeParamIndex = 2)] char[]? volumePathNames,
                uint bufferLength,
                out uint returnedLength
            );

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GetVolumeInformationW
            (
                string rootPathName,
                IntPtr volumeNameBuffer,
                uint volumeNameSize,
                IntPtr volumeSerialNumber,
                IntPtr maximumComponentLength,
                out uint fileSystemFlags,
                StringBuilder fileSystemNameBuffer,
                uint fileSystemNameSize
            );

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
            public static extern uint GetDriveTypeW(string rootPathName);
        }
    }
}