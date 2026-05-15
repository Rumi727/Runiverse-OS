#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Linq.Async;
using System.IO;
using System.Threading;
using UnityEngine.Android;

namespace RuniOS.IO
{
    /// <summary>
    /// Provides read-only access to Android streaming assets through the <see cref="IIOProvider"/> API.<br/>
    /// The provider uses Android <c>AssetManager</c> to open assets and enumerate asset directories.
    /// <br/><br/>
    /// Android StreamingAssets에 대한 읽기 전용 접근을 <see cref="IIOProvider"/> API로 제공합니다.<br/>
    /// 내부적으로 Android <c>AssetManager</c>를 사용해 에셋을 열고 에셋 디렉터리를 열거합니다.
    /// </summary>
    public class AndroidStreamingIOProvider(RuniPath rootPath = default) : IIOProvider
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            _assetManager = null;
            if (Application.platform != RuntimePlatform.Android)
                return;

            AndroidJavaObject? currentActivity = AndroidApplication.currentActivity;
            if (currentActivity == null)
                return;

            _assetManager = currentActivity.Call<AndroidJavaObject>("getAssets");
        }

        static AndroidJavaObject? _assetManager;
        static AndroidJavaObject assetManager => _assetManager ??= CreateAssetManager();

        static AndroidJavaObject CreateAssetManager()
        {
            if (Application.platform != RuntimePlatform.Android)
                throw new PlatformNotSupportedException($"{nameof(AndroidStreamingIOProvider)} can only run on Android.");

            AndroidJavaObject? currentActivity = AndroidApplication.currentActivity;
            if (currentActivity == null)
                throw new InvalidOperationException("Unable to resolve Android currentActivity.");

            AndroidJavaObject? manager = currentActivity.Call<AndroidJavaObject>("getAssets");
            return manager ?? throw new InvalidOperationException("Unable to resolve Android AssetManager.");
        }

        /// <inheritdoc/>
        public IONode rootNode => new IONode(this);

        readonly RuniPath rootPath = rootPath;

        /// <inheritdoc/>
        public bool isIndependent => false;

        /// <inheritdoc/>
        public IIOProvider Recreate(RuniPath path) => path.IsEmpty() ? this : new AndroidStreamingIOProvider(rootPath.Combine(path));

        /// <inheritdoc/>
        public bool IsSameTarget(IIOProvider other) => other is AndroidStreamingIOProvider otherAndroid && rootPath == otherAndroid.rootPath;

        #region Entry
        /// <inheritdoc/>
        public UniTask<IOEntry?> GetEntry(RuniPath path, CancellationToken cancellationToken = default) => UniTask.RunOnThreadPool(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            RuniPath actualPath = rootPath.Combine(path);
            string relativePath = actualPath.value;
            string[] children = ListAssets(relativePath);
            string name = path.GetFileName();

            if (path.IsEmpty() || children.Length > 0)
            {
                return new IOEntry
                {
                    path = path,
                    metaData = new IOMetaData
                    {
                        name = name,
                        attributes = FileAttributes.Directory | FileAttributes.ReadOnly
                    },
                    isDirectory = true
                };
            }

            if (!FileExists(relativePath))
                return (IOEntry?)null;

            return new IOEntry
            {
                path = path,
                metaData = new IOMetaData
                {
                    name = name,
                    size = TryGetFileSize(relativePath),
                    attributes = FileAttributes.ReadOnly
                },
                isDirectory = false
            };
        }, cancellationToken: cancellationToken);

        /// <inheritdoc/>
        public IUniTaskAsyncEnumerable<IOEntry> EnumerateEntries(RuniPath path, bool recursive, CancellationToken cancellationToken = default)
        {
            return Enumerate(rootPath, path, recursive, cancellationToken).EnumerateOnThreadPool(cancellationToken: cancellationToken);

            static IEnumerable<IOEntry> Enumerate(RuniPath rootPath, RuniPath path, bool recursive, CancellationToken cancellationToken)
            {
                string[] rootItems = ListAssets(rootPath.Combine(path).value);
                if (rootItems.Length <= 0)
                    yield break;

                Queue<(RuniPath path, string[] items)> queue = new Queue<(RuniPath path, string[] items)>();
                queue.Enqueue((path, rootItems));

                while (queue.Count > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    (RuniPath currentPath, string[] items) = queue.Dequeue();

                    foreach (string item in items)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        RuniPath entryPath = currentPath.Combine(item);
                        RuniPath actualEntryPath = rootPath.Combine(entryPath);
                        string[] childItems = ListAssets(actualEntryPath.value);
                        bool isDirectory = childItems.Length > 0;

                        IOEntry entry;
                        if (isDirectory)
                        {
                            entry = new IOEntry
                            {
                                path = entryPath,
                                metaData = new IOMetaData
                                {
                                    name = item,
                                    attributes = FileAttributes.Directory | FileAttributes.ReadOnly
                                },
                                isDirectory = true
                            };
                        }
                        else
                        {
                            entry = new IOEntry
                            {
                                path = entryPath,
                                metaData = new IOMetaData
                                {
                                    name = item,
                                    size = TryGetFileSize(actualEntryPath.value),
                                    attributes = FileAttributes.ReadOnly
                                },
                                isDirectory = false
                            };
                        }

                        yield return entry;

                        if (recursive && isDirectory)
                            queue.Enqueue((entryPath, childItems));
                    }
                }
            }
        }
        #endregion

        #region Read
        /// <inheritdoc/>
        public UniTask<Stream> OpenRead(RuniPath path, CancellationToken cancellationToken = default) => UniTask.RunOnThreadPool<Stream>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            string relativePath = rootPath.Combine(path).value;
            if (!TryOpenAsset(relativePath, out AndroidJavaObject? inputStream) || inputStream == null)
                throw new FileNotFoundException($"Streaming asset not found: '{relativePath}'.", relativePath);

            return new AndroidAssetStream(inputStream, TryGetFileSize(relativePath));
        }, cancellationToken: cancellationToken);
        #endregion

        static void EnsureJniThreadAttached() => AndroidJNI.AttachCurrentThread();

        static string[] ListAssets(string relativePath)
        {
            EnsureJniThreadAttached();
            return assetManager.Call<string[]>("list", relativePath) ?? [];
        }

        static bool TryOpenAsset(string relativePath, out AndroidJavaObject? stream)
        {
            EnsureJniThreadAttached();
            try
            {
                stream = assetManager.Call<AndroidJavaObject>("open", relativePath);
                return stream != null;
            }
            catch (AndroidJavaException exception) when (IsMissingAssetError(exception))
            {
                stream = null;
                return false;
            }
        }

        static bool FileExists(string relativePath)
        {
            if (!TryOpenAsset(relativePath, out AndroidJavaObject? stream) || stream == null)
                return false;

            EnsureJniThreadAttached();
            stream.Call("close");
            stream.Dispose();
            return true;
        }

        static long? TryGetFileSize(string relativePath)
        {
            EnsureJniThreadAttached();
            try
            {
                using AndroidJavaObject? fileDescriptor = assetManager.Call<AndroidJavaObject>("openFd", relativePath);
                return fileDescriptor?.Call<long>("getLength");
            }
            catch (AndroidJavaException exception) when (IsAssetFdUnsupportedError(exception))
            {
                return null;
            }
        }

        static bool IsMissingAssetError(AndroidJavaException exception)
        {
            string message = exception.Message;
            return message.Contains("FileNotFoundException", StringComparison.Ordinal) ||
                message.Contains("No such file", StringComparison.OrdinalIgnoreCase);
        }

        static bool IsAssetFdUnsupportedError(AndroidJavaException exception)
        {
            string message = exception.Message;
            return message.Contains("FileNotFoundException", StringComparison.Ordinal) ||
                message.Contains("can not be opened as a file descriptor", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("probably compressed", StringComparison.OrdinalIgnoreCase);
        }

        sealed class AndroidAssetStream(AndroidJavaObject javaInputStream, long? length) : Stream
        {
            long _position;
            bool _disposed;

            public override bool CanRead => !_disposed;
            public override bool CanSeek => false;
            public override bool CanWrite => false;

            public override long Length => length ?? throw new NotSupportedException("Length is not available for this Android asset stream.");

            public override long Position
            {
                get => _position;
                set => throw new NotSupportedException();
            }

            public override void Flush() { }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(AndroidAssetStream));
                if (buffer == null)
                    throw new ArgumentNullException(nameof(buffer));
                if (offset < 0)
                    throw new ArgumentOutOfRangeException(nameof(offset));
                if (count < 0)
                    throw new ArgumentOutOfRangeException(nameof(count));

                if (offset + count > buffer.Length)
                    throw new ArgumentException("The sum of offset and count is larger than the buffer length.");

                if (count == 0)
                    return 0;

                EnsureJniThreadAttached();
                int bytesRead = javaInputStream.Call<int>("read", buffer, offset, count);
                if (bytesRead < 0)
                    return 0;

                _position += bytesRead;
                return bytesRead;
            }

            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

            protected override void Dispose(bool disposing)
            {
                if (_disposed)
                    return;

                if (disposing)
                {
                    EnsureJniThreadAttached();
                    javaInputStream.Call("close");
                    javaInputStream.Dispose();
                }

                _disposed = true;
                base.Dispose(disposing);
            }
        }

        void IDisposable.Dispose() { }
    }
}
