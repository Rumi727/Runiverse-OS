#nullable enable
using UnityEngine.UI;

namespace RuniOS.UI
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Graphic))]
    [AddComponentMenu("UI/Effects/Rounded Corners")]
    public class RoundedCorners : MonoBehaviour, IMaterialModifier
    {
        [Header("Corner Settings")]
        public CornerRadius radius = 20f;

        [Header("Render Settings")]
        [Tooltip("안티에일리어싱 강도입니다. 1.0이 표준(1픽셀)이며, 낮을수록 날카롭고 높을수록 흐려집니다.")]
        [Range(0f, 3f)] public float softness = 1.0f; // 셰이더 수정으로 1.0이 기준점이 됨

        public bool useAntiAliasing = true;

        [Header("Update Options")]
        [Tooltip("Mask 컴포넌트가 같은 오브젝트에 있을 경우, 속성 변경 시 강제로 재질을 재생성하여 즉시 반영합니다. (인스펙터 끊김 발생 가능)")]
        public bool autoRebuildWithMask = true;

        [Tooltip("Mask 유무와 상관없이 속성이 변경될 때마다 재질을 재생성합니다.")]
        public bool alwaysRebuildMaterial = false;

        RectTransform? _rectTransform;
        Graphic? _graphic;
        Material? _material;
    
        Mask? _mask; // Mask 존재 여부 확인용
    
        // 셰이더 프로퍼티 ID
        static readonly int propWidth = Shader.PropertyToID("_Width");
        static readonly int propHeight = Shader.PropertyToID("_Height");
        static readonly int propRadius = Shader.PropertyToID("_Radius");
        static readonly int propSoftness = Shader.PropertyToID("_Softness");

        // Mask 속성 ID
        static readonly int propStencilComp = Shader.PropertyToID("_StencilComp");
        static readonly int propStencil = Shader.PropertyToID("_Stencil");
        static readonly int propStencilOp = Shader.PropertyToID("_StencilOp");
        static readonly int propStencilWriteMask = Shader.PropertyToID("_StencilWriteMask");
        static readonly int propStencilReadMask = Shader.PropertyToID("_StencilReadMask");
        static readonly int propColorMask = Shader.PropertyToID("_ColorMask");
        static readonly int propUseUIAlphaClip = Shader.PropertyToID("_UseUIAlphaClip");

        void OnEnable()
        {
            _rectTransform = transform as RectTransform;
            _graphic = GetComponent<Graphic>();
            _mask = GetComponent<Mask>(); // 활성화 시 Mask 체크
        
            Refresh();
        }

        void OnDisable()
        {
            DestroyMaterial();

            if (_graphic != null)
                _graphic.SetMaterialDirty();
        }

        void LateUpdate()
        {
            if (_rectTransform == null || !_rectTransform.hasChanged)
                return;
            
            Refresh();
            _rectTransform.hasChanged = false;
        }
        
#if UNITY_EDITOR
        void OnValidate() => Refresh();
#endif

        /// <summary>
        /// 외부에서 속성을 변경한 후 호출하면 재질에 반영합니다.
        /// Mask가 있거나 설정에 따라 재질을 재생성할지 결정합니다.
        /// </summary>
        public void Refresh()
        {
            if (_graphic == null) return;

            // Mask가 있는지 다시 확인 (런타임에 추가/제거될 수 있으므로)
            if (_mask == null) _mask = GetComponent<Mask>();
            bool hasMask = _mask != null && _mask.enabled;

            // 강제 재생성 조건: 설정이 켜져있거나, Mask가 있고 auto 옵션이 켜진 경우
            bool shouldRebuild = alwaysRebuildMaterial || (hasMask && autoRebuildWithMask);

            if (shouldRebuild)
                DestroyMaterial(); // 재질을 파괴하면 GetModifiedMaterial에서 새로 생성됨

            _graphic.SetMaterialDirty();
        }

        /// <summary>
        /// 유니티 UI 렌더링 파이프라인 호출
        /// </summary>
        Material IMaterialModifier.GetModifiedMaterial(Material baseMaterial)
        {
            if (!isActiveAndEnabled || _graphic == null)
                return baseMaterial;

            Shader shader = Shader.Find("UI/RoundedCorners");
            if (shader == null)
                return baseMaterial;

            // 재질 생성 (없거나 셰이더가 다르면)
            if (_material == null || _material.shader != shader)
                _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };

            // 1. Mask 속성 복사 (스텐실 연결)
            CopyMaskProperties(baseMaterial, _material);

            // 2. 둥근 모서리 속성 업데이트 (public 메서드 호출)
            UpdateMaterialProperties(_material);

            return _material;
        }

        /// <summary>
        /// 현재 컴포넌트의 설정을 대상 재질에 즉시 적용합니다.
        /// 런타임에 애니메이션 등으로 값을 바꿀 때 유용하게 사용할 수 있습니다.
        /// </summary>
        /// <param name="targetMaterial">적용할 대상 재질</param>
        void UpdateMaterialProperties(Material targetMaterial)
        {
            if (targetMaterial == null || _rectTransform == null) return;

            var rect = _rectTransform.rect;
        
            targetMaterial.SetFloat(propWidth, rect.width);
            targetMaterial.SetFloat(propHeight, rect.height);
            
            float maxRadius = Min(rect.width, rect.height) * 0.5f;
            Vector4 radii = new Vector4(
                radius.topLeft.Clamp(0, maxRadius),
                radius.topRight.Clamp(0, maxRadius),
                radius.bottomLeft.Clamp(0, maxRadius),
                radius.bottomRight.Clamp(0, maxRadius)
            );

            targetMaterial.SetVector(propRadius, radii);
        
            // 셰이더에서 *0.5를 하므로 1.0을 넣으면 1픽셀 AA가 됨
            float finalSoftness = useAntiAliasing ? Max(0.001f, softness) : 0.001f;
            targetMaterial.SetFloat(propSoftness, finalSoftness);
        }

        static void CopyMaskProperties(Material source, Material dest)
        {
            if (source == null || dest == null) return;

            if (source.HasProperty(propStencil))
            {
                dest.SetFloat(propStencil, source.GetFloat(propStencil));
                dest.SetFloat(propStencilComp, source.GetFloat(propStencilComp));
                dest.SetFloat(propStencilOp, source.GetFloat(propStencilOp));
                dest.SetFloat(propStencilReadMask, source.GetFloat(propStencilReadMask));
                dest.SetFloat(propStencilWriteMask, source.GetFloat(propStencilWriteMask));
            }

            if (source.HasProperty(propColorMask))
                dest.SetFloat(propColorMask, source.GetFloat(propColorMask));
        
            if (source.HasProperty(propUseUIAlphaClip))
                dest.SetFloat(propUseUIAlphaClip, source.GetFloat(propUseUIAlphaClip));
        }
        
        void DestroyMaterial()
        {
            if (_material == null)
                return;
            
            DestroyImmediate(_material);
            _material = null;
        }
    }
}