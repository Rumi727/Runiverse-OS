#nullable enable
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace RuniOS.Utility
{
    public static partial class ResourceUtility
    {
        /// <summary>
        /// 빈 게임 오브젝트
        /// </summary>
        public static Transform emptyTransform
        {
            get
            {
                if (_emptyTransform == null)
                    _emptyTransform = Resources.Load<Transform>("RuniOS/Empty Transform");

                return _emptyTransform;
            }
        }
        static Transform? _emptyTransform;

        /// <summary>
        /// 사각 트랜스폼이 추가된 빈 게임 오브젝트
        /// </summary>
        public static RectTransform emptyRectTransform
        {
            get
            {
                if (_emptyRectTransform == null)
                    _emptyRectTransform = Resources.Load<RectTransform>("RuniOS/Empty Rect Transform");

                return _emptyRectTransform;
            }
        }
        static RectTransform? _emptyRectTransform;



        /// <summary>
        /// 기본 메테리얼
        /// </summary>
        public static Material defaultMaterial
        {
            get
            {
                if (_defaultMaterial == null)
                    _defaultMaterial = Resources.Load<Material>("RuniOS/Default Material");

                return _defaultMaterial;
            }
        }
        static Material? _defaultMaterial;

        /// <summary>
        /// 단색 메테리얼
        /// </summary>
        public static Material coloredMaterial
        {
            get
            {
                if (_coloredMaterial == null)
                {
                    Shader shader = Shader.Find("Hidden/Internal-Colored");
                    _coloredMaterial = new Material(shader)
                    {
                        hideFlags = HideFlags.HideAndDontSave
                    };

                    _coloredMaterial.SetInt(srcBlend, (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    _coloredMaterial.SetInt(dstBlend, (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    _coloredMaterial.SetInt(cull, (int)UnityEngine.Rendering.CullMode.Off);
                    _coloredMaterial.SetInt(zWrite, 0);
                }

                return _coloredMaterial;
            }
        }
        static Material? _coloredMaterial;
        static readonly int srcBlend = Shader.PropertyToID("_SrcBlend");
        static readonly int dstBlend = Shader.PropertyToID("_DstBlend");
        static readonly int cull = Shader.PropertyToID("_Cull");
        static readonly int zWrite = Shader.PropertyToID("_ZWrite");

        extension(Texture2D)
        {
            public static Texture2D missingTexture => _missingTexture;
        }

        static Texture2D _missingTexture = null!;

        [OnCodeInitializing]
        static void OnCodeInitializing()
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                name = "Missing Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat,
                hideFlags = HideFlags.HideAndDontSave
            };

            texture.SetPixels
            ([
                Color.magenta, Color.black,
                Color.black,   Color.magenta
            ]);

            texture.Apply(false, true);
            _missingTexture = texture;
        }

        [OnCodeDeinitializing]
        static void OnCodeDeinitializing()
        {
            if (_missingTexture != null)
                Object.DestroyImmediate(_missingTexture);

            _missingTexture = null!;
        }
    }
}