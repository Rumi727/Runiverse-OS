#nullable enable
#if UNITY_ANDROID || UNITY_EDITOR
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using System.IO;
using System.Text;
using UnityEngine.Android;
using UnityEngine.Networking;

namespace RuniOS.IO
{
    public partial class StreamingIOEntry
    {
        // Exists 같은 탐색 API들은 AI가 작성했으며 작동을 보장하지 않음.
        // ReSharper disable once ClassNeverInstantiated.Local
        class AndroidImpl : IImpl
        {
            [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
            static void Init() => _assetManager = AndroidApplication.currentActivity.Call<AndroidJavaObject>("getAssets");


            static AndroidJavaObject? _assetManager;
            static AndroidJavaObject assetManager
            {
                get
                {
                    if (_assetManager == null)
                        throw new InvalidOperationException("Android AssetManager has not been initialized. Ensure Init() is called and running on an Android device.");

                    return assetManager;
                }
            }

            public AndroidImpl(FilePath targetPath) => this.targetPath = targetPath;

            readonly FilePath targetPath;

            public UniTask<bool> DirectoryExists() => UniTask.RunOnThreadPool(() =>
            {
                AndroidJNI.AttachCurrentThread();

                string[]? list = assetManager.Call<string[]>("list", targetPath);
                return list != null && list.Length > 0;
            });

            public UniTask<bool> FileExists() => UniTask.RunOnThreadPool(() =>
            {
                AndroidJNI.AttachCurrentThread();
                try
                {
                    using AndroidJavaObject? s = assetManager.Call<AndroidJavaObject>("open", targetPath);
                    return s != null;
                }
                catch
                {
                    return false;
                }
            });

            public IUniTaskAsyncEnumerable<string> GetDirectories() => Enumerate(targetPath, false, true, false);
            public IUniTaskAsyncEnumerable<FilePath> GetAllDirectories() => Enumerate(targetPath, false, true, true).Select(PathUtility.ToPath);

            public IUniTaskAsyncEnumerable<string> GetFiles() => Enumerate(targetPath, true, false, false);
            public IUniTaskAsyncEnumerable<FileMetaData> GetFilesWithMetaData() => GetFiles().Select(x => InternalGetFileMetaData(targetPath + x));

            public IUniTaskAsyncEnumerable<string> GetFiles(WildcardPatterns wildcardPatterns) => GetFiles().Where(wildcardPatterns.IsMatch);
            public IUniTaskAsyncEnumerable<FileMetaData> GetFilesWithMetaData(WildcardPatterns wildcardPatterns) => GetFiles(wildcardPatterns).Select(x => InternalGetFileMetaData(targetPath + x));

            public IUniTaskAsyncEnumerable<FilePath> GetAllFiles() => Enumerate(targetPath, true, false, true).Select(PathUtility.ToPath);
            public IUniTaskAsyncEnumerable<(FilePath relativePath, FileMetaData metaData)> GetAllFilesWithMetaData() => GetFiles().Select(x => (x.ToPath(), InternalGetFileMetaData(targetPath + x)));

            public IUniTaskAsyncEnumerable<FilePath> GetAllFiles(WildcardPatterns wildcardPatterns) => Enumerate(targetPath, true, false, true).Where(wildcardPatterns.IsMatch).Select(PathUtility.ToPath);
            public IUniTaskAsyncEnumerable<(FilePath relativePath, FileMetaData metaData)> GetAllFilesWithMetaData(WildcardPatterns wildcardPatterns) => GetFiles(wildcardPatterns).Select(x => (x.ToPath(), InternalGetFileMetaData(targetPath + x)));

            public UniTask<byte[]> ReadAllBytes() => UnityWebRequest.Get(targetPath).SendWebRequest().ToUniTask().ContinueWith(x => x.downloadHandler.data);
            public async UniTask<string> ReadAllText() => Encoding.UTF8.GetString(await ReadAllBytes());
            public IUniTaskAsyncEnumerable<string> ReadLines() => UniTaskAsyncEnumerable.Create<string>(async (writer, _) =>
            {
                IEnumerable<string> lines = Encoding.UTF8.GetString(await ReadAllBytes()).GetLines();
                foreach (string? line in lines)
                    await writer.YieldAsync(line);
            });

            public async UniTask<Stream> OpenRead() => new MemoryStream(await ReadAllBytes());

            public UniTask<FileMetaData> GetFileMetaData() => UniTask.RunOnThreadPool(() => InternalGetFileMetaData(targetPath));

            // 이게 대체 뭐임 ㅅ1ㅂ??
            static IUniTaskAsyncEnumerable<string> Enumerate(FilePath targetPath, bool files, bool dirs, bool recursive) => UniTaskAsyncEnumerable.Create<string>(async (writer, token) =>
            {
                await UniTask.SwitchToThreadPool();
                
                Queue<(string rel, string display)> q = new Queue<(string rel, string display)>();
                q.Enqueue((targetPath, ""));
                
                while (q.Count > 0)
                {
                    token.ThrowIfCancellationRequested();
                    AndroidJNI.AttachCurrentThread();
                    
                    (string curRel, string curDisp) = q.Dequeue();
                    string[]? items = assetManager.Call<string[]>("list", curRel);
                    if (items == null) continue;
                    foreach (var item in items)
                    {
                        string nextRel = string.IsNullOrEmpty(curRel) ? item : $"{curRel}/{item}";
                        string nextDisp = string.IsNullOrEmpty(curDisp) ? item : $"{curDisp}/{item}";
                        string[]? sub = assetManager.Call<string[]>("list", nextRel);
                        
                        bool isDir = sub != null && sub.Length > 0;
                        if (isDir)
                        {
                            if (dirs)
                                await writer.YieldAsync(nextDisp);
                            
                            if (recursive)
                                q.Enqueue((nextRel, nextDisp));
                        }
                        else if (files)
                            await writer.YieldAsync(nextDisp);
                    }
                }
            });
            
            static FileMetaData InternalGetFileMetaData(FilePath targetPath)
            {
                AndroidJNI.AttachCurrentThread();
                
                long len = 0;
                try
                {
                    using var fd = assetManager.Call<AndroidJavaObject>("openFd", targetPath);
                    len = fd.Call<long>("getLength");
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                
                return new FileMetaData(targetPath.GetFileName(), len, DateTime.MinValue);
            }
        }
    }
}
#endif