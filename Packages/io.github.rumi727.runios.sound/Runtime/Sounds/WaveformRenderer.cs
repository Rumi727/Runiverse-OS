#nullable enable
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Scripting.LifecycleManagement;

namespace RuniOS.Sounds
{
    /// <summary>
    /// Renders waveforms into render textures on the GPU.<br/>
    /// 파형을 GPU로 렌더 텍스처에 렌더링합니다.
    /// </summary>
    /// <remarks>
    /// Must be created, used, and disposed on the main thread.<br/>
    /// 메인 스레드에서 생성, 사용, 해제해야 합니다.
    /// </remarks>
    public sealed partial class WaveformRenderer : IDisposable
    {
        static readonly int colorID = Shader.PropertyToID("_Color");
        static readonly int targetHeightID = Shader.PropertyToID("_TargetHeight");
        static readonly int thicknessID = Shader.PropertyToID("_Thickness");
        static readonly int antialiasingID = Shader.PropertyToID("_Antialiasing");
        static readonly int boundaryTexID = Shader.PropertyToID("_BoundaryTex");
        static readonly int samplesPerPixelID = Shader.PropertyToID("_SamplesPerPixel");
        static readonly int subColumnsID = Shader.PropertyToID("_SubColumns");
        static readonly int modeID = Shader.PropertyToID("_Mode");

        // 축소 상태에서 픽셀 하나를 나누는 하위 열의 최대 개수입니다. 셰이더가 픽셀마다 이만큼 텍셀을 읽습니다.
        const int maxSubColumns = 16;

        // Blit은 호출 시점의 프로퍼티로 즉시 그리므로 모든 렌더러가 하나의 머티리얼을 공유합니다.
        static Material material
        {
            get
            {
                if (_material == null)
                    _material = new Material(Resources.Load<Shader>("RuniOS/Sounds/Waveform")) { hideFlags = HideFlags.HideAndDontSave };

                return _material;
            }
        }
        static Material? _material;

        [OnCodeDeinitializing]
        static void OnCodeDeinitializing()
        {
            if (_material != null)
                Object.DestroyImmediate(_material);

            _material = null;
        }

        Texture2D? minMaxTexture;

        // 열 경계마다의 파형 값입니다. 셰이더가 열 안의 파형을 꺾은선으로 복원하는 데 씁니다.
        Texture2D? boundaryTexture;

        bool isDisposed;

        /// <summary>
        /// Renders the range from <paramref name="startSample"/> to <paramref name="endSample"/> of <paramref name="waveform"/> into <paramref name="target"/>, one column per pixel and one lane per channel.<br/>
        /// <paramref name="waveform"/>의 <paramref name="startSample"/>부터 <paramref name="endSample"/>까지의 구간을 픽셀당 한 열, 채널당 한 레인으로 <paramref name="target"/>에 렌더링합니다.
        /// </summary>
        /// <remarks>
        /// The range follows the contract of <see cref="Waveform.ScheduleMinMax"/>.<br/>
        /// 구간은 <see cref="Waveform.ScheduleMinMax"/>의 계약을 따릅니다.
        /// </remarks>
        /// <param name="thickness">
        /// Line thickness in pixels, measured perpendicular to the line regardless of its slope.<br/>
        /// 기울기와 상관없이 선에 수직인 방향으로 잰 선 굵기(픽셀)입니다.
        /// </param>
        /// <param name="antialiasing">
        /// Width in pixels over which the line's edges fade out. <c>0</c> draws hard edges.<br/>
        /// 선 가장자리가 흐려지는 폭(픽셀)입니다. <c>0</c>이면 가장자리를 딱 끊어 그립니다.
        /// </param>
        /// <param name="mode">
        /// How to draw the waveform when one pixel covers two or more samples.<br/>
        /// 픽셀 하나가 샘플 2개 이상을 덮을 때 파형을 그리는 방식입니다.
        /// </param>
        /// <exception cref="ObjectDisposedException">This renderer has been disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="waveform"/> or <paramref name="target"/> is <see langword="null"/>.</exception>
        public void Render(Waveform waveform, double startSample, double endSample, RenderTexture target, Color color, float thickness = 1, float antialiasing = 1, WaveformRenderMode mode = WaveformRenderMode.Density)
        {
            // 해제 후 호출되면 다시 만든 텍스처를 해제할 곳이 없어 누수되므로 거부합니다.
            ObjectDisposedException.ThrowIf(isDisposed, this);
            ArgumentNullException.ThrowIfNull(waveform);

            if (target == null)
                throw new ArgumentNullException(nameof(target));

            int width = target.width;
            int channelCount = waveform.channelCount;
            double samplesPerPixel = (endSample - startSample) / width;

            /*
             * 축소 상태에서는 픽셀 하나를 하위 열로 나눠 min/max를 구하고, 셰이더가 하위 열들이 픽셀을 덮는 비율을 평균 내어 소리의 밀도를 보여줍니다.
             * 하위 열마다 샘플이 하나 이상 들어가도록 픽셀당 샘플 수를 넘지 않게 합니다.
             * 폭 * 하위 열 수는 텍스처 최대 폭을 넘을 수 있으므로, 텍스처는 픽셀 폭으로 두고 하위 열은 줄을 늘려 이어 붙입니다.
             */
            // 피크 방식은 픽셀마다 min/max 하나만 쓰므로 나누지 않습니다.
            int subColumns = mode == WaveformRenderMode.Density ? (int)Math.Clamp(Math.Floor(samplesPerPixel), 1, maxSubColumns) : 1;
            int rowCount = channelCount * subColumns;

            if (minMaxTexture == null || minMaxTexture.width != width || minMaxTexture.height != rowCount)
            {
                if (minMaxTexture != null)
                    Object.DestroyImmediate(minMaxTexture);

                minMaxTexture = new Texture2D(width, rowCount, TextureFormat.RGFloat, false, true)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            if (boundaryTexture == null || boundaryTexture.width != width + 1 || boundaryTexture.height != channelCount)
            {
                if (boundaryTexture != null)
                    Object.DestroyImmediate(boundaryTexture);

                boundaryTexture = new Texture2D(width + 1, channelCount, TextureFormat.RFloat, false, true)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            // 두 잡은 원본을 읽기만 하므로 서로 기다리지 않고 함께 돌립니다.
            JobHandle minMaxHandle = waveform.ScheduleMinMax(startSample, endSample, width * subColumns, minMaxTexture.GetPixelData<float2>(0));
            JobHandle boundaryHandle;
            try
            {
                boundaryHandle = waveform.ScheduleResample(startSample, endSample, width + 1, boundaryTexture.GetPixelData<float>(0));
            }
            catch
            {
                // 이미 스케줄된 잡이 텍스처 메모리에 쓰는 중이므로, 텍스처가 다시 만들어지거나 해제되기 전에 끝냅니다.
                minMaxHandle.Complete();
                throw;
            }

            JobHandle.CombineDependencies(minMaxHandle, boundaryHandle).Complete();

            minMaxTexture.Apply(false);
            boundaryTexture.Apply(false);

            material.SetColor(colorID, color);
            material.SetFloat(targetHeightID, target.height);
            material.SetFloat(thicknessID, thickness);
            material.SetFloat(antialiasingID, antialiasing);
            material.SetFloat(samplesPerPixelID, (float)samplesPerPixel);
            material.SetFloat(subColumnsID, subColumns);
            material.SetFloat(modeID, (float)mode);
            material.SetTexture(boundaryTexID, boundaryTexture);

            // Blit은 활성 렌더 타겟을 바꾸고, IMGUI는 sRGB 쓰기를 끈 채 그리므로 호출자의 상태를 되돌립니다.
            RenderTexture previous = RenderTexture.active;
            bool previousSRGBWrite = GL.sRGBWrite;

            GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
            Graphics.Blit(minMaxTexture, target, material);

            GL.sRGBWrite = previousSRGBWrite;
            RenderTexture.active = previous;
        }

        public void Dispose()
        {
            if (minMaxTexture != null)
                Object.DestroyImmediate(minMaxTexture);

            minMaxTexture = null;

            if (boundaryTexture != null)
                Object.DestroyImmediate(boundaryTexture);

            boundaryTexture = null;
            isDisposed = true;
        }
    }
}
