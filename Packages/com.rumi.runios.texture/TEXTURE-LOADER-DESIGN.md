# Texture Loader Design

## 1. 목표

`TextureLoader`는 인코딩된 이미지 데이터를 백그라운드에서 FreeImage로 디코드하고, Unity 메인 스레드에서 `Texture2D`를 생성한다.

출력 포맷을 호출자가 선택하는 변환 파이프라인은 사용하지 않는다. FreeImage 디코드 결과를 Unity가 직접 받을 수 있는 포맷으로 한 번 정규화하고, 그 결과를 기준으로 밉맵을 만든다.

핵심 순서:

```text
encoded ReadOnlyMemory<byte>
    -> FreeImage decode
    -> decoded NativeArray<byte>
    -> mipmap NativeArray<byte>[]
    -> Texture2D.SetPixelData
    -> Texture2D.Apply
```

## 2. 공개 API

```csharp
public static UniTask<Texture2D> LoadAsync(
    ReadOnlyMemory<byte> data,
    TextureLoadSettings settings = default,
    CancellationToken cancellationToken = default);

public static UniTask<Texture2D> LoadAsync(
    Stream stream,
    TextureLoadSettings settings = default,
    CancellationToken cancellationToken = default);

public static UniTask<Texture2D> LoadAsync(
    IONode file,
    TextureLoadSettings settings = default,
    CancellationToken cancellationToken = default);
```

`TextureLoadSettings`는 밉맵, 선형 색 공간, CPU 읽기 가능 여부만 설정한다.
`outputFormat`은 존재하지 않는다.

```csharp
public readonly struct TextureLoadSettings
{
    public TextureMipmapSettings mipmaps { get; }
    public bool linear { get; }
    public bool makeNoLongerReadable { get; }
}
```

## 3. 단계별 동작

### 3.1 입력

- `ReadOnlyMemory<byte>`는 복사하지 않고 그대로 FreeImage에 pin한다.
- `Stream`은 `CopyToAsync`로 `MemoryStream`에 읽고 `ToArray()` 결과를 사용한다.
- `IONode`는 자체 스트림을 열고 읽은 뒤 즉시 닫는다.
- 호출자가 준 `Stream`은 닫지 않는다.

### 3.2 FreeImage 디코드

`FreeImageDecoder`가 encoded memory를 pin한 상태로 `FreeImage_OpenMemory`, 포맷 판별, 이미지 로드를 끝낸다.
FreeImage native bitmap은 `DecodedImage`가 소유하는 `NativeArray<byte>` 하나로 복사한다.

이 복사는 FreeImage native 메모리의 수명과 Unity/Job 수명을 분리하는 유일한 디코드 버퍼 복사다.
RGB/BGR, RGB555/RGB565, RGB float의 레이아웃 정규화도 이 복사 단계에서 수행한다. 별도 변환 destination은 만들지 않는다.

`DecodedImage`는 다음 정보를 가진다.

- width / height
- Unity `TextureFormat`
- mipmap kernel 종류
- bytes per pixel
- base level `NativeArray<byte>`

### 3.3 밉맵

`TextureMipmapScheduler`는 `DecodedImage.pixels`를 첫 입력으로 사용한다.
각 밉맵 레벨마다 `NativeArray<byte>` 하나만 새로 만들고, 직전 레벨에서 다음 레벨로 Job을 예약한다.

```text
level 0: decodedImage.pixels       (decoder 소유)
level 1: mipmapLevels[0]            (scheduler 소유)
level 2: mipmapLevels[1]
level 3: mipmapLevels[2]
...
```

밉맵이 없으면 level 0만 존재한다. `Texture2D.GetPixelData`는 사용하지 않는다.

### 3.4 Unity 업로드

Job이 완료된 뒤 메인 스레드에서:

1. 디코드 결과의 `TextureFormat`과 밉맵 개수로 `Texture2D`를 생성한다.
2. `SetPixelData(decodedImage.pixels, 0)`으로 base level을 넣는다.
3. 각 `mipmapLevels`를 해당 mip level에 `SetPixelData`한다.
4. `Apply(false, makeNoLongerReadable)`를 호출한다.

`SetPixelData`에서 Unity가 소유한 텍스처 CPU 저장소로 들어가는 복사는 엔진 경계상 불가피하다. 그 이전에는 출력 변환용 배열을 만들지 않는다.

## 4. FreeImage 결과와 Unity 포맷

| FreeImage 결과 | Unity 포맷 | 비고 |
|---|---|---|
| 1/2/4/8-bit grayscale | `R8` | 값 확장은 디코드 중 수행 |
| 16-bit grayscale | `R16` | white-is-min은 디코드 중 반전 |
| signed 16-bit | `R16_SIGNED` | |
| RGB555 / RGB565 | `RGB565` | RGB555는 디코드 중 RGB565로 정규화 |
| 24-bit bitmap | `RGB24` | FreeImage BGR을 RGB로 정규화 |
| 32-bit bitmap | `RGBA32` | FreeImage BGRA를 RGBA로 정규화 |
| unsigned 32-bit | `RG32` | 원시 4-byte payload 유지 |
| signed 32-bit | `RG32_SIGNED` | 원시 4-byte payload 유지 |
| float32 | `RFloat` | |
| float64 | `RGFloat` | |
| complex | `RGBAFloat` | double payload 밉맵 |
| RGB16 / RGBA16 | `RGB48` / `RGBA64` | |
| RGB float | `RGBAFloat` | alpha 1.0을 디코드 중 기록 |
| RGBA float | `RGBAFloat` | |

플랫폼이 디코드 결과 포맷을 지원하지 않으면 변환 fallback 없이 실패한다.

## 5. 메모리와 소유권

### 밉맵 없음

```text
caller encoded memory
decodedImage.pixels
Texture2D internal storage
```

### 밉맵 있음

```text
caller encoded memory
decodedImage.pixels
mipmapLevels[0..N]
Texture2D internal storage
```

- `ReadOnlyMemory<byte>`의 소유권은 호출자에게 있다.
- `DecodedImage`가 base `NativeArray`를 dispose한다.
- `TextureMipmapData`가 생성한 mip `NativeArray`를 dispose한다.
- Job이 완료되기 전에는 어떤 NativeArray도 dispose하지 않는다.
- `Texture2D`가 데이터를 받은 뒤 임시 NativeArray를 dispose한다.
- `makeNoLongerReadable`가 `true`이면 Unity CPU 저장소는 `Apply` 후 제거된다.

## 6. 스레드 경계

- 입력 읽기: thread pool
- FreeImage native decode: thread pool
- mipmap Job 실행: Burst/Job worker
- `Texture2D` 생성, `SetPixelData`, `Apply`: main thread

`FreeImageDecoder`는 Unity 객체를 만지지 않는다. `TextureMipmapScheduler`도 `Texture2D`를 받지 않는다.

## 7. 제거한 복잡성

- `TextureLoadSettings.outputFormat`
- `ITextureInput` 및 입력 adapter 클래스
- `EncodedImageBuffer`
- `ArrayPool<byte>` 반환 계약
- `TextureOutputPlan`
- `TextureOutputPlanner`
- `TextureTransferScheduler`
- 출력 포맷 변환 Job들
- `Texture2D.GetPixelData` 기반 mipmap 작성

남은 Job은 포맷별 밉맵 생성 Job뿐이다. 디코드 결과가 곧 base level이므로 transfer 단계가 없다.
