# Runiverse OS Texture

Language available: \[[**한국어 (대한민국)**](README.md) | [English (US)](README-EN.md)]

## 개요

`com.rumi.runios.texture`는 인코딩된 이미지 데이터를 런타임 `UnityEngine.Texture2D`로 로드하는 패키지입니다.

- FreeImage 기반 이미지 디코드
- `ReadOnlyMemory<byte>`, `Stream`, `IONode` 입력
- 백그라운드 디코드와 Burst 밉맵 생성
- Unity 메인 스레드에서 `Texture2D` 생성·raw pixel 업로드
- PNG/JPEG뿐 아니라 FreeImage가 제공하는 다양한 이미지 입력 처리

이 패키지는 Unity 에셋 임포터가 아니라 런타임 로더입니다. 입력 데이터는 호출자가 제공해야 합니다.

## 요구 사항

- Unity `6000.3` 이상
- `com.unity.burst` `2.0.0`
- `com.unity.mathematics` `1.4.0`
- 프로젝트의 `RuniOS.IO` 및 `UniTask` 참조
- 현재 포함된 FreeImage 네이티브 바이너리: Windows x86/x64, Linux

## 기본 사용

```csharp
using Cysharp.Threading.Tasks;
using RuniOS.Textures;
using System.IO;
using System.Threading;
using UnityEngine;

static UniTask<Texture2D> LoadTextureAsync(Stream stream, CancellationToken cancellationToken)
{
    TextureLoadSettings settings = new(
        mipmaps: TextureMipmapSettings.full,
        linear: false,
        makeNoLongerReadable: true);

    return TextureLoader.LoadAsync(stream, settings, cancellationToken);
}
```

메모리와 `IONode`에서도 같은 로더를 사용할 수 있습니다.

```csharp
Texture2D fromMemory = await TextureLoader.LoadAsync(
    encodedBytes,
    cancellationToken: cancellationToken);

Texture2D fromFile = await TextureLoader.LoadAsync(
    imageNode,
    cancellationToken: cancellationToken);
```

## 공개 API

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
    IONode node,
    TextureLoadSettings settings = default,
    CancellationToken cancellationToken = default);
```

`TextureLoadSettings`는 다음 동작을 설정합니다.

| 설정 | 설명 |
| --- | --- |
| `mipmaps` | 밉맵 생성 방식. 기본값은 `full`입니다. |
| `linear` | 결과 텍스처를 선형 데이터로 취급할지 여부입니다. |
| `makeNoLongerReadable` | `Apply` 뒤 CPU 측 텍스처 데이터를 제거할지 여부입니다. |

현재 public API는 출력 `TextureFormat`을 직접 선택하지 않습니다. 디코더가 입력의 채널·비트 깊이에 맞는 Unity 포맷을 자동으로 선택합니다.

## 밉맵 설정

```csharp
TextureMipmapSettings.full;          // 1x1까지 모든 레벨
TextureMipmapSettings.none;          // 기본 레벨만
TextureMipmapSettings.Explicit(4);   // 기본 레벨 포함 총 4개 레벨
```

`Explicit(count)`의 `count`는 기본 레벨을 포함합니다. 이미지 크기가 허용하는 최대 레벨보다 큰 값을 지정하면 `ArgumentOutOfRangeException`이 발생합니다.

## 처리 흐름

```text
encoded image data
    -> ThreadPool: FreeImage 포맷 판별·디코드·픽셀 정규화
    -> Burst Job: 포맷별 밉맵 생성
    -> Unity main thread: Texture2D 생성, SetPixelData, Apply
```

`Texture2D` 생성과 `SetPixelData`/`Apply`는 Unity 메인 스레드 단계입니다. 현재 구현은 Unity 메인 스레드에서 `LoadAsync`를 시작하는 사용을 전제로 합니다.

디코더는 FreeImage 네이티브 버퍼를 `NativeArray<byte>`로 한 번 복사해 native 메모리 수명과 Job 수명을 분리합니다. 밉맵이 활성화되면 각 레벨마다 별도 `NativeArray<byte>`를 만들고 `TextureMipmapData`가 소유합니다.

## 출력 포맷

디코드 결과는 가능한 경우 원래 데이터 타입을 유지하고, Unity raw texture 업로드에 맞게 필요한 정규화만 수행합니다.

| FreeImage 결과 | Unity 포맷 | 처리 |
| --- | --- | --- |
| 1/2/4/8-bit grayscale, grayscale palette | `R8` | 8-bit 값으로 확장 |
| 16-bit grayscale | `R16` | `min-is-white`는 반전 |
| RGB555/RGB565 | `RGB565` | RGB555를 RGB565로 변환 |
| 24-bit bitmap | `RGB24` | FreeImage BGR을 RGB로 변환 |
| 32-bit bitmap | `RGBA32` | FreeImage BGRA를 RGBA로 변환 |
| `uint16` / `int16` | `R16` / `R16_SIGNED` | 원시 채널 유지 |
| `uint32` / `int32` | `RG32` / `RG32_SIGNED` | 원시 payload 유지 |
| `float32` / `float64` | `RFloat` / `RGFloat` | 원시 부동소수점 데이터 유지 |
| `complex` | `RGBAFloat` | double payload 밉맵 사용 |
| `rgb16` / `rgba16` | `RGB48` / `RGBA64` | 16-bit 채널 유지 |
| `rgbFloat` / `rgbaFloat` | `RGBAFloat` | RGB 입력은 alpha `1.0` 추가 |

팔레트·CMYK·비표준 bitmap 채널 레이아웃은 필요하면 FreeImage의 24-bit 또는 32-bit 변환을 거칩니다. 플랫폼이 선택된 Unity 포맷을 지원하지 않으면 변환 fallback 없이 `NotSupportedException`이 발생합니다.

## 입력 및 소유권

- `ReadOnlyMemory<byte>`: 인코딩 버퍼는 호출자 소유입니다. 로드가 끝날 때까지 underlying memory를 수정하거나 재사용하지 않아야 합니다.
- `Stream`: 전체 데이터를 비동기로 읽습니다. 호출자가 전달한 스트림은 닫지 않습니다.
- `IONode`: `node.file.ReadAllBytes()`로 파일을 읽은 뒤 메모리 입력 경로를 사용합니다.
- `CancellationToken`: 입력 읽기, 디코드 전후, Job 대기 단계에서 취소를 관찰합니다. 진행 중인 단일 native decode 호출을 중간에 강제 종료하지는 않습니다.

## 예외

- `ArgumentNullException`: null `Stream`
- `ArgumentException`: 읽을 수 없는 Stream, 빈 입력, 유효하지 않은 `IONode`
- `InvalidDataException`: 이미지 포맷 판별·디코드·픽셀 버퍼 검증 실패
- `NotSupportedException`: 플랫폼 텍스처 포맷 또는 최대 텍스처 크기 미지원
- `ArgumentOutOfRangeException`: 이미지 크기에 맞지 않는 밉맵 레벨 수
- `OperationCanceledException`: 취소 토큰에 의한 취소

## 관련 문서

- [Third-party notices](THIRD-PARTY-NOTICES.md)
- [FreeImage license](LICENSE.FreeImage.txt)
- [UnityAsyncImageLoader license](LICENSE.AsyncImageLoader.md)
