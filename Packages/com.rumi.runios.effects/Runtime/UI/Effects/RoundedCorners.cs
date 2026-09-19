#nullable enable
using UnityEngine.Rendering;
using UnityEngine.UI;
using RuniOS.Effects;

namespace RuniOS.UI.Effects
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [AddComponentMenu("UI (Canvas)/Effects/Rounded Corners")]
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    public class RoundedCorners : MonoBehaviour, IMeshModifier, IMaterialModifier, ICanvasRaycastFilter
    {
#if UNITY_EDITOR
        static RoundedCorners()
        {
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += () =>
            {
                if (sharedMaterial == null)
                    return;

                DestroyImmediate(sharedMaterial);
                sharedMaterial = null;
            };
        }
#endif

        public CornerRadius radius
        {
            get => _radius;
            set
            {
                CornerRadius normalized = Normalize(value);
                if (_radius == normalized)
                    return;

                _radius = normalized;
                SetVerticesDirty();
            }
        }
        [SerializeField] CornerRadius _radius = 20;

        public float softness
        {
            get => _softness;
            set
            {
                float normalized = NormalizeNonNegative(value);
                if (_softness == normalized)
                    return;

                _softness = normalized;
                SetVerticesDirty();
            }
        }
        [SerializeField, Range(0, 5)] float _softness = 1.0f;

        public Color outlineColor
        {
            get => _outlineColor;
            set
            {
                if (_outlineColor == value)
                    return;

                _outlineColor = value;
                SetVerticesDirty();
            }
        }
        [SerializeField] Color _outlineColor = Color.white;

        public float outlineWidth
        {
            get => _outlineWidth;
            set
            {
                float normalized = NormalizeNonNegative(value);
                if (_outlineWidth == normalized)
                    return;

                _outlineWidth = normalized;
                SetVerticesDirty();
            }
        }
        [SerializeField, Range(0, 100)] float _outlineWidth = 0.0f;

        public float outlineSoftness
        {
            get => _outlineSoftness;
            set
            {
                float normalized = NormalizeNonNegative(value);
                if (_outlineSoftness == normalized)
                    return;

                _outlineSoftness = normalized;
                SetVerticesDirty();
            }
        }
        [SerializeField, Range(0, 5)] float _outlineSoftness = 1.0f;
        
        public bool insideOutline
        {
            get => _insideOutline;
            set
            {
                if (_insideOutline == value)
                    return;

                _insideOutline = value;
                SetVerticesDirty();
            }
        }
        [SerializeField] bool _insideOutline = false;

        RectTransform? rectTransform;
        Graphic? graphic;
        Canvas? currentCanvas;

        Material? modifiedMaterial;
        Material? modifiedBaseMaterial;
        int modifiedStencil;
        int modifiedStencilOp;
        int modifiedStencilComp;
        int modifiedStencilRead;
        int modifiedStencilWrite;
        int modifiedColorMask;

        readonly List<UIVertex> vertexList = new List<UIVertex>();
        readonly List<UIVertex> outputList = new List<UIVertex>();

        static Material? sharedMaterial;

        static readonly int propStencil = Shader.PropertyToID("_Stencil");
        static readonly int propStencilOp = Shader.PropertyToID("_StencilOp");
        static readonly int propStencilComp = Shader.PropertyToID("_StencilComp");
        static readonly int propStencilReadMask = Shader.PropertyToID("_StencilReadMask");
        static readonly int propStencilWriteMask = Shader.PropertyToID("_StencilWriteMask");
        static readonly int propColorMask = Shader.PropertyToID("_ColorMask");

        void OnEnable()
        {
            rectTransform = transform as RectTransform;
            graphic = GetComponent<Graphic>();

            if (sharedMaterial == null)
            {
                Shader? shader = Shader.Find("Hidden/RuniOS/RoundedCorners");
                if (shader != null)
                    sharedMaterial = new Material(shader) { hideFlags = HideFlags.DontSave };
            }

            EnsureCanvasChannels();
            SetAllDirty();
        }

        void OnDisable()
        {
            ReleaseModifiedMaterial();
            SetAllDirty();
        }

        void OnDestroy() => ReleaseModifiedMaterial();

        void OnRectTransformDimensionsChange() => SetVerticesDirty();

        void OnTransformParentChanged()
        {
            EnsureCanvasChannels();
            SetAllDirty();
        }

        void OnCanvasHierarchyChanged()
        {
            EnsureCanvasChannels();
            SetAllDirty();
        }

        void OnDidApplyAnimationProperties()
        {
            NormalizeSerializedValues();
            SetVerticesDirty();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            NormalizeSerializedValues();
            SetVerticesDirty();
        }
#endif

        public void Refresh() => SetVerticesDirty();

        void SetVerticesDirty()
        {
            if (graphic == null)
                return;

            graphic.SetVerticesDirty();
        }

        void SetAllDirty()
        {
            if (graphic == null)
                return;

            graphic.SetMaterialDirty();
            graphic.SetVerticesDirty();
        }

        void EnsureCanvasChannels()
        {
            currentCanvas = GetComponentInParent<Canvas>();
            if (currentCanvas == null)
                return;

            const AdditionalCanvasShaderChannels required =
                AdditionalCanvasShaderChannels.TexCoord1 |
                AdditionalCanvasShaderChannels.TexCoord2 |
                AdditionalCanvasShaderChannels.TexCoord3 |
                AdditionalCanvasShaderChannels.Normal |
                AdditionalCanvasShaderChannels.Tangent;

            currentCanvas.additionalShaderChannels |= required;
        }

        Material IMaterialModifier.GetModifiedMaterial(Material baseMaterial)
        {
            if (!isActiveAndEnabled)
                return baseMaterial;

            if (sharedMaterial == null)
                return baseMaterial;

            float stencil = baseMaterial.HasProperty(propStencil) ? baseMaterial.GetFloat(propStencil) : 0;
            float stencilOp = baseMaterial.HasProperty(propStencilOp) ? baseMaterial.GetFloat(propStencilOp) : 0;
            float stencilComp = baseMaterial.HasProperty(propStencilComp) ? baseMaterial.GetFloat(propStencilComp) : 8; // 8 = Always
            float stencilRead = baseMaterial.HasProperty(propStencilReadMask) ? baseMaterial.GetFloat(propStencilReadMask) : 255;
            float stencilWrite = baseMaterial.HasProperty(propStencilWriteMask) ? baseMaterial.GetFloat(propStencilWriteMask) : 255;
            float colorMask = baseMaterial.HasProperty(propColorMask) ? baseMaterial.GetFloat(propColorMask) : 15; // 15 = RGBA

            int stencilValue = (int)stencil;
            int stencilOpValue = (int)stencilOp;
            int stencilCompValue = (int)stencilComp;
            int stencilReadValue = (int)stencilRead;
            int stencilWriteValue = (int)stencilWrite;
            int colorMaskValue = (int)colorMask;

            if (modifiedMaterial != null && modifiedBaseMaterial == baseMaterial &&
                modifiedStencil == stencilValue && modifiedStencilOp == stencilOpValue &&
                modifiedStencilComp == stencilCompValue && modifiedStencilRead == stencilReadValue &&
                modifiedStencilWrite == stencilWriteValue && modifiedColorMask == colorMaskValue)
                return modifiedMaterial;

            ReleaseModifiedMaterial();
            modifiedMaterial = StencilMaterial.Add(
                sharedMaterial, 
                stencilValue,
                (StencilOp)stencilOp, 
                (CompareFunction)stencilComp, 
                (ColorWriteMask)colorMask, 
                stencilReadValue,
                stencilWriteValue
            );

            modifiedBaseMaterial = baseMaterial;
            modifiedStencil = stencilValue;
            modifiedStencilOp = stencilOpValue;
            modifiedStencilComp = stencilCompValue;
            modifiedStencilRead = stencilReadValue;
            modifiedStencilWrite = stencilWriteValue;
            modifiedColorMask = colorMaskValue;
            return modifiedMaterial;
        }

        void ReleaseModifiedMaterial()
        {
            if (modifiedMaterial != null)
                StencilMaterial.Remove(modifiedMaterial);

            modifiedMaterial = null;
            modifiedBaseMaterial = null;
        }

        void IMeshModifier.ModifyMesh(Mesh mesh)
        {
            using VertexHelper vh = new VertexHelper(mesh);
            ((IMeshModifier)this).ModifyMesh(vh);
            vh.FillMesh(mesh);
        }

        void IMeshModifier.ModifyMesh(VertexHelper vh)
        {
            if (!isActiveAndEnabled || rectTransform == null)
                return;

            vh.GetUIVertexStream(vertexList);
            outputList.Clear();

            Rect rect = rectTransform.rect;
            Vector2 center = rect.center;

            bool isInside = insideOutline && outlineWidth > 0;
            Vector2 sizeData = new Vector2(Max(rect.width * 0.5f, 0), Max(rect.height * 0.5f, 0));
            float maxR = Min(sizeData.x, sizeData.y);

            Vector4 radiusData = new Vector4
            (
                radius.topLeft.Clamp(0, maxR),
                radius.topRight.Clamp(0, maxR),
                radius.bottomLeft.Clamp(0, maxR),
                radius.bottomRight.Clamp(0, maxR)
            );

            float bodySoft = Max(softness, 0);

            // 1. Outline Pass
            if (outlineWidth > 0)
            {
                float expand = isInside ? 0f : outlineWidth;
                float outSoft = Max(outlineSoftness, 0);

                UIVertex v = vertexList.Count > 0 ? vertexList[0] : new UIVertex();

                v.color = outlineColor;
                v.uv2 = sizeData;
                v.tangent = radiusData;
                v.uv3 = new Vector2(outlineWidth, outSoft);
                v.normal = new Vector3(1.0f, bodySoft, isInside ? 1.0f : 0.0f);

                AddQuad(v, rect, expand, center);
            }

            // 2. Body Pass
            int count = vertexList.Count;
            for (int i = 0; i < count; i++)
            {
                UIVertex v = vertexList[i];

                v.uv1 = new Vector2(v.position.x - center.x, v.position.y - center.y);
                v.uv2 = sizeData;
                v.tangent = radiusData;
                v.uv3 = new Vector2(0, bodySoft);
                v.normal = new Vector3(0.0f, 0, 0);

                outputList.Add(v);
            }

            vh.Clear();
            vh.AddUIVertexTriangleStream(outputList);
        }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera? eventCamera)
        {
            if (!isActiveAndEnabled || rectTransform == null ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out Vector2 point))
                return false;

            Rect rect = rectTransform.rect;
            Vector2 halfSize = new Vector2(Max(rect.width * 0.5f, 0), Max(rect.height * 0.5f, 0));
            Vector2 local = point - rect.center;
            float maxRadius = Min(halfSize.x, halfSize.y);
            Vector4 radii = new Vector4
            (
                radius.topLeft.Clamp(0, maxRadius),
                radius.topRight.Clamp(0, maxRadius),
                radius.bottomLeft.Clamp(0, maxRadius),
                radius.bottomRight.Clamp(0, maxRadius)
            );

            float selectedRadius;
            if (local.x > 0)
                selectedRadius = local.y > 0 ? radii.y : radii.w;
            else
                selectedRadius = local.y > 0 ? radii.x : radii.z;

            Vector2 q = new Vector2(Mathf.Abs(local.x), Mathf.Abs(local.y)) - halfSize + Vector2.one * selectedRadius;
            float distance = Min(Max(q.x, q.y), 0) + new Vector2(Max(q.x, 0), Max(q.y, 0)).magnitude - selectedRadius;
            return distance <= 0;
        }

        void NormalizeSerializedValues()
        {
            _radius = Normalize(_radius);
            _softness = NormalizeNonNegative(_softness);
            _outlineWidth = NormalizeNonNegative(_outlineWidth);
            _outlineSoftness = NormalizeNonNegative(_outlineSoftness);
        }

        static CornerRadius Normalize(CornerRadius value) => new CornerRadius
        (
            NormalizeNonNegative(value.topLeft),
            NormalizeNonNegative(value.topRight),
            NormalizeNonNegative(value.bottomRight),
            NormalizeNonNegative(value.bottomLeft)
        );

        static float NormalizeNonNegative(float value) => float.IsNaN(value) || float.IsInfinity(value) || value < 0 ? 0 : value;

        void AddQuad(UIVertex v, Rect rect, float expand, Vector2 center)
        {
            float minX = rect.xMin - expand;
            float maxX = rect.xMax + expand;
            float minY = rect.yMin - expand;
            float maxY = rect.yMax + expand;

            v.position = new Vector3(minX, minY);
            v.uv1 = new Vector2(v.position.x - center.x, v.position.y - center.y);
            outputList.Add(v);

            v.position = new Vector3(minX, maxY);
            v.uv1 = new Vector2(v.position.x - center.x, v.position.y - center.y);
            outputList.Add(v);

            v.position = new Vector3(maxX, maxY);
            v.uv1 = new Vector2(v.position.x - center.x, v.position.y - center.y);
            outputList.Add(v);

            outputList.Add(v); 

            v.position = new Vector3(maxX, minY);
            v.uv1 = new Vector2(v.position.x - center.x, v.position.y - center.y);
            outputList.Add(v);

            v.position = new Vector3(minX, minY);
            v.uv1 = new Vector2(v.position.x - center.x, v.position.y - center.y);
            outputList.Add(v);
        }
    }
}