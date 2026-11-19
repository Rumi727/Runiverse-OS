# 8. Android 에셋: `AndroidStreamingIOProvider`

> 기준 커밋: `b3e1a343`. [← 목차](README.md) · 이전: [7. StreamingAssets](07-streaming-assets.md) · 다음: [9. 가상 파일 시스템](09-virtual-fs.md)

Android에서 `StreamingAssets`의 파일은 APK(zip) 안의 `assets/` 항목으로 들어갑니다. `Application.streamingAssetsPath`는 `jar:file://…!/assets` 형태라 `System.IO`로 열 수 없고, Unity는 `UnityWebRequest`로 읽기를 권합니다. 하지만 그 방법은 파일 하나씩 요청하는 것만 되고 **디렉터리를 열거할 수 없습니다**. 리소스 팩은 폴더를 훑어 파일을 찾으므로 열거가 꼭 필요합니다.

[`AndroidStreamingIOProvider`](../../Runtime/IO/StreamingAssets/Android/AndroidStreamingIOProvider.cs#L18)는 Android의 [`AssetManager`](https://developer.android.com/reference/android/content/res/AssetManager)를 JNI로 직접 불러 이 문제를 풉니다. `AssetManager`는 APK 안의 에셋을 열고(`open`), 디렉터리 항목을 나열하고(`list`), 압축되지 않은 에셋의 크기를 알려 줍니다(`openFd`).

- [8.1 `AssetManager` 얻기](#81-assetmanager-얻기)
- [8.2 JNI 호출 세 가지](#82-jni-호출-세-가지)
- [8.3 파일인가 디렉터리인가](#83-파일인가-디렉터리인가)
- [8.4 열거](#84-열거)
- [8.5 읽기 스트림](#85-읽기-스트림)
- [8.6 스레드와 비용](#86-스레드와-비용)

## 8.1 `AssetManager` 얻기

```csharp
// Runtime/IO/StreamingAssets/Android/AndroidStreamingIOProvider.cs
20  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
21  static void Init()
    {
23      _assetManager = null;                                             // ① 도메인 리로드 없이 재시작해도 낡은 객체를 버림
24      if (Application.platform != RuntimePlatform.Android)
25          return;
27      AndroidJavaObject? currentActivity = AndroidApplication.currentActivity;
28      if (currentActivity == null)
29          return;
31      _assetManager = currentActivity.Call<AndroidJavaObject>("getAssets");   // ② 메인 스레드에서 미리 얻어 둠
    }
34  static AndroidJavaObject? _assetManager;
35  static AndroidJavaObject assetManager => _assetManager ??= CreateAssetManager();   // ③ 못 얻었으면 처음 쓸 때 다시 시도
```

② `SubsystemRegistration`은 Unity가 가장 먼저 부르는 초기화 시점이고, 메인 스레드에서 불립니다. 그래서 이후 대부분의 호출이 스레드 풀에서 일어나도([8.6절](#86-스레드와-비용)) 액티비티는 메인 스레드에서 미리 얻어 둔 것을 씁니다. ③ 그때 액티비티가 없었으면 처음 쓰는 순간 [`CreateAssetManager`](../../Runtime/IO/StreamingAssets/Android/AndroidStreamingIOProvider.cs#L37)가 다시 얻고, Android가 아니면 `PlatformNotSupportedException`을 던집니다.

APK 에셋은 하나뿐이므로 공급자 인스턴스가 여러 개여도 같은 대상입니다. [`IsSameTarget`](../../Runtime/IO/StreamingAssets/Android/AndroidStreamingIOProvider.cs#L60)은 상대가 이 타입이기만 하면 `true`입니다.

## 8.2 JNI 호출 세 가지

모든 동작은 아래 세 정적 메서드의 조합입니다. 각각 호출 전에 현재 스레드를 JVM에 붙입니다(`AndroidJNI.AttachCurrentThread`). 스레드 풀 스레드는 JVM이 모르는 스레드라 붙이지 않으면 JNI 호출이 실패합니다.

```csharp
// Runtime/IO/StreamingAssets/Android/AndroidStreamingIOProvider.cs
228 static string[] ListAssets(string relativePath)
    {
230     EnsureJniThreadAttached();
231     return assetManager.Call<string[]>("list", relativePath) ?? [];     // ① 자식 이름 목록 (없거나 파일이면 빈 배열)
    }
234 static bool TryOpenAsset(string relativePath, out AndroidJavaObject? stream)
    {
236     EnsureJniThreadAttached();
237     try
        {
239         stream = assetManager.Call<AndroidJavaObject>("open", relativePath);   // ② Java InputStream
240         return stream != null;
        }
242     catch (AndroidJavaException exception) when (IsMissingAssetError(exception))
        {
244         stream = null;
245         return false;                                                   // ③ 없는 에셋만 false, 나머지 예외는 전달
        }
    }
260 static long? TryGetFileSize(string relativePath)
    {
262     EnsureJniThreadAttached();
263     try
        {
265         using AndroidJavaObject? fileDescriptor = assetManager.Call<AndroidJavaObject>("openFd", relativePath);
266         return fileDescriptor?.Call<long>("getLength");                  // ④ 압축 안 된 에셋만 크기를 앎
        }
268     catch (AndroidJavaException exception) when (IsAssetFdUnsupportedError(exception))
        {
270         return null;                                                    // ⑤ 압축된 에셋은 크기 모름
        }
    }
```

④⑤ `openFd`는 APK 안에 **압축하지 않고** 저장된 에셋만 열 수 있습니다. 압축된 에셋은 크기를 알 수 없으므로 `FileMetaData.size`가 `null`이 됩니다. [2.5절](02-providers-and-nodes.md#25-ioentry와-filemetadata)에서 메타데이터를 모두 nullable로 둔 이유 중 하나입니다.

> **관찰된 동작:** ③⑤ "없는 에셋"과 "fd로 열 수 없는 에셋"을 예외 **메시지 문자열**로 판별합니다([`IsMissingAssetError`](../../Runtime/IO/StreamingAssets/Android/AndroidStreamingIOProvider.cs#L274), [`IsAssetFdUnsupportedError`](../../Runtime/IO/StreamingAssets/Android/AndroidStreamingIOProvider.cs#L281)). Android 버전에 따라 메시지가 다르면 판별이 달라질 수 있습니다.

## 8.3 파일인가 디렉터리인가

`AssetManager`에는 "이 경로가 디렉터리인가"를 묻는 API가 없습니다. APK의 zip 목록에는 파일만 있고, 디렉터리는 파일 경로의 앞부분으로만 존재하기 때문입니다. 그래서 이 공급자는 **자식이 있으면 디렉터리**로 봅니다.

```csharp
// Runtime/IO/StreamingAssets/Android/AndroidStreamingIOProvider.cs
108 public UniTask<IOEntry?> GetEntry(RuniPath path, CancellationToken cancellationToken = default) => UniTask.RunOnThreadPool(() =>
    {
112     string relativePath = path.value;
113     string[] children = ListAssets(relativePath);
116     if (path.IsEmpty() || children.Length > 0)                    // ① 루트이거나 자식이 있으면 디렉터리
            return new IOEntry { …, attributes = FileAttributes.Directory | FileAttributes.ReadOnly, isDirectory = true };
130     if (!FileExists(relativePath))                                 // ② 열어 봐서 열리면 파일
131         return (IOEntry?)null;
133     return new IOEntry { …, size = TryGetFileSize(relativePath), attributes = FileAttributes.ReadOnly, isDirectory = false };
    }, cancellationToken: cancellationToken);
```

② [`FileExists`](../../Runtime/IO/StreamingAssets/Android/AndroidStreamingIOProvider.cs#L249)는 `open`으로 열어 본 뒤 바로 닫습니다. 모든 엔트리는 `ReadOnly` 특성을 가집니다. 수정 시간은 알 수 없으므로 비어 있고, 따라서 [`IsSameRevision`](02-providers-and-nodes.md#25-ioentry와-filemetadata)은 이 공급자의 파일을 항상 "바뀌었을 수 있음"으로 봅니다.

`DirectoryExists`([#L78](../../Runtime/IO/StreamingAssets/Android/AndroidStreamingIOProvider.cs#L78))도 같은 규칙(루트이거나 `list` 결과가 있음)입니다.

> **관찰된 동작:** 이 규칙에서 빈 디렉터리는 디렉터리로 판정되지 않습니다. 열거([8.4절](#84-열거))에서는 자식 없는 항목을 파일로 내보내지만, 그 경로의 `GetEntry`는 `open`이 실패하므로 `null`을 돌려줍니다. APK에 빈 디렉터리가 실제로 생기는지는 확인하지 않았습니다.

## 8.4 열거

```csharp
// Runtime/IO/StreamingAssets/Android/AndroidStreamingIOProvider.cs
147 public IUniTaskAsyncEnumerable<IOEntry> EnumerateEntries(RuniPath path, bool recursive, CancellationToken cancellationToken = default)
    {
149     return Enumerate(path, recursive, cancellationToken).EnumerateOnThreadPool(cancellationToken: cancellationToken);   // ① 3.3절의 스레드 풀 열거
151     static IEnumerable<IOEntry> Enumerate(RuniPath path, bool recursive, CancellationToken cancellationToken)
        {
153         string[] rootItems = ListAssets(path.value);
154         if (rootItems.Length <= 0)
155             yield break;                                                       // ② 없는 디렉터리 = 빈 결과
157         Queue<(RuniPath path, string[] items)> queue = new Queue<(RuniPath path, string[] items)>();
158         queue.Enqueue((path, rootItems));
160         while (queue.Count > 0)                                                // ③ 너비 우선
            {
163             (RuniPath currentPath, string[] items) = queue.Dequeue();
165             foreach (string item in items)
                {
169                 RuniPath entryPath = currentPath / item;
170                 string[] childItems = ListAssets(entryPath.value);             // ④ 항목마다 list 한 번
171                 bool isDirectory = childItems.Length > 0;
                    … IOEntry 생성 (파일이면 TryGetFileSize 한 번 더) …
202                 yield return entry;
204                 if (recursive && isDirectory)
205                     queue.Enqueue((entryPath, childItems));                    // ⑤ 이미 받은 자식 목록을 재사용
                }
            }
        }
    }
```

- ③ 재귀를 큐로 풉니다. 디렉터리 엔트리가 그 내용보다 먼저 나옵니다.
- ④ `list()`는 이름만 돌려주므로 파일인지 디렉터리인지 알려면 항목마다 `list()`를 한 번 더 불러야 합니다. ⑤ 그 결과를 큐에 넣어, 디렉터리를 내려갈 때 같은 `list()`를 다시 부르지 않습니다.
- 큐는 열거 하나에 하나 만드는 할당입니다. 재귀 호출 없이 큐로 내려가므로 디렉터리가 깊어도 호출 스택이 늘지 않습니다.

> **관찰된 동작 (기기에서 확인하지 않음):** 루트(`""`)를 열거하면 `AssetManager.list("")`가 `StreamingAssets` 파일 외에 Unity가 APK `assets/`에 넣는 다른 항목(예: `bin/`)도 돌려줄 수 있습니다.

## 8.5 읽기 스트림

`open()`이 돌려준 Java `InputStream`을 [`AndroidAssetStream`](../../Runtime/IO/StreamingAssets/Android/AndroidStreamingIOProvider.cs#L289)이 .NET `Stream`으로 감쌉니다.

```csharp
// Runtime/IO/StreamingAssets/Android/AndroidStreamingIOProvider.cs
289 sealed class AndroidAssetStream(AndroidJavaObject javaInputStream, long? length) : Stream
    {
295     public override bool CanSeek => false;                                 // ① 앞으로만 읽음
298     public override long Length => length ?? throw new NotSupportedException(…);   // ② 압축 에셋은 길이 모름
308     public override int Read(byte[] buffer, int offset, int count)
        {
            … 인수 검사 …
323         EnsureJniThreadAttached();                                         // ③ Read를 부른 스레드를 JVM에 붙임
324         int bytesRead = javaInputStream.Call<int>("read", buffer, offset, count);
325         if (bytesRead < 0)
326             return 0;                                                      // ④ Java의 -1(끝)을 .NET의 0으로
328         _position += bytesRead;
329         return bytesRead;
        }
336     protected override void Dispose(bool disposing)
        {
            … EnsureJniThreadAttached(); javaInputStream.Call("close"); javaInputStream.Dispose(); …
        }
    }
```

④ Java `InputStream.read`는 끝에서 `-1`, .NET `Stream.Read`는 `0`을 돌려주므로 변환합니다. 이 공급자는 `ReadAllBytes`/`ReadAllText`를 재정의하지 않아 [기본 구현](02-providers-and-nodes.md#22-iioprovider-읽기-계약)이 이 스트림을 끝까지 읽습니다.

> **관찰된 동작 (기기에서 확인하지 않음):** ③ C# `byte[]`를 Java `read(byte[], int, int)`에 넘깁니다. Unity의 JNI 변환이 배열을 복사해서 넘기고 되돌려 쓰지 않는다면, Java가 채운 데이터가 C# 버퍼에 오지 않습니다.

## 8.6 스레드와 비용

`DirectoryExists`, `FileExists`, `GetEntry`, `OpenRead`는 모두 `UniTask.RunOnThreadPool`로 실행되고, 열거는 [`EnumerateOnThreadPool`](03-enumeration-and-matchers.md#33-enumerateonthreadpool-스레드-풀에서-열거하기)을 씁니다. JNI 호출은 일반 메서드 호출보다 훨씬 비싸고, 팩 하나를 읽을 때 수백 번 불리므로 메인 스레드에서 돌리면 프레임이 멈춥니다.

열거 비용은 항목 수에 비례합니다. 항목 하나에 `list()` 한 번, 파일이면 `openFd()` 한 번이 더 듭니다.

> **관찰된 동작:** 모든 작업을 스레드 풀에서 실행하지만 `isThreadSafe`를 재정의하지 않아 기본값 `false`를 보고합니다([2.2절](02-providers-and-nodes.md#22-iioprovider-읽기-계약)).
