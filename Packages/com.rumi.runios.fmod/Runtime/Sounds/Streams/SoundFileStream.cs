#nullable enable
using FMOD;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;

namespace RuniOS.Sounds.Streams
{
    sealed class SoundFileStream : IDisposable
    {
        static readonly FILE_OPEN_CALLBACK openCallback = Open;
        static readonly FILE_CLOSE_CALLBACK closeCallback = Close;
        static readonly FILE_READ_CALLBACK readCallback = Read;
        static readonly FILE_SEEK_CALLBACK seekCallback = Seek;

        readonly Stream stream;
        readonly object streamLock = new();
        readonly long offset;
        readonly uint length;
        readonly bool leaveOpen;

        GCHandle handle;
        bool isDisposed;

        public SoundFileStream(Stream stream, bool leaveOpen)
        {
            ExceptionUtility.ThrowIfArgumentNull(stream, nameof(stream));

            if (!stream.CanRead)
                throw new ArgumentException("The stream must be readable.", nameof(stream));

            if (!stream.CanSeek)
                throw new ArgumentException("The stream must be seekable.", nameof(stream));

            long offset = stream.Position;
            long length = stream.Length - offset;

            if (length < 0 || length > uint.MaxValue)
                throw new ArgumentException("The remaining stream length must fit in an unsigned 32-bit integer.", nameof(stream));

            this.stream = stream;
            this.offset = offset;
            this.length = (uint)length;
            this.leaveOpen = leaveOpen;
            handle = GCHandle.Alloc(this);
        }

        public CREATESOUNDEXINFO CreateExInfo() => new()
        {
            cbsize = Marshal.SizeOf<CREATESOUNDEXINFO>(),
            fileuseropen = openCallback,
            fileuserclose = closeCallback,
            fileuserread = readCallback,
            fileuserseek = seekCallback,
            fileuserdata = GCHandle.ToIntPtr(handle)
        };

        public void Dispose()
        {
            lock (streamLock)
            {
                if (isDisposed)
                    return;

                isDisposed = true;

                if (handle.IsAllocated)
                    handle.Free();

                if (!leaveOpen)
                    stream.Dispose();
            }
        }

        static RESULT Open(IntPtr name, ref uint fileSize, ref IntPtr fileHandle, IntPtr userData)
        {
            try
            {
                return Get(userData).Open(ref fileSize, ref fileHandle);
            }
            catch
            {
                fileSize = 0;
                fileHandle = IntPtr.Zero;
                return RESULT.ERR_FILE_BAD;
            }
        }

        static RESULT Close(IntPtr fileHandle, IntPtr userData) => RESULT.OK;

        static RESULT Read(IntPtr fileHandle, IntPtr buffer, uint sizeBytes, ref uint bytesRead, IntPtr userData)
        {
            try
            {
                return Get(fileHandle).Read(buffer, sizeBytes, ref bytesRead);
            }
            catch
            {
                bytesRead = 0;
                return RESULT.ERR_FILE_BAD;
            }
        }

        static RESULT Seek(IntPtr fileHandle, uint position, IntPtr userData)
        {
            try
            {
                return Get(fileHandle).Seek(position);
            }
            catch
            {
                return RESULT.ERR_FILE_COULDNOTSEEK;
            }
        }

        static SoundFileStream Get(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                throw new ArgumentException("The FMOD stream handle is invalid.", nameof(handle));

            return (SoundFileStream)GCHandle.FromIntPtr(handle).Target!;
        }

        // ReSharper disable RedundantAssignment
        RESULT Open(ref uint fileSize, ref IntPtr fileHandle)
        {
            lock (streamLock)
            {
                ThrowIfDisposed();
                stream.Position = offset;
                fileSize = length;
                fileHandle = GCHandle.ToIntPtr(handle);
                return RESULT.OK;
            }
        }
        // ReSharper restore RedundantAssignment

        RESULT Read(IntPtr buffer, uint sizeBytes, ref uint bytesRead)
        {
            if (sizeBytes > int.MaxValue || (sizeBytes > 0 && buffer == IntPtr.Zero))
                return RESULT.ERR_FILE_BAD;

            lock (streamLock)
            {
                ThrowIfDisposed();

                int requested = (int)sizeBytes;
                byte[] bytes = ArrayPool<byte>.Shared.Rent(requested);

                try
                {
                    int totalBytesRead = 0;

                    while (totalBytesRead < requested)
                    {
                        int read = stream.Read(bytes, totalBytesRead, requested - totalBytesRead);
                        if (read == 0)
                            break;

                        totalBytesRead += read;
                    }

                    if (totalBytesRead > 0)
                        Marshal.Copy(bytes, 0, buffer, totalBytesRead);

                    bytesRead = (uint)totalBytesRead;
                    return totalBytesRead == requested ? RESULT.OK : RESULT.ERR_FILE_EOF;
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(bytes);
                }
            }
        }

        RESULT Seek(uint position)
        {
            lock (streamLock)
            {
                ThrowIfDisposed();

                if (position > length)
                    return RESULT.ERR_FILE_COULDNOTSEEK;

                stream.Position = offset + position;
                return RESULT.OK;
            }
        }

        void ThrowIfDisposed()
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(SoundFileStream));
        }
    }
}
