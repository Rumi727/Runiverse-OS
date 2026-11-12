#nullable enable
using Unity.Collections;
using Unity.Mathematics;
using Unity.Scripting.LifecycleManagement;

namespace RuniOS.Sounds
{
    /// <summary>
    /// Renders waveforms of planar samples into render textures on the GPU.<br/>
    /// planar 샘플의 파형을 GPU로 렌더 텍스처에 렌더링합니다.
    /// </summary>
    /// <remarks>
    /// Must be created, used, and disposed on the main thread.<br/>
    /// 메인 스레드에서 생성, 사용, 해제해야 합니다.
    /// </remarks>
    public sealed partial class WaveformRenderer : IDisposable
    {
        static readonly int colorID = Shader.PropertyToID("_Color");
        static readonly int targetHeightID = Shader.PropertyToID("_TargetHeight");

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

        /// <summary>
        /// Renders the waveform of <paramref name="planarSamples"/> into <paramref name="target"/>, one column per pixel and one lane per channel.<br/>
        /// <paramref name="planarSamples"/>의 파형을 픽셀당 한 열, 채널당 한 레인으로 <paramref name="target"/>에 렌더링합니다.
        /// </summary>
        /// <param name="planarSamples">
        /// Samples laid out per channel as <c>[channel * sampleCount + sample]</c>.<br/>
        /// <c>[channel * sampleCount + sample]</c>로 채널별 배치된 샘플입니다.
        /// </param>
        public void Render(NativeArray<float> planarSamples, int channelCount, RenderTexture target, Color color)
        {
            int width = target.width;
            if (minMaxTexture == null || minMaxTexture.width != width || minMaxTexture.height != channelCount)
            {
                if (minMaxTexture != null)
                    Object.DestroyImmediate(minMaxTexture);

                minMaxTexture = new Texture2D(width, channelCount, TextureFormat.RGFloat, false, true)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            Waveform.ScheduleMinMax(planarSamples, channelCount, width, minMaxTexture.GetPixelData<float2>(0)).Complete();
            minMaxTexture.Apply(false);

            material.SetColor(colorID, color);
            material.SetFloat(targetHeightID, target.height);

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
        }
    }
}
