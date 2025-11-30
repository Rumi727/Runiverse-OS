#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace RuniOS.Effects
{
    [ExecuteAlways]
    [RequireComponent(typeof(Renderer))]
    public class SimpleMeshOutline : MonoBehaviour
    {
        [Header("Appearance")]
        public Color color = Color.white;
        
        [Range(0, 5)] 
        [Tooltip("두께")]
        public float width = 0.05f;
        
        [Tooltip("두께를 화면 픽셀 기준으로 고정합니다.")]
        public bool widthUseScreen = false;

        [Space(10)]
        [Range(0, 5)] 
        [Tooltip("오프셋 (빈 공간)")]
        public float gap = 0f;

        [Tooltip("오프셋을 화면 픽셀 기준으로 고정합니다.")]
        public bool gapUseScreen = false;

        [Header("Settings")]
        [Tooltip("윤곽선이 보여지는 방식을 결정합니다.\n- Normal: 가려지면 안 보임\n- AlwaysOnTop: 항상 위에 보임\n- OccludedOnly: 가려졌을 때만 보임")]
        public OutlineVisibility outlineVisibility = OutlineVisibility.Normal;

        // Internal
        Renderer? _renderer;
        MeshFilter? _meshFilter;
        Mesh? _bakedMesh;
        Material? _material;
        MaterialPropertyBlock? _mpb;
        [SerializeField, HideInInspector] Shader? _shader;

        // Property IDs
        static readonly int _ColorID = Shader.PropertyToID("_Color");
        static readonly int _WidthID = Shader.PropertyToID("_Width");
        static readonly int _OffsetID = Shader.PropertyToID("_Offset");
        static readonly int _ZTestID = Shader.PropertyToID("_ZTest");
        
        static readonly int _WidthScreenID = Shader.PropertyToID("_WidthUseScreen");
        static readonly int _OffsetScreenID = Shader.PropertyToID("_OffsetUseScreen");

        void OnEnable()
        {
            _renderer = GetComponent<Renderer>();
            _meshFilter = GetComponent<MeshFilter>();

            BakeSmoothMesh();
        }

        void OnDisable()
        {
            if (_material)
                DestroyImmediate(_material);
        }

        void OnValidate()
        {
            if (_bakedMesh == null)
                BakeSmoothMesh();
        }

        [MemberNotNullWhen(true, nameof(_bakedMesh))]
        bool BakeSmoothMesh()
        {
            if (_meshFilter == null || _meshFilter.sharedMesh == null)
                return false;
            
            Mesh source = _meshFilter.sharedMesh;
            if (_bakedMesh != null && _bakedMesh.vertexCount == source.vertexCount)
                return true;
            
            if (_bakedMesh != null)
                DestroyImmediate(_bakedMesh);

            _bakedMesh = Instantiate(source);
            _bakedMesh.name = source.name + "_OutlineBaked";
            _bakedMesh.hideFlags = HideFlags.HideAndDontSave;

            var vertices = _bakedMesh.vertices;
            var normals = _bakedMesh.normals;
            var smoothNormals = new List<Vector3>(vertices.Length);
            var smoothNormalsDict = new Dictionary<Vector3, Vector3>();

            foreach (var v in vertices)
            {
                if (!smoothNormalsDict.ContainsKey(v))
                    smoothNormalsDict[v] = Vector3.zero;
            }

            for (int i = 0; i < vertices.Length; i++)
                smoothNormalsDict[vertices[i]] += normals[i];

            smoothNormals.AddRange(vertices.Select(x => smoothNormalsDict[x].normalized));

            _bakedMesh.SetUVs(1, smoothNormals);
            _bakedMesh.UploadMeshData(false);
            return true;
        }

        void LateUpdate()
        {
            if (_shader == null)
                _shader = Shader.Find("Custom/SimpleMeshOutline");
            
            if (_shader != null)
                _material = new Material(_shader);
            
            if (_renderer == null || _material == null || !BakeSmoothMesh())
                return;

            // [수정] Enum 값을 int로 변환하여 셰이더에 전달
            _material.SetInt(_ZTestID, (int)outlineVisibility);

            _mpb ??= new MaterialPropertyBlock();
            
            _mpb.SetColor(_ColorID, color);
            _mpb.SetFloat(_WidthID, width);
            _mpb.SetFloat(_OffsetID, gap);
            
            _mpb.SetFloat(_WidthScreenID, widthUseScreen ? 1.0f : 0.0f);
            _mpb.SetFloat(_OffsetScreenID, gapUseScreen ? 1.0f : 0.0f);

            Matrix4x4 matrix = transform.localToWorldMatrix;
            int subMeshCount = _bakedMesh.subMeshCount;

            for (int i = 0; i < subMeshCount; i++)
                Graphics.DrawMesh(_bakedMesh, matrix, _material, gameObject.layer, null, i, _mpb);
        }
    }
}