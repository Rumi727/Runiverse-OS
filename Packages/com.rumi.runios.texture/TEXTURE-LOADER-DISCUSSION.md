# 런타임 텍스처 로더 논의 요약

> 작성일: 2026-08-21
>
> 범위: `Stream`/`byte[]`에서 시작하는 비동기 Unity `Texture2D` 로더, FreeImage 및 `UnityAsyncImageLoader` 검토 내용.

## 목표

- 리소스 시스템과 독립된 텍스처 패키지로 제공한다.
- 최소 입력 단위는 `Stream`이다. 필요하면 `byte[]`로 변환해도 된다.
- 파일 경로에 종속되지 않는다.
- 파일 읽기, 디코드, 픽셀 변환, 밉맵 생성을 백그라운드에서 수행한다.
- 메인 스레드에서는 Unity 객체 생성과 최종 GPU 업로드만 수행한다.
- PNG뿐 아니라 FreeImage가 읽을 수 있는 입력을 최대한 지원한다.
- Unity가 표현할 수 있는 범위에서는 채널·비트 깊이·색상·밉맵·읽기/쓰기 상태를 최대한 보존한다.
- 완전히 원본 포맷을 유지할 수 없는 경우에도 조용히 실패하지 않고, 적절한 Unity 포맷으로 승격·변환한다.

## Unity 임포터와 런타임 로더의 차이

Unity 임포터의 `TextureFormat` 선택은 대체로 프로젝트에 저장할 임포트 결과와 메모리/GPU 저장 형식을 정하는 옵션이다. 런타임 로더는 이미 원본 파일을 직접 읽으므로, 임포터의 모든 압축·다운그레이드 선택지를 그대로 노출할 필요가 없다.

런타임에서 의미가 큰 설정은 다음이다.

- 읽기 가능 여부: `Texture2D.Apply(..., makeNoLongerReadable)`
- 밉맵 생성 여부 및 밉맵 수
- 선형/감마 데이터 여부
- 입력의 채널 의미를 유지할 출력 포맷
- Unity 메모리와 GPU에 올릴 시점

메모리 압축은 원본을 읽은 뒤 결국 런타임용 텍스처로 변환해야 하므로 기본 기능으로 넣을 우선순위가 낮다. 필요하면 별도 출력 단계로 추가한다.

## 포맷·비트 깊이 결론

PNG에는 1/2/4/8/16-bit 채널 이미지가 실제로 존재한다. 여기서 16-bit는 보통 이미지 전체가 아니라 채널 하나당 16-bit라는 뜻이다. 팔레트 PNG와 저비트 grayscale/alpha PNG도 가능하다.

Unity `TextureFormat`에는 PNG의 1/2/4-bit 저장을 그대로 표현하는 일반적인 `Texture2D` 포맷이 없다. 따라서 다음과 같은 변환은 불가피하다.

| 입력 의미 | 권장 Unity 출력 |
|---|---|
| 1/2/4/8-bit grayscale | `R8` 또는 의미에 따라 `Alpha8` |
| alpha mask | `Alpha8` |
| 팔레트 색상 | 팔레트를 펼쳐 `RGB24`/`RGBA32` |
| 일반 RGB/RGBA 8-bit | `RGB24`/`RGBA32` |
| 16-bit unsigned 채널 | 대응되는 `R16`/`RG16`/`RGBA64` 계열이 있을 때 유지 |
| float/특수 signed 타입 | Unity에 대응 포맷이 있을 때 유지, 없으면 명시적으로 변환 |

즉 저비트 입력을 RGB/RGBA로 무조건 펼치면 메모리상 비효율적이다. 다만 색상 팔레트 이미지는 인덱스만으로 일반 `Texture2D`에 넣을 수 없으므로, 팔레트 셰이더를 별도로 설계하지 않는 한 RGB/RGBA로 펼쳐야 한다.

FreeImage는 `FIT_BITMAP`, `FIT_UINT16`, `FIT_INT16`, float/double 및 RGB(A) 계열 등 다양한 내부 타입을 다룬다. 하지만 FreeImage 입력 타입 전체를 Unity `Texture2D` 포맷 전체로 1:1 대응시킬 수 있는 것은 아니다. 보존 가능한 타입은 보존하고, 나머지는 손실·승격 여부를 분명히 한 뒤 변환해야 한다.

## `UnityAsyncImageLoader`에서 확인한 핵심

참고: [Looooong/UnityAsyncImageLoader](https://github.com/Looooong/UnityAsyncImageLoader)

- FreeImage 디코드와 일부 픽셀 처리·밉맵 생성을 다른 스레드로 옮긴다.
- Burst는 FreeImage 자체 디코더가 아니라, Burst로 컴파일 가능한 픽셀 전송/밉맵 작업에 적용되는 구조다.
- `Texture2D` 생성, `LoadRawTextureData`, `Apply` 등 Unity 객체 작업은 메인 스레드 단계가 필요하다.
- 로더가 반환된 뒤에도 GPU 전송이 끝나지 않았을 수 있어, 즉시 사용하면 해당 시점에 메인 스레드가 대기할 수 있다.
- 업스트림의 기본 출력은 alpha가 있으면 `RGBA32`, 없으면 `RGB24` 중심이다.
- 입력은 주로 `byte[]` API이고, 원본 FreeImage 타입·채널을 그대로 Unity 출력으로 보존하는 로더는 아니다.

### 과거 저비트/비트맵 문제의 해석

`Support non-bitmap image type` 계열 변경에서 문제가 생긴 핵심 원인은 디코더가 못 읽어서라기보다, 로더의 타입·비트 깊이 검사와 변환 경로가 제한적이었기 때문으로 보는 것이 타당하다.

당시 8/4/2/1 조건만 통과시키자 이미지가 정상적으로 보였던 현상은, 그 조건을 통과한 뒤 FreeImage가 팔레트·저비트 데이터를 일반적인 raw 픽셀로 변환해 주는 경로에 들어갔기 때문일 가능성이 높다. 조건만 추가한 것은 검증을 우회한 것이고, 모든 입력에 대한 올바른 포맷 매핑을 구현한 것은 아니다.

`ConvertToRawBit`/`ConvertToRawBits` 계열 처리는 팔레트, sub-byte 픽셀, 비트맵이 아닌 FreeImage 타입의 원시 데이터를 Unity가 이해할 수 있는 픽셀 배열로 펼치는 역할이므로, 해당 경로를 제거하면 저비트·팔레트·특수 타입이 다시 깨질 수 있다.

또한 `SystemInfo`의 일부 프로퍼티를 worker thread에서 호출하면 Unity 버전에 따라 문제가 생길 수 있다. Unity API 및 `SystemInfo` 값은 메인 스레드에서 미리 캡처하거나, worker 경로에서 제거해야 한다.

`Pixels`의 static interface member 사용은 Unity가 실제로 사용하는 C# 컴파일러/프로파일에 의존한다. 최신 C#에서는 가능해도 구버전 Unity에서는 컴파일 자체가 깨질 수 있으므로, 패키지 호환성을 우선하면 구체 타입 또는 일반 static helper가 안전하다.

## FIT 타입과 밉맵

`FIT_UINT16`에 `ushort` 기반 계산을 쓰는 것은 unsigned 데이터에는 맞을 수 있다. 그러나 `FIT_INT16`은 음수 값을 갖기 때문에 같은 계산을 재사용하면 값의 의미가 달라진다.

필요한 처리 방향:

- `FIT_UINT16`: unsigned 정수 보간 및 범위 처리
- `FIT_INT16`: signed 정수 보간 및 음수 보존
- 오버플로를 피하기 위해 중간 계산은 `int` 또는 더 넓은 타입 사용
- 최종 저장 시 해당 타입의 범위로 명시적 clamp/rounding
- 밉맵 생성도 타입별 의미를 유지

밉맵은 런타임 raw 데이터 로더가 자동으로 파일에서 따라오는 기능이 아니다. 로더가 각 레벨의 데이터를 만들거나, Unity에 메인 스레드 재계산을 맡겨야 한다. 비동기 목표라면 FreeImage/Burst/별도 CPU 루틴으로 worker 단계에서 생성하고, 최종 `Texture2D`에 모든 레벨을 업로드하는 방식이 적절하다.

## FreeImage 메모리 로드 흐름

일반적인 파이프라인은 다음과 같다.

```text
Stream
  -> byte[] 또는 unmanaged buffer
  -> FreeImage_OpenMemory
  -> FreeImage_LoadFromMemory
  -> FIBITMAP 타입/비트 깊이/채널 확인
  -> 필요한 경우 palette·sub-byte·특수 타입 변환
  -> raw pixels 및 밉맵 생성
  -> 메인 스레드에서 Texture2D 생성
  -> LoadRawTextureData / Apply
```

- `FreeImage_OpenMemory`는 메모리 블록을 FreeImage의 `FIMEMORY`로 감싼다.
- `FreeImage_LoadFromMemory`는 그 메모리에서 이미지 포맷을 판별하고 `FIBITMAP`을 만든다.
- `FIMEMORY`와 `FIBITMAP`은 모든 FreeImage 작업이 끝난 뒤 닫아야 한다.
- `FIMEMORY`를 닫기 전에 원본 `byte[]`/unmanaged buffer를 해제하면 안 된다.
- FreeImage.NET의 `LoadFromStream`은 네이티브 함수가 .NET `Stream`을 직접 이해해서가 아니라, managed stream을 FreeImage I/O callback 형태로 감싸는 래퍼로 보는 것이 맞다.
- 최종 설계에서는 `SafeHandle`을 사용하지 않는다. 대부분의 FreeImage 핸들은 수명이 짧은 포인터이고, `SafeHandle`의 GC·finalizer 비용을 감수할 이유가 없다. 현재 `MemoryHandle`처럼 `IntPtr`와 동일한 레이아웃의 명시적 구조체 wrapper를 사용하고, 단일 owner가 `using`/`finally`에서 정확히 한 번 해제한다. 구조체 wrapper는 ABI와 수명 표현을 위한 타입이지, 복사까지 추적하는 범용 managed resource 보호 계층으로 만들지 않는다.

완전한 non-allocation은 현실적으로 어렵다. `Texture2D`와 네이티브 텍스처 메모리는 필수이고, 일반 `Stream`은 byte buffer 복사가 필요할 수 있다. 목표는 buffer 재사용, 변환 중간 배열 최소화, worker별 scratch buffer 재사용이다.

## 패키지·포크 방향

- 리소스 시스템에 즉시 통합하지 않고 `com.rumi.runios.texture`를 독립 패키지로 둔다.
- FMOD, Sound, UI, NBS, Effect처럼 모듈식 패키지로 취급한다.
- Git submodule/추가 Git dependency를 피하기 위해 포크 코드를 패키지 안에 포함하는 방향을 검토했다.
- Git 이력 전체를 보존하기보다 원 저작권 고지와 라이선스를 유지하는 것을 차선책으로 선택했다.
- 패키지에는 assembly definition, `csc.rsp`, `AssemblyInfo`, FreeImage native binary, 라이선스 고지를 포함한다.
- 동시 설치 충돌은 현재 고려 대상에서 제외한다.

## 대안 로더 조사 결과

| 후보 | 장점 | 현재 목표와의 차이 |
|---|---|---|
| [StbImageSharpForUnity](https://github.com/mochi-neko/StbImageSharpForUnity) | 순수 C#, `Stream`/`byte[]`, worker decode 예제가 깔끔함 | Unity 변환부가 RGB/RGBA/Alpha8 중심이며 16-bit·팔레트·원본 타입 보존에 부적합. 밉맵/업로드는 메인 스레드 |
| [AsyncImageLibrary](https://github.com/SrejonKhan/AsyncImageLibrary) | SkiaSharp 기반 비동기 디코드, 캐시/큐 제공 | native 의존성, `byte[]`/경로 중심, `RGBA32`/`BGRA32` 출력, 밉맵 비활성 |
| [unity-async-textureimport](https://github.com/mlavik1/unity-async-textureimport) | FreeImage worker 처리와 CPU 밉맵 생성 | 오래된 프로토타입, `BGRA32` 고정, 경로/`byte[]` 중심, Stream API와 타입 매핑이 부족 |
| [SixLabors/ImageSharp](https://github.com/SixLabors/ImageSharp) | `Stream` 비동기 로드, 명시적 `TPixel`, `Rgb48`/`Rgba64` 등 | Unity `Texture2D` 어댑터·밉맵·업로드를 새로 작성해야 함. 현재 upstream의 .NET 타깃도 Unity 패키지와 검토 필요 |
| [KtxUnity](https://github.com/atteneder/KtxUnity) | KTX2/Basis, GPU 압축·밉맵·비동기 로드 | PNG/JPG/FreeImage 입력 로더가 아니라 사전 변환된 KTX 자산용 |
| [Texture Apply Async](https://github.com/gilzoide/unity-texture-apply-async) | `Apply` 시 렌더 스레드 동기화로 인한 stall 완화 | 이미지 디코더가 아니라 최종 업로드 단계의 보조 수단 |
| [IvanMurzak/Unity-ImageLoader](https://github.com/IvanMurzak/Unity-ImageLoader) | URL/경로 캐시, 중복 요청 병합, Sprite/UI 편의 기능 | raw `Stream`/`byte[]` 디코더 및 출력 포맷 제어 계층이 아님 |

Unity 기본 [`ImageConversion.LoadImage`](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ImageConversion.LoadImage.html)는 `byte[]`에서 PNG/JPG/EXR을 읽을 수 있지만 동기 API이고, 런타임 출력 포맷·밉맵·worker 디코드 제어가 부족하다.

## 현재 결론

확인한 후보 중 모든 요구사항을 만족하면서 `UnityAsyncImageLoader`보다 깔끔한 완제품은 찾지 못했다.

1. 일반적인 8-bit PNG/JPG만 필요하면 `StbImageSharpForUnity`가 가장 깔끔한 선택이다.
2. FreeImage 수준의 입력 호환성, 저비트/팔레트 처리, 16-bit·signed 타입, 밉맵을 목표로 하면 현재 FreeImage 기반 로더를 유지·정리하는 편이 적합하다.
3. 새로 설계한다면 디코더와 Unity 출력을 분리한다.
   - decoder: `Stream` → FreeImage bitmap/내부 pixel representation
   - converter: 내부 타입 → Unity `TextureFormat` 및 mip level
   - uploader: 메인 스레드 `Texture2D` 생성·raw upload·읽기/쓰기 설정
4. Unity에 대응 포맷이 없는 입력까지 원본 그대로 유지하려면 `Texture2D`만으로는 부족하다. 별도 raw buffer/팔레트 셰이더/압축 자산 포맷이 필요하다.
5. 목표는 “모든 입력을 받되, Unity가 표현 가능한 범위는 보존하고, 불가능한 범위는 가장 의미가 보존되는 포맷으로 승격하는 것”이다.
