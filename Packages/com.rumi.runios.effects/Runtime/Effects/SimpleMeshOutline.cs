#nullable enable
using UnityEngine.Serialization;

namespace RuniOS.Effects
{
    [ExecuteAlways]
    [RequireComponent(typeof(Renderer))]
    public class SimpleMeshOutline : MonoBehaviour
    {
        public Color color
        {
            get => _color;
            set => _color = value;
        }
        [SerializeField] Color _color = Color.white;
        
        public float width
        {
            get => _width;
            set => _width = value;
        }
        [SerializeField, Range(0, 5)] float _width = 0.05f;
        
        public bool useFixedWidth
        {
            get => _useFixedWidth;
            set => _useFixedWidth = value;
        }
        [SerializeField] bool _useFixedWidth = false;

        public float gap
        {
            get => _gap;
            set => _gap = value;
        }
        [SerializeField, Range(0, 5)] float _gap = 0f;

        public bool useFixedGap
        {
            get => _useFixedGap;
            set => _useFixedGap = value;
        }
        [SerializeField] bool _useFixedGap = false;

        public OutlineVisibility outlineVisibility
        {
            get => _outlineVisibility;
            set => _outlineVisibility = value;
        }
        [SerializeField] OutlineVisibility _outlineVisibility = OutlineVisibility.Normal;

        // Internal
        MeshFilter? meshFilter;
        SkinnedMeshRenderer? skinnedMeshRenderer;
        Mesh? lastMesh;
        Mesh? bakedMesh;
        Material? material;
        MaterialPropertyBlock? mpb;
        [FormerlySerializedAs("_shader"),SerializeField, HideInInspector] Shader? shader;

        // Property IDs
        static readonly int propColor = Shader.PropertyToID("_Color");
        static readonly int propWidth = Shader.PropertyToID("_Width");
        static readonly int propOffset = Shader.PropertyToID("_Offset");
        static readonly int propZTest = Shader.PropertyToID("_ZTest");
        
        static readonly int propWidthScreen = Shader.PropertyToID("_WidthUseScreen");
        static readonly int propOffsetScreen = Shader.PropertyToID("_OffsetUseScreen");

        void OnEnable()
        {
            meshFilter = GetComponent<MeshFilter>();
            skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
        }

        void OnDisable()
        {
            if (material)
                DestroyImmediate(material);
        }

        Mesh? GetSharedMesh()
        {
            if (meshFilter != null && meshFilter.sharedMesh != null)
                return meshFilter.sharedMesh;
            else if (skinnedMeshRenderer != null && skinnedMeshRenderer.sharedMesh != null)
                return skinnedMeshRenderer.sharedMesh;
            else
                return null;
        }
        
        Mesh? GetBakeSmoothMesh()
        {
            Mesh? source = GetSharedMesh();
            if (source == null)
                return null;
            
            Mesh bakedMesh = Instantiate(source);
            bakedMesh.name = source.name + "_OutlineBaked";
            bakedMesh.hideFlags = HideFlags.HideAndDontSave;

            var vertices = bakedMesh.vertices;
            var normals = bakedMesh.normals;
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

            bakedMesh.SetUVs(1, smoothNormals);
            bakedMesh.UploadMeshData(false);
            
            return bakedMesh;
        }

        void LateUpdate()
        {
            if (shader == null)
            {
                shader = Shader.Find("Custom/SimpleMeshOutline");
                if (shader == null)
                    return;
            }
            
            if (material == null)
                material = new Material(shader);
            
            Mesh? shardedMesh = GetSharedMesh();
            if (shardedMesh != lastMesh || bakedMesh == null)
            {
                if (bakedMesh != null)
                    DestroyImmediate(bakedMesh);
                
                bakedMesh = GetBakeSmoothMesh();
                lastMesh = shardedMesh;
            }

            if (bakedMesh == null)
                return;

            // [수정] Enum 값을 int로 변환하여 셰이더에 전달
            material.SetInt(propZTest, (int)outlineVisibility);

            mpb ??= new MaterialPropertyBlock();
            
            mpb.SetColor(propColor, color);
            mpb.SetFloat(propWidth, width);
            mpb.SetFloat(propOffset, gap);
            
            mpb.SetFloat(propWidthScreen, useFixedWidth ? 1.0f : 0.0f);
            mpb.SetFloat(propOffsetScreen, useFixedGap ? 1.0f : 0.0f);

            Matrix4x4 matrix = transform.localToWorldMatrix;
            int subMeshCount = bakedMesh.subMeshCount;

            for (int i = 0; i < subMeshCount; i++)
                Graphics.DrawMesh(bakedMesh, matrix, material, gameObject.layer, null, i, mpb);
        }
    }
}