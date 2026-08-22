# Runiverse OS Texture

Language available: \[[한국어 (대한민국)](README.md) | [**English (US)**](README-EN.md)]

## Overview

`com.rumi.runios.texture` loads encoded image data into runtime `UnityEngine.Texture2D` objects.

- FreeImage-based image decoding
- `ReadOnlyMemory<byte>`, `Stream`, and `IONode` inputs
- Background decoding and Burst mipmap generation
- `Texture2D` creation and raw pixel upload on Unity's main thread
- Support for the broad image input range provided by FreeImage, not only PNG and JPEG

This package is a runtime loader, not a Unity asset importer. Callers provide the encoded image data.

## Requirements

- Unity `6000.3` or later
- `com.unity.burst` `2.0.0`
- `com.unity.mathematics` `1.4.0`
- The project's `RuniOS.IO` and `UniTask` references
- Bundled FreeImage native binaries: Windows x86/x64 and Linux

## Basic usage

```csharp
using Cysharp.Threading.Tasks;
using RuniOS.Textures;
using System.IO;
using System.Threading;
using UnityEngine;

static UniTask<Texture2D> LoadTextureAsync(Stream stream, CancellationToken cancellationToken)
{
    TextureLoadSettings settings = new(
        mipmapCount: 0,
        linear: false,
        makeNoLongerReadable: true);

    return TextureLoader.LoadAsync(stream, settings, cancellationToken);
}
```

The same loader can consume memory and `IONode` inputs.

```csharp
Texture2D fromMemory = await TextureLoader.LoadAsync(
    encodedBytes,
    cancellationToken: cancellationToken);

Texture2D fromFile = await TextureLoader.LoadAsync(
    imageNode,
    cancellationToken: cancellationToken);
```

## Public API

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

`TextureLoadSettings` controls the following behavior.

| Setting | Description |
| --- | --- |
| `mipmapCount` | Values less than or equal to `0` generate the full chain automatically, `1` disables mipmaps, and values of `2` or greater request an explicit count including the base level. The default is `0`. |
| `linear` | Whether the resulting texture treats pixel values as linear data. |
| `makeNoLongerReadable` | Whether CPU-side texture data is discarded after `Apply`. |

The current public API does not expose direct `TextureFormat` selection. The decoder chooses a Unity format automatically from the input channel and bit-depth information.

## Mipmap settings

```csharp
new TextureLoadSettings(mipmapCount: 0); // Automatic generation down to 1x1
new TextureLoadSettings(mipmapCount: 1); // Base level only
new TextureLoadSettings(mipmapCount: 4); // Four total levels, including base
```

Values less than or equal to `0` generate levels down to 1x1. Values of `2` or greater include the base level in the requested count. Values beyond the maximum level allowed by the image dimensions throw `ArgumentOutOfRangeException`.

## Processing pipeline

```text
encoded image data
    -> ThreadPool: FreeImage type detection, decoding, pixel normalization
    -> Burst Job: format-specific mipmap generation
    -> Unity main thread: Texture2D creation, SetPixelData, Apply
```

`Texture2D` creation and `SetPixelData`/`Apply` are main-thread operations. The current implementation assumes that `LoadAsync` is started from Unity's main thread.

The decoder copies FreeImage's native buffer into one `NativeArray<byte>` so native-memory lifetime is independent from Job lifetime. When mipmaps are enabled, each level gets its own `NativeArray<byte>` owned by `TextureMipmapData`.

## Output formats

Decoded data keeps its original data type when possible. Only normalization required for Unity raw texture upload is performed.

| FreeImage result | Unity format | Processing |
| --- | --- | --- |
| 1/2/4/8-bit grayscale, grayscale palette | `R8` | Expanded to 8-bit values |
| 16-bit grayscale | `R16` | `min-is-white` is inverted |
| RGB555/RGB565 | `RGB565` | RGB555 converted to RGB565 |
| 24-bit bitmap | `RGB24` | FreeImage BGR converted to RGB |
| 32-bit bitmap | `RGBA32` | FreeImage BGRA converted to RGBA |
| `uint16` / `int16` | `R16` / `R16_SIGNED` | Raw channels preserved |
| `uint32` / `int32` | `RG32` / `RG32_SIGNED` | Raw payload preserved |
| `float32` / `float64` | `RFloat` / `RGFloat` | Raw floating-point data preserved |
| `complex` | `RGBAFloat` | Uses double-payload mipmaps |
| `rgb16` / `rgba16` | `RGB48` / `RGBA64` | 16-bit channels preserved |
| `rgbFloat` / `rgbaFloat` | `RGBAFloat` | RGB input receives alpha `1.0` |

Paletted, CMYK, and non-canonical bitmap channel layouts may be converted by FreeImage to 24-bit or 32-bit output. If the current platform does not support the selected Unity format, the loader throws `NotSupportedException` without a conversion fallback.

## Input and ownership

- `ReadOnlyMemory<byte>`: The encoded buffer remains owned by the caller. Do not modify or reuse its underlying memory until loading completes.
- `Stream`: The complete stream is read asynchronously. The supplied stream is not closed by the loader.
- `IONode`: The loader calls `node.file.ReadAllBytes()` and then uses the memory-input path.
- `CancellationToken`: Cancellation is observed during input reading, around decoding, and while waiting for Jobs. An in-progress native decode call is not forcefully interrupted.

## Exceptions

- `ArgumentNullException`: null `Stream`
- `ArgumentException`: unreadable `Stream`, empty input, or invalid `IONode`
- `InvalidDataException`: image type detection, decoding, or pixel-buffer validation failure
- `NotSupportedException`: unsupported platform texture format or maximum texture size
- `ArgumentOutOfRangeException`: mipmap count incompatible with image dimensions
- `OperationCanceledException`: cancellation through the supplied token

## Related documents

- [Texture Loader design](TEXTURE-LOADER-DESIGN.md)
- [Texture Loader discussion record](TEXTURE-LOADER-DISCUSSION.md)
- [Third-party notices](THIRD-PARTY-NOTICES.md)
- [FreeImage license](LICENSE.FreeImage.txt)
- [UnityAsyncImageLoader license](LICENSE.AsyncImageLoader.md)
