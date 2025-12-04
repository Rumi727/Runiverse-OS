#nullable enable
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace RuniOS.UI.Effects
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic))]
    [AddComponentMenu("UI/Effects/Rounded Corners")]
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    public class RoundedCorners : MonoBehaviour, IMeshModifier, IMaterialModifier
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
            set { _radius = value; Refresh(); }
        }
        [SerializeField] CornerRadius _radius = 20;

        public float softness
        {
            get => _softness;
            set { _softness = value; Refresh(); }
        }
        [SerializeField, Range(0, 5)] float _softness = 1.0f;

        public Color outlineColor
        {
            get => _outlineColor;
            set { _outlineColor = value; Refresh(); }
        }
        [SerializeField] Color _outlineColor = Color.white;

        public float outlineWidth
        {
            get => _outlineWidth;
            set { _outlineWidth = value; Refresh(); }
        }
        [SerializeField, Range(0, 100)] float _outlineWidth = 0.0f;

        public float outlineSoftness
        {
            get => _outlineSoftness;
            set { _outlineSoftness = value; Refresh(); }
        }
        [SerializeField, Range(0, 5)] float _outlineSoftness = 1.0f;
        
        public bool insideOutline
        {
            get => _insideOutline;
            set { _insideOutline = value; Refresh(); }
        }
        [SerializeField] bool _insideOutline = false;

        RectTransform? rectTransform;
        Graphic? graphic;

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

            Refresh();
        }

        void OnDisable()
        {
            if (graphic == null)
                return;

            graphic.SetMaterialDirty();
            graphic.SetVerticesDirty();
        }

        void OnRectTransformDimensionsChange() => Refresh();

#if UNITY_EDITOR
        void OnValidate() => Refresh();
#endif

        public void Refresh()
        {
            if (graphic == null)
                return;

            graphic.SetMaterialDirty();
            graphic.SetVerticesDirty();
        }

        Material IMaterialModifier.GetModifiedMaterial(Material baseMaterial)
        {
            if (!isActiveAndEnabled)
                return baseMaterial;

            float stencil = baseMaterial.HasProperty(propStencil) ? baseMaterial.GetFloat(propStencil) : 0;
            float stencilOp = baseMaterial.HasProperty(propStencilOp) ? baseMaterial.GetFloat(propStencilOp) : 0;
            float stencilComp = baseMaterial.HasProperty(propStencilComp) ? baseMaterial.GetFloat(propStencilComp) : 8; // 8 = Always
            float stencilRead = baseMaterial.HasProperty(propStencilReadMask) ? baseMaterial.GetFloat(propStencilReadMask) : 255;
            float stencilWrite = baseMaterial.HasProperty(propStencilWriteMask) ? baseMaterial.GetFloat(propStencilWriteMask) : 255;
            float colorMask = baseMaterial.HasProperty(propColorMask) ? baseMaterial.GetFloat(propColorMask) : 15; // 15 = RGBA

            return StencilMaterial.Add(
                sharedMaterial, 
                (int)stencil, 
                (StencilOp)stencilOp, 
                (CompareFunction)stencilComp, 
                (ColorWriteMask)colorMask, 
                (int)stencilRead, 
                (int)stencilWrite
            );
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
            float shrinkOffset = isInside ? outlineWidth : 0f;

            Vector2 sizeData = new Vector2
            (
                (rect.width * 0.5f) - shrinkOffset, 
                (rect.height * 0.5f) - shrinkOffset
            );

            float effectiveWidth = rect.width - (shrinkOffset * 2);
            float effectiveHeight = rect.height - (shrinkOffset * 2);
            float maxR = Min(effectiveWidth, effectiveHeight) * 0.5f;

            Vector4 radiusData = new Vector4
            (
                radius.topLeft.Clamp(0, maxR),
                radius.topRight.Clamp(0, maxR),
                radius.bottomLeft.Clamp(0, maxR),
                radius.bottomRight.Clamp(0, maxR)
            );

            float bodySoft = Max(0.001f, softness);

            // 1. Outline Pass
            if (outlineWidth > 0)
            {
                float expand = isInside ? 0f : outlineWidth;
                float outSoft = Max(0.001f, outlineSoftness);

                UIVertex v = vertexList.Count > 0 ? vertexList[0] : new UIVertex();

                v.color = outlineColor;
                v.uv2 = sizeData;
                v.tangent = radiusData;
                v.uv3 = new Vector2(outlineWidth, outSoft);
                v.normal = new Vector3(1.0f, bodySoft, 0);

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