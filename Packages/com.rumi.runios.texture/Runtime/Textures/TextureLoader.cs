#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using System.IO;
using System.Threading;

namespace RuniOS.Textures
{
    /// <summary>
    /// Loads encoded image data into runtime textures.<br/>
    /// 인코딩된 이미지 데이터를 런타임 텍스처로 로드합니다.
    /// </summary>
    public static partial class TextureLoader
    {
        /// <summary>
        /// Asynchronously reads the specified stream, decodes its image data, and creates a texture without closing the stream.<br/>
        /// 지정된 스트림을 닫지 않고 이미지를 비동기로 읽어 디코드하고 텍스처를 생성합니다.
        /// </summary>
        /// <param name="stream">
        /// The readable stream containing encoded image data. Ownership remains with the caller.<br/>
        /// 인코딩 이미지 데이터를 포함한 읽기 가능한 스트림입니다. 소유권은 호출자에게 유지됩니다.
        /// </param>
        /// <param name="settings">
        /// The mipmap, color-space, and readability settings.<br/>
        /// 밉맵, 색 공간, 읽기 가능 여부 설정입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the load operation.<br/>
        /// 로드 작업을 취소하는 데 사용하는 토큰입니다.
        /// </param>
        /// <returns>
        /// When the asynchronous operation completes, returns the created <see cref="Texture2D"/>.<br/>
        /// 비동기 작업이 완료되면 생성된 <see cref="Texture2D"/>를 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="stream"/> is <see langword="null"/>.<br/>
        /// <paramref name="stream"/>이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="stream"/> is unreadable or contains no data.<br/>
        /// <paramref name="stream"/>을 읽을 수 없거나 데이터가 없는 경우 발생합니다.
        /// </exception>
        /// <exception cref="InvalidDataException">
        /// Thrown when the encoded data cannot be decoded.<br/>
        /// 인코딩 데이터를 디코드할 수 없는 경우 발생합니다.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Thrown when the decoded texture format is not supported by the current platform.<br/>
        /// 디코드된 텍스처 포맷을 현재 플랫폼에서 지원하지 않는 경우 발생합니다.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown when the operation is canceled through <paramref name="cancellationToken"/>.<br/>
        /// <paramref name="cancellationToken"/>을 통해 작업이 취소된 경우 발생합니다.
        /// </exception>
        public static async UniTask<Texture2D> LoadAsync(Stream stream, TextureLoadSettings settings = default, CancellationToken cancellationToken = default)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead)
                throw new ArgumentException("The image stream must be readable.", nameof(stream));

            return await LoadAsync(await stream.ReadToEndAsync(cancellationToken), settings, cancellationToken);
        }

        /// <summary>
        /// Asynchronously opens the specified file node, decodes its image data, and creates a texture.<br/>
        /// 지정된 파일 노드를 비동기로 열어 이미지를 디코드하고 텍스처를 생성합니다.
        /// </summary>
        /// <param name="node">
        /// The project I/O node that identifies the image file.<br/>
        /// 이미지 파일을 식별하는 프로젝트 I/O 노드입니다.
        /// </param>
        /// <param name="settings">
        /// The mipmap, color-space, and readability settings.<br/>
        /// 밉맵, 색 공간, 읽기 가능 여부 설정입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the load operation.<br/>
        /// 로드 작업을 취소하는 데 사용하는 토큰입니다.
        /// </param>
        /// <returns>
        /// When the asynchronous operation completes, returns the created <see cref="Texture2D"/>.<br/>
        /// 비동기 작업이 완료되면 생성된 <see cref="Texture2D"/>를 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="node"/> is invalid or contains no data.<br/>
        /// <paramref name="node"/>이 유효하지 않거나 데이터가 없는 경우 발생합니다.
        /// </exception>
        /// <exception cref="InvalidDataException">
        /// Thrown when the encoded data cannot be decoded.<br/>
        /// 인코딩 데이터를 디코드할 수 없는 경우 발생합니다.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Thrown when the decoded texture format is not supported by the current platform.<br/>
        /// 디코드된 텍스처 포맷을 현재 플랫폼에서 지원하지 않는 경우 발생합니다.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown when the operation is canceled through <paramref name="cancellationToken"/>.<br/>
        /// <paramref name="cancellationToken"/>을 통해 작업이 취소된 경우 발생합니다.
        /// </exception>
        public static async UniTask<Texture2D> LoadAsync(IONode node, TextureLoadSettings settings = default, CancellationToken cancellationToken = default)
        {
            if (!node.isValid)
                throw new ArgumentException("The image file node must be valid.", nameof(node));

            return await LoadAsync(await node.file.ReadAllBytes(cancellationToken), settings, cancellationToken);
        }

        /// <summary>
        /// Asynchronously decodes the specified encoded image data and creates a texture.<br/>
        /// 지정된 인코딩 이미지 데이터를 비동기로 디코드하고 텍스처를 생성합니다.
        /// </summary>
        /// <param name="data">
        /// The encoded image data. The memory remains owned by the caller.<br/>
        /// 인코딩된 이미지 데이터입니다. 메모리 소유권은 호출자에게 유지됩니다.
        /// </param>
        /// <param name="settings">
        /// The mipmap, color-space, and readability settings.<br/>
        /// 밉맵, 색 공간, 읽기 가능 여부 설정입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the load operation.<br/>
        /// 로드 작업을 취소하는 데 사용하는 토큰입니다.
        /// </param>
        /// <returns>
        /// When the asynchronous operation completes, returns the created <see cref="Texture2D"/>.<br/>
        /// 비동기 작업이 완료되면 생성된 <see cref="Texture2D"/>를 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="data"/> is empty.<br/>
        /// <paramref name="data"/>가 비어 있는 경우 발생합니다.
        /// </exception>
        /// <exception cref="InvalidDataException">
        /// Thrown when the encoded data cannot be decoded.<br/>
        /// 인코딩 데이터를 디코드할 수 없는 경우 발생합니다.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Thrown when the decoded texture format is not supported by the current platform.<br/>
        /// 디코드된 텍스처 포맷을 현재 플랫폼에서 지원하지 않는 경우 발생합니다.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown when the operation is canceled through <paramref name="cancellationToken"/>.<br/>
        /// <paramref name="cancellationToken"/>을 통해 작업이 취소된 경우 발생합니다.
        /// </exception>
        public static async UniTask<Texture2D> LoadAsync(ReadOnlyMemory<byte> data, TextureLoadSettings settings = default, CancellationToken cancellationToken = default)
        {
            if (data.Length == 0)
                throw new ArgumentException("Encoded image data cannot be empty.", nameof(data));

            cancellationToken.ThrowIfCancellationRequested();

            using DecodedImage decodedImage = await UniTask.RunOnThreadPool(() => FreeImageDecoder.Decode(data), cancellationToken: cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            ValidateDecodedImage(decodedImage);

            TextureMipmapData? mipmapData = null;
            if (settings.mipmaps.mode != TextureMipmapMode.none)
            {
                mipmapData = TextureMipmapScheduler.Schedule(decodedImage, settings.mipmaps);
                while (!mipmapData.Value.dependency.IsCompleted)
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: cancellationToken);

                mipmapData.Value.dependency.Complete();
            }

            Texture2D? texture = null;
            try
            {
                texture = new Texture2D(decodedImage.width, decodedImage.height, decodedImage.textureFormat, mipmapData?.count ?? 1, settings.linear);
                if (mipmapData != null)
                {
                    for (int level = 1; level < mipmapData.Value.count; level++)
                        texture.SetPixelData(mipmapData.Value.levels[level - 1], level);
                }

                texture.SetPixelData(decodedImage.pixels, 0);
                texture.Apply(false, settings.makeNoLongerReadable);

                return texture;
            }
            catch
            {
                if (texture != null)
                    Object.DestroyImmediate(texture);

                throw;
            }
            finally
            {
                mipmapData?.Dispose();
            }
        }

        static void ValidateDecodedImage(DecodedImage decodedImage)
        {
            int maxTextureSize = SystemInfo.maxTextureSize;
            if (decodedImage.width > maxTextureSize || decodedImage.height > maxTextureSize)
                throw new NotSupportedException($"Image dimensions {decodedImage.width}x{decodedImage.height} exceed the platform limit of {maxTextureSize}.");

            if (!SystemInfo.SupportsTextureFormat(decodedImage.textureFormat))
                throw new NotSupportedException($"Texture format {decodedImage.textureFormat} is not supported by this platform.");
        }
    }
}