#nullable enable
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace RuniOS.UI.Effects
{
    /// <summary>
    /// 연결된 그래픽의 모서리를 둥글게 처리하고 안티에일리어싱을 적용하는 컴포넌트입니다.
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(Graphic))]
    [AddComponentMenu("UI/Effects/Rounded Corners")]
    public class RoundedCorners : MonoBehaviour, IMaterialModifier
    {
        public CornerRadius radius
        {
            get => _radius;
            set
            {
                _radius = value;
                Refresh();
            }
        }
        [SerializeField] CornerRadius _radius = 20;
        
        public bool useAntiAliasing
        {
            get => _useAntiAliasing;
            set
            {
                _useAntiAliasing = value;
                Refresh();
            }
        }
        [SerializeField] bool _useAntiAliasing = true;
        
        public float softness
        {
            get => _softness;
            set
            {
                _softness = value;
                Refresh();
            }
        }
        [SerializeField, Range(0, 3)] float _softness = 1.0f;
        
        public bool autoRebuildWithMask
        {
            get => _autoRebuildWithMask;
            set
            {
                _autoRebuildWithMask = value;
                Refresh();
            }
        }
        [SerializeField] bool _autoRebuildWithMask = true;
        public bool alwaysRebuildMaterial

        {
            get => _alwaysRebuildMaterial;
            set
            {
                _alwaysRebuildMaterial = value;
                Refresh();
            }
        }
        [SerializeField] bool _alwaysRebuildMaterial = false;

        RectTransform? rectTransform;
        Graphic? graphic;
        Material? material;
        [FormerlySerializedAs("_shader"),SerializeField, HideInInspector] Shader? shader;
    
        Mask? mask; // Mask 존재 여부 확인용
    
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
            rectTransform = transform as RectTransform;
            graphic = GetComponent<Graphic>();
            mask = GetComponent<Mask>(); // 활성화 시 Mask 체크
        
            Refresh();
        }

        void OnDisable()
        {
            DestroyMaterial();

            if (graphic != null)
                graphic.SetMaterialDirty();
        }

        void OnRectTransformDimensionsChange() => Refresh();

#if UNITY_EDITOR
        void OnValidate() => Refresh();
#endif

        /// <summary>
        /// 외부에서 속성을 변경한 후 호출하면 재질에 반영합니다.
        /// Mask가 있거나 설정에 따라 재질을 재생성할지 결정합니다.
        /// </summary>
        public void Refresh()
        {
            if (graphic == null) return;

            // Mask가 있는지 다시 확인 (런타임에 추가/제거될 수 있으므로)
            if (mask == null) mask = GetComponent<Mask>();
            bool hasMask = mask != null && mask.enabled;

            // 강제 재생성 조건: 설정이 켜져있거나, Mask가 있고 auto 옵션이 켜진 경우
            bool shouldRebuild = alwaysRebuildMaterial || (hasMask && autoRebuildWithMask);

            if (shouldRebuild)
                DestroyMaterial(); // 재질을 파괴하면 GetModifiedMaterial에서 새로 생성됨

            graphic.SetMaterialDirty();
        }

        /// <summary>
        /// 유니티 UI 렌더링 파이프라인 호출
        /// </summary>
        Material IMaterialModifier.GetModifiedMaterial(Material baseMaterial)
        {
            if (!isActiveAndEnabled || graphic == null)
                return baseMaterial;

            shader = Shader.Find("UI/RoundedCorners");
            if (shader == null)
                return baseMaterial;

            // 재질 생성 (없거나 셰이더가 다르면)
            if (material == null || material.shader != shader)
                material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };

            // 1. Mask 속성 복사 (스텐실 연결)
            CopyMaskProperties(baseMaterial, material);

            // 2. 둥근 모서리 속성 업데이트 (public 메서드 호출)
            UpdateMaterialProperties(material);

            return material;
        }

        /// <summary>
        /// 현재 컴포넌트의 설정을 대상 재질에 즉시 적용합니다.
        /// 런타임에 애니메이션 등으로 값을 바꿀 때 유용하게 사용할 수 있습니다.
        /// </summary>
        /// <param name="targetMaterial">적용할 대상 재질</param>
        void UpdateMaterialProperties(Material targetMaterial)
        {
            if (targetMaterial == null || rectTransform == null) return;

            var rect = rectTransform.rect;
        
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
            if (material == null)
                return;
            
            DestroyImmediate(material);
            material = null;
        }
    }
}