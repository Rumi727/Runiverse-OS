# 5. 쓰기·복사·이동

> 기준 커밋: `b3e1a343`. [← 목차](README.md) · 이전: [4. Physical과 샌드박스](04-physical-and-sandbox.md) · 다음: [6. `GroupIOProvider`](06-group-provider.md)

[`IWritableIOProvider`](../../Runtime/IO/IWritableIOProvider.cs#L13)는 구현 필수 멤버 다섯 개(`OpenWrite`, `CreateFile`, `CreateDirectory`, `DeleteFile`, `DeleteDirectory`) 위에 복사·이동·`WriteAll*`을 기본 구현으로 제공합니다. 새 공급자는 다섯 개만 구현하면 나머지가 동작합니다. 운영체제에 더 나은 방법이 있는 공급자는 덮어씁니다.

이 장은 기본 구현의 알고리즘, 실패하거나 취소됐을 때 남는 상태, 공급자별로 덮어쓴 부분을 다룹니다.

- [5.1 쓰기 노드 API](#51-쓰기-노드-api)
- [5.2 `OpenWrite`와 `CreateFile`](#52-openwrite와-createfile)
- [5.3 `CopyFile`과 `MoveFile`](#53-copyfile과-movefile)
- [5.4 디렉터리 복사·이동](#54-디렉터리-복사이동)
- [5.5 공급자별 재정의](#55-공급자별-재정의)

## 5.1 쓰기 노드 API

사용하는 쪽은 [`IOWriteNode`](../../Runtime/IO/IOWriteNode.cs#L11)의 `file`/`dir`을 씁니다. 읽기 멤버는 `IONode`와 같고, 쓰기 멤버가 더해집니다.

| `node.file.` | 공급자 멤버 | `node.dir.` | 공급자 멤버 |
| --- | --- | --- | --- |
| `WriteAllBytes/Text`, `WriteLines` | 같은 이름 | `Create()` | `CreateDirectory` |
| `OpenWrite()` | `OpenWrite` | `Delete()` | `DeleteDirectory` |
| `Delete()` | `DeleteFile` | `Move(dest)` | `MoveDirectory` |
| `Move(dest, overwrite)` | `MoveFile` | `Copy(dest)` | `CopyDirectory` |
| `Copy(dest, overwrite)` | `CopyFile` | | |

`Move`/`Copy`의 `dest`는 노드 기준이 아니라 **공급자 루트 기준** 경로입니다([`IOWriteNode.File.cs#L131`](../../Runtime/IO/IOWriteNode.File.cs#L131)). 복사·이동은 같은 공급자 안에서만 일어납니다. 서로 다른 공급자 사이의 복사는 이 API에 없고, 호출자가 한쪽에서 읽어 다른 쪽에 씁니다.

## 5.2 `OpenWrite`와 `CreateFile`

| 멤버 | 기존 내용 | 쓰는 곳 |
| --- | --- | --- |
| [`OpenWrite`](../../Runtime/IO/IWritableIOProvider.cs#L61) | **지우지 않음.** 처음부터 덮어쓰고, 새 내용이 짧으면 뒤가 남음 | 일부만 고칠 때 |
| [`CreateFile`](../../Runtime/IO/IWritableIOProvider.cs#L93) | 비움 | 전체를 새로 쓸 때 |

`WriteAll*` 기본 구현은 모두 `CreateFile`을 씁니다.

```csharp
// Runtime/IO/IWritableIOProvider.cs
404 async UniTask WriteAllText(RuniPath path, string text, CancellationToken cancellationToken = default)
    {
406     await using Stream stream = await CreateFile(path, cancellationToken);
407     await using StreamWriter writer = new StreamWriter(stream);      // UTF-8, BOM 없음
408     await writer.WriteAsync(text);
    }
```

## 5.3 `CopyFile`과 `MoveFile`

기본 `CopyFile`은 스트림 복사입니다.

```csharp
// Runtime/IO/IWritableIOProvider.cs
154 async UniTask CopyFile(RuniPath sourcePath, RuniPath destinationPath, bool overwrite = false, CancellationToken cancellationToken = default)
    {
157     if (sourcePath == destinationPath)                                  // ① 자기 자신에게 복사 거부
158         throw new IOException($"The file '{sourcePath}' cannot be copied to itself.");
160     if (!overwrite && await GetEntry(destinationPath, cancellationToken) != null)
161         throw new IOException($"The destination '{destinationPath}' already exists.");
163     try
        {
165         await using Stream source = await OpenRead(sourcePath, cancellationToken);
166         await using Stream destination = await CreateFile(destinationPath, cancellationToken);   // ② 대상 비움
167         await source.CopyToAsync(destination, 81920, cancellationToken);
        }
169     catch
        {
172         if (!overwrite && await FileExists(destinationPath, cancellationToken))
173             await DeleteFile(destinationPath, cancellationToken);       // ③ 새로 만든 반쪽 파일은 지움
175         throw;
        }
    }
```

① 같은 경로면 ②가 원본을 비운 뒤 읽게 되므로 미리 거부합니다. ③ 없던 대상에 쓰다 실패하거나 취소되면 반쯤 쓴 파일을 남기지 않습니다. 덮어쓰던 경우(`overwrite`)는 ②에서 이미 잘렸으므로 되돌릴 수 없고(171행 주석), 대상을 지우지도 않습니다.

기본 [`MoveFile`](../../Runtime/IO/IWritableIOProvider.cs#L288)은 같은 복사 뒤에 원본을 지웁니다(312행). 원본 삭제는 복사가 끝난 다음이므로, 도중에 실패해도 원본은 남습니다.

> **관찰된 동작:** 같은 경로를 넘기면 `CopyFile`은 예외(157행), `MoveFile`은 아무것도 하지 않고 반환합니다(291행). `MoveFile`의 주석(290행)은 "복사"라고 적혀 있습니다.

> **관찰된 동작:** ③의 정리는 실패 원인이 된 그 취소 토큰으로 `FileExists`/`DeleteFile`을 부릅니다. 취소 때문에 실패했다면 정리 호출도 취소될 수 있습니다(공급자가 토큰을 보는 경우).

`MoveFile`의 XML 주석(257~266행)은 대소문자를 구분하지 않는 파일 시스템에서 **대소문자만 다른 이름 변경**은 `overwrite`와 관계없이 이름을 바꾼다고 약속하고, 다른 경로가 같은 파일을 가리킬 수 있는 공급자는 이 메서드를 재정의해야 한다고 적습니다. 기본 구현은 서로 다른 경로를 서로 다른 파일로 다루므로, 이 약속은 재정의한 구현(Physical)이 지킵니다.

> **관찰된 동작 (실행으로 확인하지 않음):** Physical의 `MoveFile`은 두 경로를 [`ResolveFullPath`](04-physical-and-sandbox.md#42-모든-경로가-지나는-resolvefullpath)로 바꾼 뒤 `File.Move`를 부릅니다. Windows에서는 [1.5절](01-paths.md#15-physicalpath-디스크에-닿을-때)의 소문자화로 대소문자만 다른 두 경로가 같은 문자열이 되므로, 대소문자만 바꾸는 이동이 이름을 바꾸지 못할 수 있습니다.

## 5.4 디렉터리 복사·이동

```csharp
// Runtime/IO/IWritableIOProvider.cs
204 async UniTask CopyDirectory(RuniPath sourcePath, RuniPath destinationPath, CancellationToken cancellationToken = default)
    {
207     if (destinationPath.StartsWith(sourcePath))                    // ① 자기 자신·하위로 복사 거부
208         throw new IOException(…);
211     if (await GetEntry(destinationPath, cancellationToken) != null)  // ② 대상이 있으면 거부 (합치지 않음)
212         throw new IOException(…);
214     await CreateDirectory(destinationPath, cancellationToken);
216     await foreach (IOEntry entry in EnumerateEntries(sourcePath, false, cancellationToken))
        {
218         RuniPath childDestinationPath = destinationPath / entry.path.GetFileName();
219         if (entry.isDirectory)
220             await CopyDirectory(entry.path, childDestinationPath, cancellationToken);   // ③ 재귀
221         else
222             await CopyFile(entry.path, childDestinationPath, cancellationToken: cancellationToken);
        }
    }
```

① 자기 하위로 복사하면 복사한 자식을 다시 복사하며 끝나지 않습니다. [`StartsWith`](01-paths.md#13-상위상대-경로-판정)는 조각 경계로 판정하고, 빈 경로(루트)는 모든 경로의 상위입니다. ② 이미 있는 디렉터리와 합칠지는 사용하는 쪽이 정할 일이라 거부합니다.

[`MoveDirectory`](../../Runtime/IO/IWritableIOProvider.cs#L340)도 같은 구조에 두 가지가 다릅니다.

```csharp
// Runtime/IO/IWritableIOProvider.cs
343 if (sourcePath.IsEmpty() || destinationPath.StartsWith(sourcePath))     // ① 루트 자체는 옮길 수 없음
…
353 List<IOEntry> entries = await EnumerateEntries(sourcePath, false, cancellationToken).ToListAsync(cancellationToken);   // ② 먼저 목록을 다 받음
354 foreach (IOEntry entry in entries)
    { … MoveDirectory / MoveFile … }
363 await DeleteDirectory(sourcePath, cancellationToken);
```

② 옮기는 동안 원본 디렉터리가 바뀌므로, 열거하면서 옮기지 않고 목록을 먼저 리스트로 받습니다. 여기서 리스트를 만드는 것은 원본이 바뀌는 중에 열거하지 않기 위한 의도된 할당입니다.

> **관찰된 동작:** 디렉터리 복사·이동은 대상을 먼저 만들고 하나씩 처리하므로, 중간에 실패하면 대상에는 일부만 있는 트리가, 이동의 경우 원본에는 아직 옮기지 못한 나머지가 남습니다. 되돌리기는 하지 않습니다.

## 5.5 공급자별 재정의

| 멤버 | 기본 구현 | Physical | Virtual |
| --- | --- | --- | --- |
| `WriteAll*` | `CreateFile` + 스트림 | `File.WriteAll*` (동기) | `GetOrCreateFile` 뒤 파일에 씀 ([9장](09-virtual-fs.md)) |
| `CopyFile` | 스트림 복사 | `File.Copy` | 기본 구현 |
| `MoveFile` | 복사 + 삭제 | `File.Move` | 노드를 떼어 다른 부모에 붙임 |
| `CopyDirectory` | 재귀 복사 | 기본 구현 | 기본 구현 |
| `MoveDirectory` | 재귀 이동 | `Directory.Move` | 노드를 떼어 붙임 |
| `CreateSymbolicLink` | `NotSupportedException` | 루트 안 상대 링크 생성 | 기본 구현 |
| 없는 파일 `DeleteFile` | — | 조용히 넘어감 (`File.Delete`) | 예외 |

Physical의 [`CreateSymbolicLink`](../../Runtime/IO/PhysicalIOProvider.cs#L282)는 링크 위치 기준 **상대 경로**로 링크를 만듭니다. 그래서 루트 폴더를 통째로 옮겨도 링크가 가리키는 위치가 루트 안에서 유지됩니다.

Virtual의 이동은 데이터를 복사하지 않고 트리에서 노드를 떼어 다시 붙입니다([`MoveFile`](../../Runtime/IO/Virtual/VirtualIOProvider.cs#L282)~345행). 떼어 낸 뒤 붙이기에 실패하면 노드가 트리에서 사라지므로, 떼어 내기 **전에** 대상 부모가 있는지, 이름이 올바른지, 대상이 비어 있는지를 모두 확인합니다(279행 주석). `overwrite`일 때 기존 대상은 떼어 내기 전에 지웁니다(309행).
